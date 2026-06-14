#!/usr/bin/env python3
"""
Meshy AI asset generation pipeline for the hockey project.

Reads an asset manifest (assets.json) describing original, IP-free characters and
props, then drives Meshy's REST API to produce game-ready models:

    character:  text-to-3d preview (A-pose) -> refine (PBR) -> rigging (rigged FBX + walk/run anims)
    prop:       text-to-3d preview          -> refine (PBR)

Everything is resumable: per-asset task ids and statuses are persisted to
Tools/meshy/_work/state.json, so the script can be stopped and re-run (or run in
the background) without repeating completed work or re-spending credits.

Downloads land in  Assets/Art/Generated/<asset_id>/  and are committed to the repo.
Only the Python standard library is used (no pip install required).

Usage:
    export MESHY_API_KEY=...                 # or put the key in Tools/meshy/secrets/meshy.key
    python3 Tools/meshy/generate.py --dry-run            # validate manifest, print plan (no network)
    python3 Tools/meshy/generate.py --max-priority 1     # generate only the essential (tier-1) assets
    python3 Tools/meshy/generate.py                      # generate everything
    python3 Tools/meshy/generate.py --status             # show progress of all assets
    python3 Tools/meshy/generate.py --only puck,net      # generate specific assets

API reference: https://docs.meshy.ai/api/text-to-3d  and  https://docs.meshy.ai/api/rigging
"""

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request

API_BASE = "https://api.meshy.ai"

ROOT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(ROOT, "..", ".."))
WORK = os.path.join(ROOT, "_work")
STATE_PATH = os.path.join(WORK, "state.json")
OUT_ROOT = os.path.join(REPO, "Assets", "Art", "Generated")

KEY = None


def log(*a):
    print("[meshy]", *a, flush=True)


# ----------------------------------------------------------------------------- key / http
def load_key():
    key = os.environ.get("MESHY_API_KEY")
    if not key:
        for p in (os.path.join(ROOT, "secrets", "meshy.key"), os.path.join(ROOT, "meshy.key")):
            if os.path.exists(p):
                with open(p) as f:
                    key = f.read().strip()
                break
    if not key:
        log("ERROR: no API key found.")
        log("  Set the MESHY_API_KEY environment variable, or write the key to")
        log("  Tools/meshy/secrets/meshy.key (that path is gitignored).")
        sys.exit(2)
    return key


def _headers():
    return {"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"}


def api(method, path, body=None, retries=5):
    url = API_BASE + path
    data = json.dumps(body).encode() if body is not None else None
    last = None
    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, data=data, headers=_headers(), method=method)
            with urllib.request.urlopen(req, timeout=90) as r:
                raw = r.read().decode()
                return json.loads(raw) if raw else {}
        except urllib.error.HTTPError as e:
            txt = e.read().decode(errors="replace")
            if e.code in (429, 500, 502, 503, 504):
                wait = 2 ** attempt
                log(f"HTTP {e.code} on {method} {path}; retry in {wait}s")
                time.sleep(wait)
                last = e
                continue
            raise SystemExit(f"HTTP {e.code} on {method} {path}: {txt}")
        except (urllib.error.URLError, TimeoutError) as e:
            wait = 2 ** attempt
            log(f"network error on {method} {path}: {e}; retry in {wait}s")
            time.sleep(wait)
            last = e
            continue
    raise SystemExit(f"giving up on {method} {path}: {last}")


def download(url, dest):
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    last = None
    for attempt in range(4):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "hockey-meshy/1.0"})
            with urllib.request.urlopen(req, timeout=180) as r, open(dest, "wb") as f:
                f.write(r.read())
            return dest
        except Exception as e:  # noqa: BLE001 - want to retry any transient failure
            wait = 2 ** attempt
            log(f"download retry for {os.path.basename(dest)} in {wait}s ({e})")
            time.sleep(wait)
            last = e
    raise SystemExit(f"failed to download {url}: {last}")


# ----------------------------------------------------------------------------- state
def load_state():
    if os.path.exists(STATE_PATH):
        with open(STATE_PATH) as f:
            return json.load(f)
    return {}


def save_state(s):
    os.makedirs(WORK, exist_ok=True)
    with open(STATE_PATH, "w") as f:
        json.dump(s, f, indent=2)


# ----------------------------------------------------------------------------- meshy calls
def create_t23d(body):
    return api("POST", "/openapi/v2/text-to-3d", body)["result"]


def get_t23d(tid):
    return api("GET", f"/openapi/v2/text-to-3d/{tid}")


def create_rig(body):
    return api("POST", "/openapi/v1/rigging", body)["result"]


def get_rig(tid):
    return api("GET", f"/openapi/v1/rigging/{tid}")


def wait_for(getter, tid, label, poll, timeout):
    start = time.time()
    while True:
        t = getter(tid)
        status = t.get("status")
        log(f"  {label} {tid[:8]}... {status} {t.get('progress', 0)}%")
        if status == "SUCCEEDED":
            return t
        if status in ("FAILED", "CANCELED"):
            raise SystemExit(f"{label} {tid} ended as {status}: {t.get('task_error') or ''}")
        if time.time() - start > timeout:
            raise SystemExit(f"{label} {tid} timed out after {timeout}s")
        time.sleep(poll)


# ----------------------------------------------------------------------------- downloads
def save_model(task, outdir):
    os.makedirs(outdir, exist_ok=True)
    urls = task.get("model_urls") or {}
    for fmt in ("fbx", "glb", "obj", "mtl", "usdz", "stl"):
        if urls.get(fmt):
            download(urls[fmt], os.path.join(outdir, f"model.{fmt}"))
    if task.get("thumbnail_url"):
        download(task["thumbnail_url"], os.path.join(outdir, "thumbnail.png"))
    for i, tex in enumerate(task.get("texture_urls") or []):
        suffix = "" if i == 0 else f"_{i}"
        for chan in ("base_color", "metallic", "normal", "roughness", "emission"):
            if tex.get(chan):
                download(tex[chan], os.path.join(outdir, f"tex_{chan}{suffix}.png"))


def save_rig(task, outdir):
    os.makedirs(outdir, exist_ok=True)
    res = task.get("result") or {}
    if res.get("rigged_character_fbx_url"):
        download(res["rigged_character_fbx_url"], os.path.join(outdir, "character_rigged.fbx"))
    if res.get("rigged_character_glb_url"):
        download(res["rigged_character_glb_url"], os.path.join(outdir, "character_rigged.glb"))
    anims = res.get("basic_animations") or {}
    for name in ("walking", "running"):
        for fmt in ("fbx", "glb"):
            u = anims.get(f"{name}_{fmt}_url")
            if u:
                download(u, os.path.join(outdir, f"anim_{name}.{fmt}"))


# ----------------------------------------------------------------------------- pipeline
def merged(asset, defaults):
    m = dict(defaults)
    m.update(asset)
    return m


def run_asset(asset, defaults, state, args):
    aid = asset["id"]
    cfg = merged(asset, defaults)
    outdir = os.path.join(OUT_ROOT, aid)
    st = state.setdefault(aid, {})

    if args.dry_run:
        stages = ["preview", "refine"] + (["rig"] if cfg.get("rig") else [])
        log(f"[dry] {aid:18} ({cfg.get('category','?')}): stages={'+'.join(stages)} "
            f"topology={cfg.get('topology')} poly={cfg.get('target_polycount')} "
            f"pose={cfg.get('pose_mode') or '-'} -> Assets/Art/Generated/{aid}/")
        return

    # --- 1. preview (geometry) ------------------------------------------------
    if not st.get("preview_id"):
        body = {
            "mode": "preview",
            "prompt": cfg["prompt"],
            "ai_model": cfg.get("ai_model", "meshy-5"),
            "model_type": cfg.get("model_type", "standard"),
            "topology": cfg.get("topology", "triangle"),
            "target_polycount": cfg.get("target_polycount", 30000),
            "target_formats": cfg.get("target_formats", ["fbx", "glb"]),
            "should_remesh": True,
            "auto_size": True,
            "origin_at": "bottom",
        }
        if cfg.get("pose_mode"):
            body["pose_mode"] = cfg["pose_mode"]
        st["preview_id"] = create_t23d(body)
        save_state(state)
        log(f"{aid}: preview task created {st['preview_id']}")
    if st.get("preview_status") != "SUCCEEDED":
        wait_for(get_t23d, st["preview_id"], f"{aid} preview", args.poll, args.timeout)
        st["preview_status"] = "SUCCEEDED"
        save_state(state)

    # --- 2. refine (PBR textures) --------------------------------------------
    if not st.get("refine_id"):
        body = {
            "mode": "refine",
            "preview_task_id": st["preview_id"],
            "enable_pbr": cfg.get("enable_pbr", True),
            "hd_texture": cfg.get("hd_texture", False),
            "target_formats": cfg.get("target_formats", ["fbx", "glb"]),
        }
        if cfg.get("texture_prompt"):
            body["texture_prompt"] = cfg["texture_prompt"]
        st["refine_id"] = create_t23d(body)
        save_state(state)
        log(f"{aid}: refine task created {st['refine_id']}")
    if not st.get("refine_done"):
        t = wait_for(get_t23d, st["refine_id"], f"{aid} refine", args.poll, args.timeout)
        save_model(t, os.path.join(outdir, "model"))
        st["refine_status"] = "SUCCEEDED"
        st["refine_done"] = True
        st["credits_refine"] = t.get("consumed_credits")
        save_state(state)
        log(f"{aid}: model downloaded -> {os.path.relpath(os.path.join(outdir, 'model'), REPO)}")

    # --- 3. rig (characters only) --------------------------------------------
    if cfg.get("rig"):
        if not st.get("rig_id"):
            body = {"input_task_id": st["refine_id"], "height_meters": cfg.get("height_meters", 1.8)}
            st["rig_id"] = create_rig(body)
            save_state(state)
            log(f"{aid}: rigging task created {st['rig_id']}")
        if not st.get("rig_done"):
            t = wait_for(get_rig, st["rig_id"], f"{aid} rig", args.poll, args.timeout)
            save_rig(t, os.path.join(outdir, "rigged"))
            st["rig_status"] = "SUCCEEDED"
            st["rig_done"] = True
            save_state(state)
            log(f"{aid}: rigged character + anims downloaded")

    st["complete"] = True
    save_state(state)
    log(f"{aid}: COMPLETE")


def write_generated_manifest(assets, state):
    entries = []
    for a in assets:
        st = state.get(a["id"], {})
        if not st.get("complete"):
            continue
        entries.append({
            "id": a["id"],
            "category": a.get("category"),
            "rig": bool(a.get("rig")),
            "dir": f"Assets/Art/Generated/{a['id']}",
        })
    os.makedirs(OUT_ROOT, exist_ok=True)
    with open(os.path.join(OUT_ROOT, "generated.manifest.json"), "w") as f:
        json.dump({"assets": entries}, f, indent=2)
    if entries:
        log(f"wrote generated.manifest.json ({len(entries)} completed assets)")


# ----------------------------------------------------------------------------- main
def main():
    global KEY
    ap = argparse.ArgumentParser(description="Meshy AI asset generation pipeline")
    ap.add_argument("--assets", default=os.path.join(ROOT, "assets.json"))
    ap.add_argument("--only", default="", help="comma-separated asset ids")
    ap.add_argument("--max", type=int, default=0, help="limit number of assets processed")
    ap.add_argument("--max-priority", type=int, default=0, help="only assets with priority <= N")
    ap.add_argument("--poll", type=int, default=12, help="poll interval seconds")
    ap.add_argument("--timeout", type=int, default=5400, help="per-task timeout seconds")
    ap.add_argument("--dry-run", action="store_true", help="print plan, no network calls")
    ap.add_argument("--status", action="store_true", help="print progress and exit")
    args = ap.parse_args()

    with open(args.assets) as f:
        data = json.load(f)
    defaults = data.get("defaults", {})
    assets = data["assets"]
    state = load_state()

    if args.status:
        for a in assets:
            st = state.get(a["id"], {})
            rig = (st.get("rig_status", "-") if a.get("rig") else "n/a")
            print(f"  {a['id']:18} preview={st.get('preview_status','-'):10} "
                  f"refine={st.get('refine_status','-'):10} rig={rig:10} "
                  f"complete={st.get('complete', False)}")
        return

    only = {x for x in args.only.split(",") if x}
    todo = [a for a in assets if (not only or a["id"] in only)]
    if args.max_priority:
        todo = [a for a in todo if a.get("priority", 99) <= args.max_priority]
    if args.max:
        todo = todo[:args.max]

    if not args.dry_run:
        KEY = load_key()

    log(f"{len(todo)} asset(s) to process")
    failures = []
    for a in todo:
        if state.get(a["id"], {}).get("complete"):
            log(f"{a['id']}: already complete, skipping")
            continue
        try:
            run_asset(a, defaults, state, args)
        except SystemExit as e:
            log(f"{a['id']} FAILED: {e}")
            failures.append(a["id"])
        except Exception as e:  # noqa: BLE001 - keep going so one bad asset doesn't stop the batch
            log(f"{a['id']} ERROR: {e}")
            failures.append(a["id"])

    write_generated_manifest(assets, state)
    log("DONE." + (f" failures: {failures}" if failures else " all requested assets ok."))
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()

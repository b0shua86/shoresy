#!/usr/bin/env python3
"""Shrink generated art for committing: downscale loose 4K textures to 2K and drop
the redundant unrigged mesh when a rigged version exists. Safe to re-run (idempotent).

Usage:
  python3 Tools/meshy/curate.py                      # curate all non-exp_ assets
  python3 Tools/meshy/curate.py Assets/Art/Generated/skater_home_captain ...
"""
import sys, os, glob
from PIL import Image

ROOT = "Assets/Art/Generated"
MAX = 2048  # max texture dimension to keep


def curate(d):
    d = d.rstrip("/")
    base = os.path.basename(d)
    if not os.path.isdir(d) or base.startswith("exp_"):
        return
    saved = 0
    # 1) downscale loose textures to <=2K
    for png in glob.glob(os.path.join(d, "model", "tex_*.png")):
        try:
            im = Image.open(png)
            w, h = im.size
            if max(w, h) > MAX:
                s = MAX / float(max(w, h))
                before = os.path.getsize(png)
                im.resize((max(1, int(w * s)), max(1, int(h * s))), Image.LANCZOS).save(png, optimize=True)
                saved += before - os.path.getsize(png)
        except Exception as e:
            print(f"  skip {png}: {e}")
    # 2) drop redundant unrigged mesh for rigged characters
    mfbx = os.path.join(d, "model", "model.fbx")
    if os.path.exists(os.path.join(d, "rigged", "character_rigged.fbx")) and os.path.exists(mfbx):
        saved += os.path.getsize(mfbx)
        os.remove(mfbx)
    print(f"{base}: reclaimed {saved/1e6:.1f} MB")


def main():
    targets = sys.argv[1:] or [p for p in glob.glob(os.path.join(ROOT, "*")) if os.path.isdir(p)]
    for t in targets:
        curate(t)


if __name__ == "__main__":
    main()

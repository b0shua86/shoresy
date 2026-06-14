# Meshy AI asset pipeline

Generates the game's 3D characters and props from the original, IP-free
descriptions in [`assets.json`](./assets.json) using the
[Meshy AI](https://docs.meshy.ai/) REST API.

- **Characters** run `text-to-3d preview (A-pose) → refine (PBR) → rigging`,
  producing a **rigged FBX/GLB plus walk & run animations** — ready to drop onto a
  Unity Humanoid avatar.
- **Props** run `text-to-3d preview → refine (PBR)`.

Output lands in `Assets/Art/Generated/<asset_id>/` and is committed to the repo.
Job state lives in `Tools/meshy/_work/state.json` (gitignored) so runs are fully
**resumable** — stop and restart any time without re-spending credits.

## Setup

You need a Meshy account with API access and credits. Provide the key either way:

```bash
export MESHY_API_KEY="msy-xxxxxxxx"
# ...or write it to a gitignored file:
mkdir -p Tools/meshy/secrets && echo "msy-xxxxxxxx" > Tools/meshy/secrets/meshy.key
```

The key is **never** committed (`.env`, `*.key`, and `Tools/meshy/secrets/` are
gitignored).

## Usage

```bash
# Validate the manifest and print the plan — no network, no key needed:
python3 Tools/meshy/generate.py --dry-run

# Generate just the essential tier-1 assets first (recommended to validate quality
# and credit cost before committing to the full batch):
python3 Tools/meshy/generate.py --max-priority 1

# Generate everything:
python3 Tools/meshy/generate.py

# One or a few specific assets:
python3 Tools/meshy/generate.py --only prop_puck,prop_net

# Check progress at any time:
python3 Tools/meshy/generate.py --status
```

Only the Python standard library is used — no `pip install` required.

## Credits / cost

Each character consumes credits for **three** Meshy tasks (preview + refine + rig);
each prop for **two** (preview + refine). The full manifest is ~14 characters and
~6 props. Generate `--max-priority 1` first, eyeball the results and the
`consumed_credits` reported per task, then run the rest. Failed tasks are refunded
by Meshy automatically.

## Importing into Unity

1. Generated files import automatically when Unity has focus. For each character,
   the primary file is `rigged/character_rigged.fbx`.
2. Select the FBX → **Rig** tab → set **Animation Type = Humanoid**, **Avatar
   Definition = Create From This Model** → Apply. (The `anim_walking` / `anim_running`
   clips can be retargeted onto the shared avatar, or used as references for the
   skating locomotion set.)
3. Materials: Meshy ships PBR maps as `model/tex_*.png` (base color, metallic,
   normal, roughness, emission). Create a URP/Lit material per asset and assign the
   maps, or let the editor importer tool (under `Assets/Editor/`, added with the art
   integration milestone) build them automatically from `generated.manifest.json`.

## Editing the roster

`assets.json` is the single source of truth. Each entry: `id`, `category`
(`character` | `prop`), `rig` (characters only), `priority` (1–3), Meshy options
(`topology`, `pose_mode`, `target_polycount`, `height_meters`), and a `prompt`
(≤600 chars). Keep all prompts original and free of any real names, shows, teams,
or trademarks.

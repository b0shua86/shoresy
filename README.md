# Barn Burner — arcade hockey (working title)

A fan-made, **arcade ice-hockey game** with small-town senior-league energy: big
hits, brawls, relentless chirping, and a bare-knuckle **fight minigame**, all
wrapped in a **broadcast-TV presentation** (scorebug, GOAL stings, instant replays,
crowd, jumbotron). Single player **vs AI** with difficulty adjustable mid-game.

> ⚠️ **Personal fan project.** Not for sale. All trademarks belong to their owners.
> All characters, teams, and assets in this repo are **original** and IP-free.

## Tech

| | |
|---|---|
| Engine | **Unity 6000.4.11f1** |
| Render pipeline | Universal Render Pipeline (URP) |
| Input | Unity Input System (gamepad + keyboard) |
| Camera | Cinemachine |
| Targets | **Windows + macOS** desktop (not web) |
| Art | Grounded-realistic, rigged humanoids generated via **Meshy AI** |

## Design pillars

1. **Feel before graphics.** Tight, responsive controls; satisfying skating, puck
   physics, hits, and shots.
2. **The opponent is beatable** and difficulty is tunable from an in-game menu.
3. **Your AI teammates are genuinely good** — they support the play, find lanes,
   make/accept passes, and back-check instead of all chasing the puck.
4. **Scalable team sizes** — N-v-N, default 3v3, scaling to 5v5.

## Repo layout

```
Assets/                 Unity project assets
  Art/Generated/        Meshy-generated models (committed)
  Scenes/               Scenes (Main bootstrap scene)
  Scripts/              Gameplay code (assembly-definition separated)
  Settings/             URP render pipeline + quality assets
ProjectSettings/        Unity project settings (pins editor 6000.4.11f1)
Packages/               UPM manifest
Tools/meshy/            Meshy AI generation pipeline (see Tools/meshy/README.md)
ROADMAP.md              Milestones and build sequence
```

## Getting started

1. Install **Unity 6000.4.11f1** via Unity Hub.
2. Clone this branch and open the folder as a Unity project. On first open Unity
   restores packages and regenerates the local `Library/` (gitignored); it may
   migrate package versions to those bundled with the editor.
3. Open `Assets/Scenes/Main.unity` and press **Play**. A bootstrapper constructs
   the rink, players, puck, camera, and HUD at runtime, so the game is playable
   without hand-wiring a scene.

> This repository was scaffolded in a headless CI-style environment without the
> Unity Editor (no editor launch, compile, art import, or platform builds happen
> there). All gameplay logic lives in C# under `Assets/Scripts/`; you build,
> playtest, and export Windows/macOS binaries locally.

## Generating art

See [`Tools/meshy/README.md`](Tools/meshy/README.md). In short:

```bash
export MESHY_API_KEY="msy-..."
python3 Tools/meshy/generate.py --max-priority 1   # essentials first
```

## Roadmap

See [`ROADMAP.md`](ROADMAP.md).

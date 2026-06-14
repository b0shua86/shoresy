# Roadmap

Build order favors **gameplay and feel first**, with the **Meshy art pipeline
running in parallel** so rigged characters are ready to drop in by the art
milestone. Each milestone is meant to leave the project in a playable/runnable
state.

Legend: ☐ todo · ◐ in progress · ☑ done

## M0 — Foundation
- ☑ Repo + Unity 6000.4.11f1 project pin, URP/Input System/Cinemachine manifest, `.gitignore`
- ☑ Meshy generation pipeline + IP-free asset manifest (`Tools/meshy/`)
- ◐ Assembly definitions and folder structure (`Core`, `Gameplay`, `AI`, `UI`, `Audio`, `Presentation`, `Editor`)
- ☐ Runtime **GameBootstrap** that builds a playable scene from code (rink, lights, camera, puck, players, HUD) + `Main.unity`
- ☐ Editor bootstrap that generates URP assets, tags/layers, and the Input Actions asset on load

## M1 — Skating, puck, camera (the feel)
- ☐ Skater controller: acceleration, edges/carving, momentum, turning, stopping (gamepad + keyboard)
- ☐ Puck physics (friction, bounces off boards), puck possession/dangling
- ☐ Pass / shoot / deke, one-timers
- ☐ Cinemachine broadcast follow camera
- ☐ Tuning surface (ScriptableObject) for all feel constants

## M2 — Match rules
- ☐ Team/roster model; scalable **N-v-N** (default 3v3 → 5v5)
- ☐ Faceoffs, goals + goal detection, periods + clock, whistles, stoppages
- ☐ Penalties + penalty box + power play; (optional) offside/icing toggle
- ☐ Match state machine + game flow

## M3 — AI (the #1 priority: good teammates, beatable opponent)
- ☐ Steering/positioning system: roles, zones, spacing (don't all chase the puck)
- ☐ Offense: support lanes, give-and-go, find open ice, accept passes
- ☐ Defense: back-check, mark, gap control, shot blocking
- ☐ Difficulty profiles (reaction time, accuracy, aggression, awareness) + **in-game pause menu** to change skill live
- ☐ Goalie AI: positioning, saves, rebounds

## M4 — Contact
- ☐ Checking / big hits / knockdowns + recovery
- ☐ Hit detection, momentum transfer, stumble/fall states

## M5 — Fight minigame
- ☐ Drop-the-gloves brawl: own control scheme (block/dodge/jab/haymaker/stamina)
- ☐ Clear win/lose + clean hand-off to and from the match (trigger, resolve, resume)

## M6 — Broadcast presentation
- ☐ Scorebug / lower-thirds HUD
- ☐ Animated **GOAL!** sting, instant replays (state recording + playback)
- ☐ Jumbotron, crowd audio reactions, chirp/commentary popups

## M7 — Setup & content
- ☐ Front-end menus: main, captain/roster select, settings, difficulty
- ☐ Roster of distinct characters with looks, numbers, and **signature chirps** (data-driven)
- ☐ Arenas: indoor **barn** + outdoor **pond rink**
- ☐ Audio: SFX, music, menu sounds; settings + **save** system

## M8 — Art integration (Meshy)
- ☐ Import rigged characters; configure Humanoid avatars
- ☐ Skating locomotion blend trees; retarget/author animations
- ☐ Materials from Meshy PBR maps (auto-build from `generated.manifest.json`)
- ☐ Reflective ice, lighting, post-processing pass

## M9 — Shipping
- ☐ Options + performance scaling, controller remap
- ☐ Windows + macOS build configs + automated build script
- ☐ Distribution (itch.io / personal)

---

### Parallel track — Meshy art (runs from M0 onward)
- ◐ Tier-1 essentials (a few skaters per team, both goalies, puck, net, player stick)
- ☐ Tier-2 roster depth (more skaters, away goalie, goalie stick)
- ☐ Tier-3 extras (coach, referee, trophy, zamboni)

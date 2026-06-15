# Roadmap

Build order favors **gameplay and feel first**, with the **Meshy art pipeline
running in parallel**. Each milestone leaves the project in a playable/runnable state.

Legend: ☐ todo · ◐ in progress · ☑ done

**Current status (mid-build):** a full match is playable from code — skating, puck,
passing/shooting, **body checking + stamina**, role-based AI lines, goalies,
faceoffs/periods/clock/score, HUD, and a pause menu with **live difficulty**. The full
Meshy roster (**14 characters + 5 props**) is generated, committed, and wired into the
match via a one-click editor builder. Up next: the **fight minigame**, **penalties**,
and broadcast presentation.

## M0 — Foundation
- ☑ Repo + Unity 6000.4.11f1 pin, URP/Input System/Cinemachine manifest, `.gitignore`
- ☑ Meshy generation pipeline + IP-free asset manifest (`Tools/meshy/`)
- ☑ Assembly definitions and folder structure
- ☑ Runtime **GameBootstrap** builds a playable scene from code (rink, lights, camera, puck, players, HUD)
- ☑ Editor bootstrap (`ProjectSetup`) creates `Main.unity` + build settings on load

## M1 — Skating, puck, camera (the feel)
- ☑ Skater controller: acceleration, edge grip/carving, momentum, turning, stopping (gamepad + keyboard)
- ☑ Puck physics + possession; stick-tip carry
- ☑ Pass / shoot
- ☑ Broadcast follow camera
- ☑ Tuning surface (`GameConfig` / `SkatingTuning`)
- ☐ Dekes / one-timers

## M2 — Match rules
- ☑ Team/roster model; scalable **N-v-N**
- ☑ Faceoffs, goals + detection, periods + clock, stoppages, phase state machine
- ☐ Penalties + penalty box + power play; offside/icing toggle

## M3 — AI (good teammates, beatable opponent)
- ☑ Steering/positioning: roles, anchors, only-the-closest-forechecks (no swarm)
- ☑ Offense: drive the net, pass options
- ◐ Defense: forecheck + structure (gap control / shot blocking still light)
- ☑ Difficulty profiles + **in-game pause menu** to change skill live
- ☑ Goalie AI: angle-based positioning (saves/rebounds still basic)

## M4 — Contact
- ☑ Checking / big hits / knockback + stun + puck pops loose
- ☑ Stamina (sprint + big hits burn energy; gassed = no sprint)
- ☐ Stumble/fall animation states

## M5 — Fight minigame
- ☐ Drop-the-gloves brawl: block / dodge / jab / haymaker / stamina
- ☐ Win/lose + clean hand-off to and from the match

## M6 — Broadcast presentation
- ◐ IMGUI scorebug, controls hint, GOAL/FACE-OFF/result stings, energy bar
- ☐ Animated **GOAL!** sting, instant replays
- ☐ Jumbotron, crowd audio, chirp/commentary popups

## M7 — Setup & content
- ☐ Front-end menus (main, roster select, settings)
- ☐ Distinct characters with numbers + **signature chirps** (data-driven)
- ☐ Arenas: indoor **barn** + outdoor **pond rink**
- ☐ Audio: SFX, music, menu; settings + **save**

## M8 — Art integration (Meshy)
- ☑ One-click builder: import FBX → Resources prefabs + locomotion controllers (**Shoresy ▸ Build Character Prefabs & Anims**)
- ☑ Runtime `SkaterVisual` binds models to skaters (capsule fallback); `CharacterRoster` mapping (player = home star)
- ◐ Skating locomotion blend (walk→run; idle clip + retarget polish pending)
- ☐ Team crest + jersey **numbers as decals** (consistent across the roster)
- ☐ Materials auto-build from `generated.manifest.json`; reflective ice; post-processing
- ◐ Hero/star: rig + finalize face (look-alike done; rigging in progress)

## M9 — Shipping
- ☐ Options + performance scaling, controller remap
- ☐ Windows + macOS build configs + build script
- ☐ Distribution (itch.io / personal)

---

### Parallel track — Meshy art
- ☑ Tier-1 essentials (skaters per team, both goalies, puck, net, stick)
- ☑ Tier-2 roster depth (more skaters, away goalie)
- ☑ Tier-3 extras (coach, referee, trophy, ice resurfacer)
- ◐ Hero/star (bulldog-crest look-alike; headband + rig pass in progress)

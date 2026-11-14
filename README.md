# Tiny Giant Problems

A cooperative creature-care game about tiny technicians working on enormous,
living fantasy animals. Built from the supplied v0.1 design document.

## Open and play

- Unity **6000.3.26f1** (Unity 6.3 LTS), URP.
- Install Git LFS and run `git lfs pull` after cloning to download the models,
  textures, generated meshes and audio. Then open this repository as a Unity project.
- Open `Assets/_TinyGiantProblems/Scenes/Mossback_FirstPlayable.unity`.
- Press Play, then **Board Mossback / Solo**.
- Complete three care jobs and return to the turquoise harness beacon to extract.

Keyboard: WASD move, mouse orbit, scroll zoom, Space jump, Shift sprint,
F grip/brace, 1–4 tool selection, hold left mouse to use, E extract,
R reel a tether, hold Backspace for recovery, Esc pause.

Gamepad: left stick move, right stick orbit, A jump, B grip/brace, shoulders
cycle tools, RT use, LT reel, X extract, Start menu. The fourth tool clips/releases
a tether to a nearby gold socket. Climbing uses hold grip + movement against a flank.

## Crew prototype

**Host Crew (LAN)** starts a four-person direct-IP session on UDP port 7777.
Other players enter the host machine's IPv4 address and select **Join Crew**.
Use `127.0.0.1` for multiple instances on the same PC. This does not provide
internet matchmaking, NAT traversal, Steam invitations, Relay, or host migration.
Allow the Windows firewall prompt for local-network testing if Windows requests it.

The host owns job completion and creature reactions. Local movement is predicted;
the prototype sends poses and host snapshots through NGO custom messages. It is
not yet a production movement-authority or anti-cheat implementation.

## Development

- `Tiny Giant Problems/Build Mossback First Playable` rebuilds the authored
  prototype scene from `MossbackBuilder`. Save unrelated work before using it.
- `Tiny Giant Problems/Build Windows Player` writes
  `Builds/Windows/TinyGiantProblems.exe`.
- `ExpeditionSmokeTest.Run()` explicitly runs integrated gameplay checks in Play
  mode and writes `TestResults/expedition-smoke.txt`.
- Generated binaries, Library, caches, logs and credentials are ignored.
- Large art assets and audio use Git LFS; keep LFS enabled when committing replacements.
- `MossbackReactionSmokeTest.Run()` checks the Animator behaviors, random reactions,
  braced attachment and animated collision in Play mode.
- `Tools/generate_audio.py` generates only missing audio and reads the API key
  from `ELEVENLABS_API_KEY`; prompts are recorded in `Docs/audio-provenance.json`.

## Scope and roadmap

The current Mossback expedition includes imported scenery and equipment, a fitted
harness, solid animated body collision and a 21-joint creature skeleton. An Animator
drives head turns, sneeze wind-up/release and random wet-dog shakes; the eyes stay rigid
to the skull. See [animation notes](Docs/Mossback-Animator-Behaviors.md).

The playable loop includes five zones, three care jobs,
warned sneeze/shake, bracing, a socket tether/reel, checkpoint recovery, extraction,
debrief, replay, keyboard/gamepad controls, and generated prototype audio.

Still requires player feedback and further engineering: final creature animation,
climbing polish, rope swinging and teammate rescue, robust network interpolation
and latency testing, complete remapping/accessibility settings, service-backed
online sessions, additional contracts and production art. No release-readiness
or commercial performance claim is implied.

Design: `Tiny Giant Problems/Tiny_Giant_Problems_GDD_v0.1.md`.
Implementation specification: `Docs/Mossback-Technical-Specification.md`.

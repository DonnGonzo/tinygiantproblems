# Development log

## 2026-11-10 — Mossback first playable

Created the Unity 6000.3.26f1 URP project at the requested local path and connected
the provided GitHub repository. Preserved both source design documents. Added a
concrete technical specification and reproducible scene/build tools.

Implemented third-person capsule movement, keyboard/gamepad actions, orbit camera,
five moving traversal zones, flank grip climbing and ledge recovery, eight authored
anchors, tether/reel, bracing, bounded launches and checkpoint rescue. Added three
tool-gated care jobs, comfort/irritation, alternating warned sneeze/shake events,
extraction, debrief, best-score storage, replay and pause options.

Created Mossback graybox geometry, meadow dressing, warm lighting, URP post effects,
uGUI/TMP menus/HUD, and five ElevenLabs audio assets. Higgsfield was checked but had
zero credits; no paid generation was submitted.

Added direct-IP four-person host/client sessions using NGO 2.14.0. The first
four-process check exposed a snapshot exceeding the 1264-byte unfragmented payload
limit; switched the snapshot channel to reliable fragmented delivery. This is a
prototype protocol, with client-predicted poses and host-owned care/state; network
bandwidth, interpolation and anti-cheat need later work.

Integrated Play-mode checks passed for moving-frame stability over 90 simulated
seconds, walking across zones, actual grip climbing, brace protection, bounded
launches, rescue grace, socket tethering, wrong-tool and distance rejection,
premature extraction rejection, all three care jobs, idempotent rewards, debrief,
replay reset and reaction warning duration. The test harness is committed; raw
run outputs are local under `TestResults`.

The Windows development build is generated under `Builds/Windows`. Generated
builds are intentionally excluded from source control. Initial source/audio push:
`90c3abd` on `main`.

## Next design gates

- Human playtests: readability, traversal feel, repeated expedition enjoyment.
- Two physical machines, artificial latency, disconnects during interaction and
  sustained four-player runs. Same-PC connection tests do not prove these.
- Production animation and creature silhouette after traversal validation.
- Rope swinging, teammate rescue, full rebinding/accessibility and richer job verbs.
- Internet session/invite integration and Steam setup when the local loop is ready.

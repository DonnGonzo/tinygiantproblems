# Mossback first playable

Source of truth: `Tiny Giant Problems/Tiny_Giant_Problems_GDD_v0.1.md`.
Pinned editor: Unity 6000.3.26f1; Universal Render Pipeline; Windows x64.

## Playable contract

**A Very Bad Itch.** Board Mossback from the guild harness. Traverse five named
zones, vacuum Bramblemites, groom tangled fur, soothe an irritated patch, and
return to the harness to extract. Complete all three jobs and restore comfort.
Warned shakes and sneezes interrupt work. Bracing prevents launch; a socket tether
catches falls and reels the technician back. Checkpoint recovery prevents softlocks.

## Scene and ownership

- Expedition: mission phase, work progress, comfort, irritation, reaction schedule.
- MossbackFrame: gentle kinematic translation/yaw; simplified traversal colliders.
  - Harness, Hindquarters, Spine, Shoulder, Head: stable zone transforms.
  - Eight named anchor sockets; three job sockets; extraction marker.
  - Visual body, four legs, face, fur tufts and harness dressing.
- Technician: controlled capsule motor, input actions, camera target, equipment.
- OrbitCamera: camera obstruction test, adjustable distance and field of view.
- ExpeditionHUD: uGUI/TMP contract, vitals, contextual prompt, reaction caption,
  tool selection, briefing, pause/settings and debrief.
- Meadow: distant hills, trees, clouds and migration landmarks.

## Simulation boundaries

Player movement applies support-frame displacement before capsule movement.
Ground probes discover the current support. Detaching preserves bounded launch
velocity. Visual breathing never alters traversable collider scale. Grounded,
climbing, airborne, braced and tethered states are explicit; reset clears all
transient state. Reactions have warning, impact, recovery and cooldown phases.

Jobs validate distance, selected tool and reaction/brace state. Progress is clamped
and completion is idempotent. A single mission authority owns rewards and state.
Networking uses Netcode for GameObjects plus Unity Transport for direct host/join;
Relay/Steam integration is a later service milestone requiring project setup.

## Initial tuning

Walk 5 m/s; sprint 8 m/s; climb 3 m/s; jump 7 m/s; gravity 20 m/s².
Grip pool 100; warning 4 seconds; reaction cooldown at least 24 seconds.
Tool reach 3 m; each job requires 6 seconds of uninterrupted accumulated care.
Comfort starts at 38; each job adds 20; extraction threshold 70.
Solo recovery is unlimited and costs score only. No permanent equipment loss.

## Acceptance checks

1. Live Editor frames advance while unfocused, no compile/runtime errors.
2. Supported player remains on a moving/rotating frame for 90 simulated seconds.
3. Climb, jump, brace, launch, tether catch/reel and checkpoint reset stay finite.
4. All three jobs reject wrong tools and out-of-range requests; no double reward.
5. Warnings precede impulses; bracing blocks them; recovery grants grace.
6. Contract reaches extraction/debrief and resets without an Editor restart.
7. Windows build launches; keyboard/mouse and gamepad mappings exist.
8. Host/client smoke test verifies shared jobs and reaction state before claiming
   co-op is validated. Four-person latency/long-session testing remains external.

## Art and audio policy

Use authored procedural graybox geometry with a readable storybook palette.
Do not mistake prototype geometry for final creature art. Generate only a small
ElevenLabs audio set, retain prompts and provenance, and never commit API keys.
Higgsfield account checked: zero credits; no paid generation submitted.

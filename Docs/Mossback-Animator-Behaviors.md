# Mossback animator behaviors

The creature now uses an Animator Controller at `Assets/_TinyGiantProblems/Art/MossbackRig/Animation/Mossback.controller` with five editable clips/states:

- **LookAround:** 16-second idle with head/neck turns totaling approximately 26 degrees in either direction, held looks, nods and chewing.
- **SneezeWindup:** four-second inhale, raised head, jaw movement and ear preparation.
- **Sneeze:** 1.65-second forward snap, recoil and recovery; nose puff follows the snap and reaction audio plays on release.
- **ShakeWindup:** four-second bracing/lowering motion.
- **WetDogShake:** 3.4-second damped oscillation, approximately 3.1 Hz, with full-body roll and staggered head, ear and tail motion. It transitions back to idle.

Clips animate pose controls on `MossbackMotion`; `CreatureFrame` and `MossbackSkeleton` apply those controls together before technician movement. The harness follows the body and hoof targets stay planted during reactions. Animated collision refreshes each frame during reactions. Wider neck, ear and jaw skin transitions support the increased motion without the sharp folds from the earlier weights; sampled maximum edge stretch is below 2x.

The host now randomly selects sneeze or shake, instead of alternating the two labels. The first warning arrives 26–39 seconds after starting; later warnings are scheduled 26–43 seconds after impact. Existing four-second warnings, brace/tether behavior and bounded launch impulses are preserved. Pause stops the countdown; replay cancels animation and pending particles. Connected crew receive the warning/impact animation through the existing authoritative snapshot flow; this change does not introduce a second client-side random scheduler.

All 15 dedicated live behavior checks pass: both head-turn directions, sneeze anticipation/release/puff/recovery, distinct shake state, body oscillation, planted hoof targets, braced player attachment, collision synchronization, random scheduling and selection, pause, and replay cancellation. Results: `TestResults/mossback-reactions.txt`.

All 17 full expedition regression checks also pass. The updated rig retains normalized skin weights and solid animated collision. Expanded-pose checks report maximum sampled edge stretch of 1.975x and less than 0.00005 local units of drift in fully rooted vertices. The broader weights retain the deck-bearing region while allowing the forehead and ears to follow the larger turns.

Preview: `Screenshots/Mossback-Behaviors-Preview.mp4` (30 seconds, 24 fps, offline poses from the same Animator clips).

The updated Windows player is available at `Builds/Windows/TinyGiantProblems.exe`.

November 14 eye correction: the eye and eyelid regions now use rigid head-bone weights, with a broad blend into the cheeks. This removes the apparent eye wobble caused by mixed neck/ear weights while retaining the approved head turns, sneeze and shake clips. Expanded-pose validation measures less than 0.000005 local units of eye drift relative to the skull. Left/right preview stills were inspected; the earlier behavior video predates this eye correction.

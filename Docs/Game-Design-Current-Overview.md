# Tiny Giant Problems
## Consolidated game design and development record

Prepared November 16, 2026. This overview reconciles the original November 10 design with the supplied models, subsequent design requests, and recorded implementation through November 14. It is a project reference, not a release announcement.

The companion complete document contains this current overview, the entire original design proposal, and the detailed development records. Read this overview first: it supersedes older implementation descriptions where the project changed. Historical proposals remain proposals even when written in the future tense. Recorded test results are previous runs, not tests rerun for this document.

## 1. The game we are making

Tiny Giant Problems is a third-person cooperative fantasy creature-care game. Players are tiny technicians from the Small Works Guild, climbing enormous friendly creatures to solve small problems that become enormous physical adventures. The creature is both a character and the level. Caring for it is the goal; sneezes, shakes, awkward equipment and exposed routes provide affectionate comedy.

The defining moment is a team working on Mossback, noticing a sneeze coming, scrambling to brace or tether, and recovering together when somebody gets launched. The humor should come from understandable consequences and the difference in scale. The creature should remain appealing and expressive rather than cruel, frightening or deliberately ridiculous.

The five original pillars are: a living level; funny, recoverable mistakes; cooperation without fixed classes; care rather than harm; and simple controls with room for mastery. The original tone target was 70% affectionate absurdity, 20% wonder and 10% low-stakes danger. These are creative guidelines, not measured production requirements.

**Platform and scope:** Windows PC is the current build target. Steam is the proposed distribution platform. Solo and a four-person LAN prototype exist. The original eventual 2-6 player ambition is not six-player support today. Standard contracts of 20-35 minutes and quick contracts of 10-15 minutes are design targets, not measured durations of the current three-job mission.

## 2. Scope and status at a glance

| Area | Current implementation | Intended expansion / unresolved work |
|---|---|---|
| Creature | One playable Mossback with skeleton, collision and reactions | Additional species; migrating routes; richer mood-driven behavior |
| Mission | A Very Bad Itch: three jobs, extraction, debrief, replay | Contract board, optional jobs, varied work orders and difficulty |
| Players | Solo and four-person direct-IP LAN prototype | Service-backed online sessions, invites, latency/rejoin hardening; six players is stretch |
| Tools | Vacuum, groomer, sprayer, socket tether/reel | Scanner, glider, winch and other modeled equipment need gameplay systems |
| Movement | Walk, sprint, jump, flank climbing, brace, tether, recovery | Swinging, teammate rescue, carrying, richer transitions and polish |
| World | Imported forest/camp props and fitted wooden harness | Route readability, visual consistency, performance and final art polish |
| Animation | Mossback 21 bones and five Animator states; bramblemite 18 bones | Final animation quality, traveling locomotion, more behaviors |
| Progression | Mission score and local best-score storage | Guild Pay, upgrades, cosmetics, trust and unlockable contracts |
| Release | Local Windows development build | External playtests, production networking, performance and release gates |

An imported model does not establish that its gameplay feature exists. The glider, scanner, rescue winch, snack launcher, baby creature and workshop props should not be presented as fully playable systems simply because their art is available.

## 3. The current playable expedition

**Contract: A Very Bad Itch.** Start from the briefing, board Mossback, travel through the harness and body routes, complete care, then return to the turquoise extraction beacon. The five authored zones are Harness, Hindquarters, Spine, Shoulder and Head. Eight named anchor sockets support the tether system.

The playable flow is Briefing -> Working -> Extraction -> Debrief -> Replay. Three care jobs use the matching tools: vacuum Bramblemites, groom tangled fur, and soothe an irritated patch. Wrong tools, excessive distance and blocked actions cannot advance treatment. Care completion is clamped and cannot award the same job twice.

**Current code rules:** Treatment range is 3.2 world units. A job requires six seconds of accumulated valid care; each update contributes at most 0.1 seconds. Comfort begins at 38 and increases by 20 per completed job, reaching 98 after all three. Irritation begins at 30 and falls with completed care. Extraction requires exactly three completed jobs and comfort of at least 70; the technician must also reach the extraction point. These rules take precedence over rounded values in the original first-playable specification.

The score calculation is max(0, completed jobs x 100 + 150 - recoveries x 25 - floor(max(0, elapsed seconds - 480) / 10)). A full run without recovery penalties or overtime therefore scores 450. This is a prototype score, not the proposed Guild Pay currency. Local best score is stored through PlayerPrefs under GuildBest.

Recovery is available to prevent getting stuck; it costs score rather than permanently losing equipment. A short grace period protects the player from an immediate repeat launch. Replay clears mission progress, reactions and transient state rather than requiring an Editor restart.

## 4. Controls, traversal and player safety

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move / orbit | WASD / mouse | Left / right stick |
| Zoom | Mouse wheel | See current settings and camera behavior |
| Jump / sprint | Space / Shift | A for jump |
| Grip / brace | Hold F | B |
| Select tool | 1-4 | Shoulder buttons |
| Use tool | Hold left mouse | RT |
| Reel tether | R | LT |
| Extract | E near beacon | X |
| Checkpoint recovery | Hold Backspace | Recovery interface |
| Pause / menu | Esc | Start |

The fourth tool clips or releases a tether at a nearby authored gold socket. This is an existing safety tether and reel, not a completed free-swing grappling simulation. Climbing uses grip plus movement against valid flank surfaces. Walk speed is 5 m/s, sprint 8 m/s, climb 3 m/s, jump velocity 7 m/s and gravity 20 m/s squared in the first-playable tuning. Grip has a 100-point pool. These values remain tuning parameters.

The motor uses controlled capsule movement and moving-frame support rather than letting an uncontrolled rigid body determine normal walking. Support displacement is applied before player movement. Physics synchronization and zero CharacterController minimum-move distance address small-frame motion. When released, the character receives a bounded impulse. Bracing/tether protection and recovery grace provide counterplay.

The intended fairness contract is unchanged: every major reaction needs a readable warning, a safe response, a bounded consequence and a recovery route. Full remapping, hold/toggle options, accessible timing, high-contrast markers and comprehensive camera comfort controls remain a completion checklist, not a claim that every option is finished.

## 5. Mossback: appearance, skeleton and behavior

The user's direction evolved from a very goofy face toward the original face with a more toonish treatment. A wolf-like alternative was rejected. The body should retain enough fur/moss detail for burr clusters and care targets to fit naturally. Harnesses and equipment are separate fitted objects, not baked into the source creature. Mossback was enlarged to three times the previous scale.

The replacement body is fitted to approximately 18 x 18 x 42 local units under a 3x frame, giving an approximate 54 x 54 x 126 world-unit envelope. The supplied source geometry is preserved. The imported body is skinned to a 21-bone rig: spine, neck, head, jaw, ears, tail and four hip/knee/foot chains. Head turns, breathing, weight shifts, hoof settling and restrained chewing bring the stationary creature to life. A traveling walk cycle is not implemented. The closed source mouth limits convincing wide-open mouth motion without further modeling.

| Animator state | Duration | Visible action |
|---|---:|---|
| LookAround | 16 seconds | Held left/right looks, nods and chewing; roughly 26 degrees total head/neck turn each way |
| SneezeWindup | 4 seconds | Inhale, head raise, jaw/ear anticipation |
| Sneeze | 1.65 seconds | Forward snap, recoil, nose puff and recovery |
| ShakeWindup | 4 seconds | Lowering and bracing before the shake |
| WetDogShake | 3.4 seconds | Damped full-body oscillation with staggered head, ears and tail |

The host randomly selects a sneeze or shake. The first warning is scheduled 26-39 seconds after starting; later warnings arrive 26-43 seconds after impact. Existing four-second warnings remain. Pause stops the countdown, and replay cancels pending animations and particles. Connected players receive host warning/impact state; clients do not choose independent random reactions.

Eye movement was explicitly removed after it looked like a glitch. Eye and eyelid vertices now follow the head rigidly while the surrounding cheek weights blend smoothly. This preserves the approved head turns, sneeze and wet-dog shake without apparent eye wobble.

The render mesh and simplified animated collision are separate. The body collision uses about 12,000 triangles, refreshes at 20 Hz while idle and each frame during reactions. Harness and stable traversal frames move with the creature. This is a deliberate implementation change from the original proposal's mostly primitive/convex collision strategy, and needs continued profiling.

## 6. Bramblemite and solid world objects

Bramblemite is the small whimsical pest used by the vacuum care job: a rounded red/orange shell with bramble-like nubs, six legs, two antennae and a readable friendly face. The imported asset slug is spelled bramblenmite in some paths; the creature name is Bramblemite.

Its 18-joint skeleton consists of shell/thorax, head, two two-joint antenna chains and six hip/knee leg chains. Idle movement includes leg taps, antenna motion and gentle body/head motion. Vacuuming increases wriggling. It is currently a stationary care target, not roaming pest AI. A separate 2,500-triangle skinned collision mesh follows at 20 Hz. Finishing treatment disables its collision as the care visual shrinks; replay restores it.

The latest collision pass records 295 solid world prop instances, covering trees, rocks, plants, camp furniture, supplies, care targets and equipment. Simplified non-convex mesh colliders are capped at 2,500 triangles per source mesh and retain openings such as rock arches. Harness rails and braces also collide.

There are 58 deliberate exceptions: held/worn art must not collide with its own technician, and deck/ladder art uses existing fitted traversal collision. Solid scenery is not the same as movable rigid-body scenery. Openings must remain passable, and solid props must not block the intended route.

## 7. Map, camp, harness and art pipeline

The original rough placeholder dressing has been replaced with imported forest scenery, rocks, plants, a winding dirt trail and a planted camp clearing. Camp dressing includes boarding structures, workshop furniture, chest/tool storage, extraction equipment, a survey drone, baby creature and glider display. The camp is not yet a complete contract-selection or upgrade hub.

The harness uses split wooden deck modules fitted to the back, a level starting landing, side approaches and rope ladders. Rope handrails, brass collars, timber supports and ladder-side ropes make the construction read as connected equipment. Continued polish should check floor continuity, ladder alignment, edge clearance, camera obstruction and whether route direction is obvious to a new player.

User-provided GLBs and named reference images live in GiantTiny. Several files contain multiple items: trees, rocks, cosmetics, furniture, plants and decking must be separated before placement. Do not instantiate a whole source assortment where only one item belongs. Recent replacements applied to the game include Mossback, tangled fur, decking/rope ladders, spare canister, rope launcher, extraction beacon and glider. A mountain source asset is available but was not applied in the recorded map pass.

The import tool extracts OBJ geometry and textures, builds URP materials and prepares reusable prefabs. Source models and hashes are retained. Color, normals, occlusion and metallic/smoothness handling belong to the pipeline rather than manual per-instance material edits. Replacements need orientation, scale, pivot, material, collision, rig and placement review. A newly imported GLB does not automatically inherit a valid skeleton or correct kit segmentation.

**Art direction:** tactile storybook fantasy, readable silhouettes, rounded friendly forms, practical handmade guild tools, mossy natural surfaces, timber, rope and brass. Detail should support care targets and scale rather than obscure eyes, paths or tool silhouettes. Production optimization remains necessary: some replacement meshes are around 700,000 triangles and repeated detailed decking is expensive. LODs, texture budgets and measured frame-time targets remain work.

## 8. Full-game progression and content vision

The original macro loop is Guild Base -> select contract -> choose gear -> board -> traverse and care -> react and rescue -> extract -> debrief -> earn rewards -> unlock gear/contracts -> repeat. Today the middle expedition loop exists; the surrounding persistent hub/economy does not.

The original design proposes Guild Pay, separate care grades, cosmetics, species trust and horizontal equipment specialization. Example paths are Rigger, Care Specialist, Rescuer and Collector; these are flexible loadout directions, not locked classes. Starter gear must remain sufficient for mandatory jobs. Upgrade prices, exact unlock order, balance and save schema have not been finalized. When asked to choose the next workstream, the user chose map/harness polish over adding progression; no later proposal should be mistaken for an implemented upgrade economy.

Six planned job families are parasite removal; fur/shell maintenance; soothing comfort care; rigging/repair; search/retrieval; and emergency response. The current three tasks are a narrow first sample. Proposed additional tools include grip gauntlets, multi-grabber, portable scanner, wind glider, emergency anchor and snack launcher, alongside richer versions of the current tools.

Mossback is the first creature. Shellstrider is the proposed second: a tortoise-like mountain wanderer with shell cliffs, cracks, roots and storm-driven route changes. Skywhale is an aspirational later airborne creature with fin-wings, gliding and wind hazards. Neither is playable. Every added species should reuse the care/traversal framework while adding one movement hazard, one traversal material, two jobs and a signature behavior.

Contracts should vary authored job sockets, mood, weather, pests and optional objectives rather than generate unreliable climbing terrain. Tick Season, Heavy Mist, Long Migration, Sleepy Giant, Festival Delivery and Family Day are original modifier concepts. Release content counts are unresolved: the original document mentions both a three-giant aspiration and a focused two-giant launch option. No release roster or date is committed.

## 9. Technology, networking, audio and interface

The project pins Unity 6000.3.26f1. The current manifest includes URP 17.3.0, Input System 1.20.0, Netcode for GameObjects 2.14.0, Cinemachine 3.1.7 and Test Framework 1.6.0. Package presence does not mean every proposed integration is used. The playable scene is Assets/_TinyGiantProblems/Scenes/Mossback_FirstPlayable.unity.

Runtime responsibilities are divided among Expedition (mission phase, care, mood, events and rewards), CreatureFrame (moving support), TechnicianMotor (controlled traversal), care rules/targets, HUD/input/camera, creature pose controls and skeletons, and the LAN session/message layer. Mossback Animator clips drive MossbackMotion controls; CreatureFrame and MossbackSkeleton consume them before technician movement. BramblemiteSkeleton drives the smaller rig procedurally.

The four-person LAN prototype uses direct IPv4 host/join on UDP 7777. The host owns shared care completion and creature events, while local movement is predicted and player poses/host snapshots use NGO custom messages. An early payload-size issue was addressed with reliable fragmented snapshot delivery. This remains a prototype protocol, with interpolation, bandwidth, authority and latency work outstanding.

No current claim is made for Relay, internet matchmaking, Steam invites, NAT traversal or host migration. Same-PC connection checks are not a substitute for long sessions on two physical machines or simulated poor connections. Rejoin behavior, disconnects while interacting and synchronized animated collision need explicit regression coverage as systems grow.

The HUD/menu stack uses uGUI/TMP for briefing, objectives, vitals, contextual prompts, reaction captions, tools, pause/settings and debrief. Generated prototype audio and provenance are recorded locally. The full audio vision includes anticipatory inhale, rope strain, wind, footsteps, soft creature vocals, impacts and readable event captions. Essential information must remain understandable without voice chat. Full audio mix controls, zone-specific soundscapes and the complete accessibility plan remain production tasks.

## 10. Development workflow and evidence

Open the project with the pinned Unity version after pulling Git LFS assets. Open Mossback_FirstPlayable and enter Play mode; choose Board Mossback / Solo, or Host Crew / Join Crew for LAN testing. The menu command Tiny Giant Problems / Build Windows Player produces Builds/Windows/TinyGiantProblems.exe. Build Mossback First Playable regenerates the authored scene and may replace manual scene work; preserve unrelated edits first.

| Pipeline / entry point | Purpose |
|---|---|
| Tools/import_gianttiny.py | Import supplied models, split kits and prepare geometry/textures |
| Tools/build_mossback_collision.py | Generate simplified Mossback body collision |
| Tools/build_prop_collision.py | Generate cached simplified prop collision meshes |
| WorldCollisionBuilder.ApplyLibrary() | Restore prefab solids and Bramblemite rig |
| WorldCollisionBuilder.ApplyScene() | Apply scene collision policy |
| MossbackRigBuilder / BramblemiteRigBuilder | Build runtime-ready skeleton/skin assets |
| ExpeditionSmokeTest.Run() | Validate the core mission/traversal/replay loop |
| MossbackReactionSmokeTest.Run() | Validate animation, reactions, timing and collision |

Recorded validation includes 17 expedition checks and 15 Mossback reaction checks passing. World-prop probes cover trees, rocks, arch, workbench, chest, beacon, Bramblemite and canister. Arch tests confirm an open center and solid sides. Bramblemite checks cover normalized weights, bounded motion, increased movement under vacuum, completed-target collision disable and replay restore. The last recorded Windows build succeeded with zero errors and warnings.

These are implementation/regression results, not proof of commercial readiness, visual polish, 60 FPS, accessibility completeness or robust online multiplayer. Human playtesting and representative hardware/network profiling remain essential.

GitHub repository: https://github.com/DonnGonzo/tinygiantproblems. The recorded pushed main snapshot is 7ce6130, containing imported map assets and Mossback behaviors. The working tree still contains the subsequent prop collision and Bramblemite changes. This document does not commit or push them. Builds, screenshots, test outputs and generated PDF output are local artifacts; editable design records live under Docs. Large model/texture/mesh/audio assets use Git LFS.

## 11. Next development gates

These are recommended priorities based on the current gaps, not newly approved feature commitments or dated promises.

1. **Finish a convincing Mossback route.** Audit every ladder, platform edge, solid prop and camera angle in a real playthrough. Remove snag points and unsupported-looking construction; verify head turns and shakes with a player on the harness.
2. **Profile the actual scene.** Establish a representative PC baseline, identify expensive render meshes/materials/shadows and animated collision work, then add simplification/LODs without changing silhouettes or openings.
3. **Validate cooperative reliability.** Test two physical machines, then four players, with latency, disconnects, replay and repeated reactions. Confirm care cannot duplicate and recovery never softlocks.
4. **Improve care and rescue depth.** Make tasks satisfying beyond holding a button; choose one cooperative rescue mechanic and validate it before adding many tools or creatures.
5. **Implement the persistent loop deliberately.** Agree on a small contract set, reward model and first meaningful upgrades, then add versioned saves and a functional camp interface.
6. **Build the vertical slice.** Onboarding, accessible settings, final animation/audio feedback and an enjoyable repeatable session should precede a second giant or public release promise.

Release gates from the original design include new players understanding the objective unaided, noticing warnings, recovering from mistakes, voluntarily requesting another run, and completing stable four-player sessions. Original 60 FPS / 1080p and midrange hardware assumptions are targets to measure, not published minimum specifications. The $14.99 premium price was a hypothesis, not an approved price. Early Access versus focused 1.0 is still open.

## 12. Decisions to preserve and questions still open

**Preserve:** tiny workers caring for a giant; affectionate physical comedy; Mossback first; three-times creature scale; toonish original-face direction rather than wolf-like or excessively goofy; body detail for burrs; separate fitted harness; solid body and world props; expressive head/sneeze/shake movement; rigid eyes; animated Bramblemite; readable warnings and recovery; kit separation before placement.

**Open:** how far Mossback travels during a mission; final care-task complexity; exact rescue mechanics; solo assistance; final progression and upgrade economy; second-creature timing; online transport/services; final player cap; release roster, price and schedule; measured hardware budgets. Preserve these as questions instead of filling gaps with invented agreements.

## 13. Reading the supporting records

Part II preserves the complete original GDD, including worldbuilding, creature concepts, economy proposals, technical architecture, risks, milestone gates and playtest strategy. Its external references and technology recommendations are historical material from that document, not freshly verified vendor guidance.

Part III preserves technical implementation notes and development records in source order. Each retains its original context. Early references to graybox art, an unrigged 40,000-triangle Mossback, alternating reactions or missing body collision describe earlier stages and are superseded by this overview and the later replacement/rig/animation/collision notes. The original asset checklist is a production history, not an accurate list of everything still missing.

This document draws on the repository's original GDD, README, technical specification, art/import notes, animation and collision records, current CareRules.cs and Packages/manifest.json, plus the user's direction in the project conversation. It preserves the source documents rather than rewriting historical proposals as completed features.

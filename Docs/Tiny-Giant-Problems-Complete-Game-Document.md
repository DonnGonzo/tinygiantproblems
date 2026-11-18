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


# Part II | Original design proposal (historical)

# TINY GIANT PROBLEMS

**Game Design Document & Unity Technical Architecture — Version 0.1**  
**Prepared:** November 10, 2026  
**Status:** Preproduction concept / design hypothesis, not a production commitment  
**Target:** Windows PC / Steam; 1–6 players (first network target: 4)  
**Genre:** Cooperative fantasy expedition / physics comedy / creature care

> **One-line pitch:** You're tiny creature-care technicians working on enormous, living fantasy animals. Climb them, treat their problems, manage their moods, and get everyone off before a sneeze sends your entire crew flying.

---

## 1. Executive Summary

*Tiny Giant Problems* (working title) is a session-based cooperative adventure in which players traverse and maintain massive creatures as those creatures migrate through fantasy landscapes. Each giant is both a character and a playable level: its fur, shell, wings, skin, and equipment provide routes, work sites, hazards, and opportunities for improvised rescue. A mission begins with a mundane care contract and ideally escalates into a memorable chain reaction caused by player choices, creature behavior, and tactile tools.

**Commercial promise:** A visually immediate hook for a Steam capsule/trailer (tiny people doing jobs on a colossal animal), easily explained objectives, funny emergent moments for friend groups and streamers, and enough progression and creature variety to support repeat sessions. **Commercial uncertainty:** Neither demand nor revenue has been validated. The game must demonstrate repeatable fun with graybox mechanics before expensive art production.

**Recommended first playable:** Four players on **Mossback**, a gentle, fur-covered quadruped. Complete three maintenance tasks, survive a predictable set of reactions, rescue an endangered teammate, and extract. If that is entertaining at low visual fidelity, expand. If not, revise the mechanics before building additional creatures.

### 1.1 Product brief

| Dimension | Initial design target |
|---|---|
| Platforms | Windows PC via Steam at launch; controller and keyboard/mouse support |
| Players | Solo-capable; 2–6 co-op eventual; validate 2–4 before expanding to 6 |
| Perspective | Third-person with shoulder/orbit camera; adjustable FOV and camera distance |
| Session length | 20–35 minutes per standard contract; 10–15 minute quick contracts |
| Monetization | Premium one-time purchase; $14.99 is a *testable pricing hypothesis*, not a fixed decision |
| Tone | 70% affectionate absurdity, 20% wondrous adventure, 10% low-stakes danger |
| Content at prototype | 1 giant; 1 route; 3 job types; 4-player co-op; 4 tools |
| Content at 1.0 aspiration | 3 distinct giants, 6+ mission templates, meaningful upgrade choices, cosmetics and modifiers |
| Primary success test | A playtester wants a second expedition and can describe an unscripted funny moment |

### 1.2 Design pillars

1. **The level is alive.** Giant movement changes traversal and action windows. Players read breathing, motion, sound, and mood rather than following static obstacle courses.
2. **Useful mistakes are funny.** A careless action causes an understandable physical consequence, creates new decisions, and produces an opportunity for a save—not a random, unavoidable failure.
3. **Cooperate without fixed classes.** Every player can perform essential tasks; different tools and loadouts create improvised roles and teamwork.
4. **Care is the objective, chaos is the obstacle.** Players help the giant rather than harvest or injure it. This emotional connection differentiates the fantasy.
5. **Three minutes to understand; thirty missions to master.** The controls and goals are readable immediately, but safe routing, tool combinations, behavior knowledge, and upgrades provide depth.

### 1.3 What the game is not

It is not an open-world creature survival MMO; an elaborate creature anatomy simulator; a fully destructible open-world physics sandbox; an extraction shooter; a horror game; or a competitive griefing game. These ideas might be technically impressive but distract from the core co-op loop.

---

## 2. Fantasy, World and Player Identity

Players belong to the **Small Works Guild**, a crew of miniature technicians hired to treat migratory colossi. Villages and ecosystems depend on these creatures; many giants are gentle but cannot groom or maintain themselves. Each mission is presented as a guild contract, accompanied by a delightfully overconfident instruction manual and an NPC dispatcher.

**Art direction:** Storybook fantasy with tactile 3D materials and slightly exaggerated proportions. Fur acts like swaying grass, whiskers like suspension cables, scales like cliffs, and the giant's silhouette reads even at thumbnail size. Color-coding and material patterns identify safe climb zones, job zones, and hazards without relying solely on UI markers.

**Emotional rhythm:** Awe on first seeing the giant → small confidence while climbing → an avoidable mistake → a wild group recovery → relief at finishing the treatment → laughter at debrief. Failure is slapstick and recoverable; players should feel protective toward the creature.

**Boundaries:** Injury treatments are fanciful and non-graphic. Parasites resemble whimsical monsters or pests, not medically realistic closeups. The game avoids realistic suffering or cruelty to animals.

---

## 3. Core Session Loop

### 3.1 Macro loop

**Guild base → select contract → choose gear → board the giant → traverse → perform jobs → react to creature events → extract → evaluate care quality → earn currency/experience → unlock gear/contracts → repeat.**

### 3.2 Standard 25–30 minute contract

| Phase | Time target | Player decisions | Designed feeling |
|---|---:|---|---|
| Briefing & loadout | 2–4 min | Pick tools, route, optional risky bonus | Anticipation |
| Boarding & reconnaissance | 3–5 min | Find anchor spots, assess mood, scan targets | Scale and wonder |
| Primary work | 8–12 min | Sequence jobs, coordinate hands/ropes, control risk | Competence and teamwork |
| Escalation | 5–7 min | Respond to movement, roaming pests, changing weather | Comic panic |
| Exit & debrief | 3–5 min | Reach extraction, save friend/equipment, receive grade | Relief and storytelling |

**Important:** This is a pacing target, not a rigid scripted timeline. Events trigger from state, risk, and mission phases, with anti-spam guardrails. Players should sometimes complete a calm mission, making dangerous ones more memorable.

### 3.3 Mission structure

A contract contains **2–4 primary jobs**, **0–2 optional jobs**, a **soft mission clock**, a **care-quality objective**, and an **extraction condition**. Mission length comes from route distance, job complexity, weather, and creature temperament—not simply inflating health bars. Some contracts allow early extraction at reduced rewards.

### 3.4 Failure, rescue, and accessibility of fun

Players can be **stable**, **dangling**, **launched**, **incapacitated**, or **recovered**. A fall should first become a short rescue sequence (rope catch, glider recovery, teammate winch). When rescue is impossible, the player respawns at an expedition checkpoint after a short delay with a limited equipment penalty; never force a friend to spectate for twenty minutes. A team fails if it abandons mandatory jobs or allows critical creature stress to remain unmanaged at extraction, not because one person misclicked once. Solo mode grants an additional safety tool and gentler event timings.

---

## 4. Player Movement and Physical Interactions

### 4.1 Movement model

The character has **grounded**, **climbing**, **hanging**, **swinging**, **airborne**, **ragdoll**, and **recovery** locomotion states. Default movement is reliable and responsive; physics chaos is introduced through *specific interactions* rather than making walking unpredictably slippery. Character movement uses a controlled motor and casts against simplified collision surfaces. Ragdoll activation is temporary and reserved for obvious major impacts.

| Capability | Player action | Costs / limitations | Coordination opportunity |
|---|---|---|---|
| Walk/sprint | Navigate fur, skin and platforms | Sprint reduces grip recovery briefly | Carry supplies |
| Grip climb | Hold grip button near valid material | Stamina consumption, reduced in safe zones | Spot footholds |
| Anchor/rope | Place preapproved anchor, deploy rope | Limited reel length / anchor inventory | Create group shortcuts |
| Swing | Jump from tensioned rope | Momentum and timing | Catch falling teammates |
| Deploy glider | Reduce fall speed, steer to safe ledge | Slow to unfold, poor in high wind | Midair recovery |
| Carry/drag | Transport tools, supplies, pests | Occupies hands; heavy objects need two players | Natural slapstick |
| Brace | Clamp to anchor or fur | Temporarily unable to work | Survive known shakes |

**Design rule:** Any major launch requires a readable tell and a plausible reaction option (brace, anchor, duck into cover), except when explicitly caused by a teammate's obvious mistake.

### 4.2 Initial input map

Keyboard/mouse: WASD move, mouse look, Space jump, Shift sprint, E interact, mouse left use selected tool, mouse right aim/tool alternate, Q ping, R reel/secondary interaction, F brace/grip, 1–4 quick tools, Tab mission tablet, Esc pause. Controller: left stick move, right stick camera, A/Cross jump, X/Square interact, triggers tool/aim, shoulder buttons cycle tools, hold B/Circle grip/brace, D-pad ping/tablet. All bindings are remappable; avoid mandatory rapid tapping.

### 4.3 Interaction rules

Objects display contextual affordances at close range: **Grab**, **Use**, **Attach**, **Trade**, **Throw**, **Secure**. Two-hand objects expose dedicated grip points and mass. Player intent is verified by the host; the object transitions to a single authoritative owner/handler while other players receive predicted visual feedback. If two players grab simultaneously, the host resolves deterministic priority and displays who has the handle.

### 4.4 Anti-frustration guardrails

Collision geometry is intentionally smoother than the visual model. Ropes attach to named anchor sockets rather than arbitrary pixels in animated fur. Unstuck resets and checkpoint recovery exist in the pause menu. No insta-kill fall damage during routine missions. Avoid permanent equipment loss in early contracts. Put a brief grace period after reviving someone to prevent immediate repeat falls.

---

## 5. Giant Creature Simulation (The Signature System)

### 5.1 Creature as character and world

A giant consists of (a) a **visual/animation rig**, (b) a **creature-local traversal surface atlas** and simplified collision volumes, (c) **body zone sensors**, (d) a **behavior brain**, (e) a **contract objective map**, and (f) **networked authoritative state**. The giant should not be a single enormous free-simulated Rigidbody. Instead, body animation and local simulated reactions provide a controllable, performant foundation.

**Coordinate principle:** Each player and attached prop may be represented relative to a creature-frame/zone-frame while attached, then converted to world-space on release. Visual bones can move independently, but safe gameplay anchors live on stable, authored attachment frames. This protects climbing against animation wobble, net jitter, and giant-scale numerical drift.

### 5.2 Body zone schema

Every giant is divided into named zones with properties:

- `ZoneId`, `DisplayName`, `AttachmentFrame`, `TraversalMesh/ColliderSet`.
- `GripMaterial` (fur / scale / shell / slick / no grip); stamina and wind modifiers.
- `AnchorSockets[]`, `ShelterVolumes[]`, `JobSockets[]`.
- `Sensitivity` (0–1); `Heat`, `Wetness`, `Infestation`, `Irritation` (0–100 as applicable).
- `ReactionEventWeight` modifiers; permitted camera volumes; audio surface type.
- `SupportedAnimations`, valid transition windows, zone connectivity edges.

For Mossback: Left Flank, Right Flank, Spine, Shoulder, Neck, Head, Ear, Belly, Hindquarters, Harness Platform. Start with five accessible zones and add the rest after traversal is stable.

### 5.3 Creature vitals and triggers

Use a small, legible set of meters rather than a complex biological simulation:

| Meter | Meaning | Increased by | Decreased by | Behavior effects |
|---|---|---|---|---|
| Comfort 0–100 | Overall wellbeing | Successful gentle care | Mistakes, untreated ailments | Low comfort raises restlessness |
| Irritation 0–100 | Immediate itch/pain/agitation | Rough tools, parasites, bad timing | Calming, removing cause | High irritation causes shakes/scratches |
| Stamina 0–100 | Ability to continue route | Rest | Movement, stress | Low stamina triggers rest stops |
| Trust 0–100 | Persistent affinity to the guild | Good mission grades | Harmful/abandoned care | Unlocks easier boarding or novel interactions |

Missions use **observable thresholds** and telegraphing rather than raw random events. Example tuning hypothesis: irritation ≥45 permits mild scratch; ≥70 permits vigorous shake; ≥90 enters an emergency, with a ten-second reaction window before the most dangerous animation. These values are provisional, designer-tunable, and subject to playtests; never link directly to instantaneous unavoidable death.

**Recommended behavior selection:** State-driven weighted selection with cooldowns. States include Calm, Curious, Restless, Alert, Agitated, Exhausted, Sleeping, Emergency. Reaction events have `Prerequisites`, `WarningDuration`, `ImpactWindow`, `Intensity`, `BodyZoneMask`, `Cooldown`, `Counterplay`, and `RecoveryDuration`. The host chooses an event and replicates its event ID, start timestamp, severity, and optional seed; clients animate locally against synchronized timing.

### 5.4 Reaction examples

| Trigger | Warning | Physical effect | Player counterplay | Comic payoff |
|---|---|---|---|---|
| Sneezing | Nose twitch, loud inhale, particles | Head/neck lurch, gust around face | Hide behind ear, brace, clip tether | Players launched like confetti |
| Back scratch | Paw lift, localized tremor | Scrapes side, rubs against tree | Leave zone or shelter | Entire team dives into fur |
| Full body shake | Low groan, fur ripples outward | Radial impulse to unsecured props/players | Anchor, brace, finish current task | Supplies scatter across back |
| Roll to nap | Repeated slow yaw, sleepy vocal | Body tilts 70–100° during staged animation | Move to safe side/shell ledge | Teammates slide past each other |
| Sudden sprint | Look toward distant noise; hooves tense | Wind and acceleration increase | Retract glider, anchor to frame | Someone is towed by a rope |
| Happy purr | Relaxed posture and rumble | Mild platform vibration; comfort rises | Continue delicate work | Gentle interruptions, funny voice wobble |

### 5.5 Creature fairness contract

Every major reaction must meet four conditions: **readable tell**, **safe response**, **bounded impulse**, and **recovery route**. Add a `ThreatDirector` that caps simultaneous major events, guarantees reaction cooldowns, and blocks a second hard launch immediately after checkpoint recovery. A reaction should turn an easy job into a short improvised mini-adventure—not invalidate ten minutes of work.

### 5.6 Emotional response as progression

Trust changes body language and services: a trusted Mossback may lean against the boarding platform, pause a shake when calmed, or reveal a special safe pathway. Trust should be earned per creature species and guild profile, but never make a new player in a group feel useless. This is *horizontal quality-of-life* progression, not mandatory stat inflation.

---

## 6. Work Orders, Tools, and Encounter Design

### 6.1 Initial job families

1. **Parasite removal:** Locate moving pest, secure it with tongs/net, detach carefully, contain it without releasing it onto a teammate. Pest behavior: flee, burrow, stick to a tool, or latch to another zone.
2. **Fur and shell maintenance:** Untangle knots, remove debris, scrub growth, repair shell cracks with adhesive patches. Timing matters during movement; two players can stabilize a large item.
3. **Medical-style comfort care:** Cool a hot spot, apply soothing gel, remove a thorn, deliver a calming snack. No graphic surgery; care is tactile and playful.
4. **Rigging and repair:** Replace a broken harness buckle, fix a weather sensor, install guide ropes, repair a suspended work platform.
5. **Search and retrieval:** Find missing guild equipment, misplaced baby creature, or escaped pest in a layered environment with alternate routes.
6. **Emergency response:** High-risk optional objective triggered by player mistakes or severe weather; e.g., recover a contaminated canister before the giant reaches a village.

### 6.2 Tool catalogue

| Tool | Verb / interaction | Failure or comic interaction | Upgrade direction |
|---|---|---|---|
| Grip gauntlets | Hold safe surfaces | Stamina drains on slick fur | Better efficiency, not infinite grip |
| Rope launcher | Place socket-to-socket line | Tangled lines, accidental swing | Longer reach, faster reel |
| Multi-grabber | Two-handed pull/grab | Can knock loose nearby props | Improved stability |
| Pest vacuum | Collect small parasites | Overheats, suction attaches to teammate | Larger canister, quieter operation |
| Soothing sprayer | Reduce irritation in targeted zone | Wrong solution increases itch | Different spray types |
| Portable scanner | Reveal invisible job clues and sensitivities | False positives if moving too quickly | Wider scan arc |
| Wind glider | Rescue from falls, cross gaps | Bad in gusts, carries extra cargo poorly | Better air control |
| Emergency anchor | Quick temporary safe attachment | Single-use, limited duration | Faster deployment |
| Snack launcher | Calm or distract giant | Wrong snack triggers hiccups | More targeted delivery |

**Prototype priority:** Grip gauntlets, rope launcher, pest tongs/vacuum, soothing sprayer. The goal is to test verbs and interaction compatibility; avoid implementing nine weapons-grade gadgets before the loop works.

### 6.3 Difficulty knobs

Difficulty is built from *behavior timing*, *route exposure*, *number of simultaneous jobs*, *tool scarcity*, and *weather*, not just making the giant more hostile. Casual and standard difficulties should share rewards within a narrow range; optional hazardous contracts can offer additional cosmetics, not essential power. Dynamic scaling can provide extra anchor sockets and calmer events for a solo player without changing the world story.

### 6.4 Sample contract: “A Very Bad Itch”

**Client:** Mossback sanctuary. **Brief:** Remove three Bramblemites before sundown. **Primary work:** identify infestation on left shoulder, groom rear spine, secure dislodged hatchling parasite near the ear. **Optional:** Recover a lost survey drone under the jaw. **Complications:** A moving pest migrates zones if a player makes excessive noise; a missed soothing spray raises irritation; high irritation queues a warned shake. **Success:** at least 2/3 critical treatments done, creature comfort >45 at extraction, team returns safely. **Bonus:** all three tasks, optional drone, no creature emergency. **Best clip moment:** giant shake sends the vacuum canister spinning; a player swings to catch it and is pulled off the spine.

---

## 7. Creature Roster and Content Plan

### 7.1 Mossback — first creature / tutorial ecosystem

**Identity:** Shaggy, slow-moving valley herbivore, roughly the size of a small hill. **Visual:** soft moss-like fur, enormous ears, broad shoulders, rope-ready harness, rounded hoofs. **Biome:** green highlands with stone arches and shallow rivers. **Movement:** breathing, short walks, turns, sit, sneeze, scratch, rest roll. **Key mechanics:** fur grip, local itch zones, dust bursts, rope shortcuts. **Hazards:** Bramblemites, burrs, falling branches, rain making fur slippery. **Signature rescue:** A player can jump onto the hanging ear and use it as a temporary moving platform during a sneeze.

**First mission sequence:** Arrival platform → climb flank → place anchor on shoulder → identify and catch parasite → apply sprayer to hot spot → collect debris on spine → route through ear toward exit → descend to extraction. First two reactions scripted gently for onboarding; later reactions use live behavior director.

### 7.2 Shellstrider — second giant / topological navigation

**Identity:** Gentle tortoise-like mountain wanderer with cliffs and a small ecology on its shell. **Biome:** canyon paths and waterfalls. **Mechanics:** cliff climbing, cracks as narrow passages, wet surfaces, movable shell plates, wind exposure. **Signature event:** The giant withdraws its head during a storm, changing the traversal route and creating an emergency shortcut through a shell tunnel. **Work orders:** reseal plate joints, prune overgrown invasive roots, reposition survey instruments. **Risk:** Scope explosion from making an entire biome; build small modular zones connected by deliberate routes.

### 7.3 Skywhale — aspirational later giant / advanced traversal

**Identity:** Friendly airborne animal with broad fin-wings and floating islets around it. **Biome:** cloud sea and storm fronts. **Mechanics:** high-wind gliding, tethered movement, wing flex, altitude hazards, timed docking. **Signature event:** Powerful wingbeat sweeps across the worksite; players brace, hide in feather valleys, or use the air current to cross. **Scope label:** Stretch goal; do not promise for launch until ground-giant locomotion and multiplayer synchronization are stable.

### 7.4 Creature production matrix

Every new giant must add **one new movement hazard, one unique traversal material, two unique jobs, and a signature behavior**, while reusing the common movement, jobs, tool framework, and behavior interface. Creature concepts that require entirely new engine foundations should be reserved for major expansions.

---

## 8. Progression, Rewards, and Replayability

### 8.1 Economy

At extraction, the host calculates a deterministic **Guild Pay** award from: base contract + completed jobs + creature comfort bonus + optional findings + rescue/care bonuses − modest consumable costs. Avoid penalizing harmless physics antics so aggressively that people stop experimenting. No randomized paid loot boxes, energy timers, or premium consumables.

Example provisional formula: `Pay = ContractBase × CompletionMultiplier + CareBonus + OptionalBonus − ConsumedSupplyCost`. A grade (S/A/B/C) is separate from money and emphasizes compassionate care and objective completion. Mission score is for bragging rights and replay value, not punishing a struggling friend.

### 8.2 Upgrade philosophy

**Early game:** Better traversal reliability, improved rescue options, and more interesting choices—not massive raw-stat power gaps. **Midgame:** Specialize loadouts for job types and difficult zones. **Late game:** Cosmetic guild customization, special contracts, mastery badges, and alternate strategies.

Example unlock paths:

- **Rigger:** extra anchor charge → faster zipline setup → small deployable safety platform.
- **Care Specialist:** improved scanner cues → safer soothing formulas → faster gentle treatment.
- **Rescuer:** better glider handling → enhanced catch tether → emergency recover beacon.
- **Collector:** better pest containment → deployable cage → lure toolkit.

No rigid classes. A group of all beginners must still complete all mandatory tasks with starter equipment.

### 8.3 Social unlocks and hub

The Guild Base is a compact, interactive staging area: contract board, tool lockers, cosmetics desk, living field guide, giant observation deck, and a photo wall of recent absurd moments. Player outfits emphasize funny silhouettes, hats, harness colors, and guild patches. Cosmetic unlocks are earned from play in the proposed base game.

### 8.4 Procedural variety without procedural blandness

**Hand-author terrain and reactions.** Randomize job sockets, secondary tasks, creature mood, weather, roaming pests, and optional rewards from validated combinations. Seed each mission and store its high-level parameters for reproducibility. Avoid procedural terrain generation for the first release; unstable generated geometry would be dangerous for both climbing and networking.

### 8.5 Replay systems

Contract modifiers: Tick Season (pests move faster), Heavy Mist (scanner range reduced), Long Migration (more walking and wind), Sleepy Giant (more naps/rolls), Festival Delivery (time pressure), Family Day (more optional finds). Add weekly featured combinations only if updates can be operated sustainably; no live-service obligation is needed for a premium co-op game.

---

## 9. Cooperative Social Design

**Party support:** Invite friends, lobby code, private/public settings, drop-in at safe checkpoints, rejoin after disconnect where technically feasible. **Communication:** Context ping wheel is core; voice chat is optional and should not be a prerequisite. If in-game voice is added, prioritize clear mute/block/report controls and avoid open mic defaults. The game can work through external voice applications from day one.

**Friendship-friendly friction:** Heavy items have two handles. One person braces while another treats an irritated zone. A rope line requires someone to hold a winch briefly. Tool misuse can inconvenience teammates, but direct sabotage must have limits. Players can knock each other off balance in harmless moments, but no unlimited griefing by deleting all resources or locking a player out.

**Downed teammates:** A launched player can catch a tether, glide to a lower body zone, or enter a recoverable dangling state. Other players can use a winch or launch a lifeline. Introduce these rescue options in the tutorial, so spectacular failures invite cooperation rather than rage-quitting.

**Host disconnect:** For the initial build, safely end or pause with explicit communication and preserved rewards at the last checkpoint; do **not** advertise seamless host migration unless implemented and tested. Host migration can be a later feature. A robust rejoin path and clear session failure messaging are more valuable than an unstable automatic migration system.

---

## 10. UI, Audio, Accessibility, and Art

### 10.1 UI surfaces

- **In-world:** Hand-sized wrist scanner shows immediate job clues; body-zone icons appear as readable beacons.
- **Mission tablet:** Primary/optional tasks, elapsed time, giant comfort/irritation/stamina, body-zone map, teammate status.
- **Tools:** Large item silhouettes, clear grip points, reticle states (valid/blocked/too far), short tutorial animations.
- **Debrief:** Grade, care summary, income, recovered equipment, standout teamwork moment, unlocked entries.

Use creature animations and sound as primary tell channels; HUD is secondary. A player should recognize an imminent sneeze without opening a menu.

### 10.2 Audio priorities

Physical comedy needs excellent audio: an escalating inhale before a sneeze, wind roar on an exposed shoulder, rope tension creaks, goofy impact sounds, big muffled rumbling purrs, soft creature vocals, and teammate-spatial sound effects. Create unique mix profiles by zone (inside ear vs exposed spine). Add audio subtitles/event captions and independent controls for voice/music/effects.

### 10.3 Accessibility plan

Rebind every control; controller support; subtitles with speaker/event labels; camera shake intensity slider and off switch; motion blur off; field-of-view setting; hold/toggle climb option; high-contrast climbable-material cues beyond color; distinct shapes for job markers; adjustable QTE-free timing and lower difficulty options; text size scaling. Avoid relying exclusively on proximity voice for essential objectives.

### 10.4 Visual performance direction

Stylized rather than hyperrealistic. Use LODs, baked lighting where practical, limited expensive fur simulation (cards/shell layers), and strategic VFX. Visually dense fur should have simplified underlying climbing colliders. Focus polish on silhouette, facial reactions, the feeling of height, and physical tool feedback—not polygon counts alone.

---

## 11. Unity Technical Architecture

### 11.1 Recommended stack — design proposal, not a tested integration

| Layer | Candidate | Rationale |
|---|---|---|
| Engine | Unity 6.3 LTS, pinned project version | Stable LTS baseline; verify package compatibility at project creation |
| Render pipeline | URP | Scalable stylized visuals on mainstream PC hardware |
| Code | C# / GameObjects / assembly definitions | Fits small-team iterative component architecture |
| Input | Unity Input System | Rebinding and gamepad support |
| Netcode | Netcode for GameObjects | Understandable GameObject architecture and RPC/state synchronization |
| Session service | Unity Multiplayer Services SDK (sessions + Relay) | Host/join workflow without immediate dedicated-server requirement |
| Platform | Steamworks integration for achievements/invites; validate adapter | Steam user context, store and community tooling |
| Physics | Unity PhysX Rigidbody for props; controlled character locomotion | Authority and predictable interaction behavior |
| Animation | Animator rig + Animation Rigging as needed | Layered body reactions and attachment frames |
| Camera | Cinemachine | Camera smoothing and gameplay states |
| Assets | ScriptableObject definitions; Addressables only if beneficial | Data-driven creature/tool/contract content |
| Version control | Git + LFS or Unity Version Control | Binary asset management and collaboration |
| CI / tests | Unity Test Framework + automated builds | Regression safety for physics, saves, and multiplayer |

**Verified documentation note:** Unity positions Multiplayer Services sessions as a unified layer over Lobby/Relay/Matchmaker, and labels the older standalone service packages deprecated in favor of `com.unity.services.multiplayer`. Pin exact versions only after testing the initial chosen editor; official documentation and package compatibility change over time.

### 11.2 Runtime system diagram

```text
Client A/B/C/D (input, camera, cosmetics, local prediction)
              |        ^
        commands      snapshots/events
              v        |
Host / authoritative simulation
  SessionCoordinator + NetworkWorld
  MissionDirector ---------- RewardCalculator
  CreatureBrain ------------ CreatureState + ReactionScheduler
  ZoneRuntime -------------- AttachmentFrames/SurfaceAtlas
  PlayerMotor -------------- Grip/Rope/Glider/Rescue
  InteractionAuthority ----- Tools/Props/Pests
  ContractRuntime ---------- ObjectiveState
              |
        PersistentProfile (local / approved platform save)
```

**Single-authority rule:** The host owns creature behavior, objective completion, authoritative prop state, rewards, and attachment decisions. Clients own input collection, cameras, VFX/audio, and safe cosmetic approximations. Do not alternate authority unpredictably when a player picks up a prop.

### 11.3 Two coordinate spaces

- **World frame:** Guild base, biome dressing, extraction craft and distant scenery.
- **Creature-local frame:** Attached players, props, job sockets, anchors and traversal zones. A creature root movement changes the local frame, while fine body movement is handled by attachment frames keyed to animation bones or authored helper transforms.

The **AttachmentResolver** receives `(CreatureId, ZoneId, AttachmentSocketId, localPosition/localRotation)` and returns a stable pose. Character simulation uses that pose as a supporting reference while attached. Release transfers linear/angular velocity from local frame + relevant reaction impulse back into world frame. These are design abstractions; actual Unity API and Rigidbody implementation must be prototyped for jitter and tunneling.

### 11.4 Traversal collider strategy

Do not use a fully dynamic per-frame skinned mesh collider for normal gameplay. Use low-poly convex/primitive colliders parented to stable bones and curated climbable zones. Where a surface needs fine detail, use pre-authored local grip volumes or an interaction mesh that updates sparingly. Visual fur can move more richly than gameplay geometry. Constraints: no steep collision spikes, no rapidly changing normals, and generous anchor socket collision radii.

**Key tests:** Walk across a shoulder during idle breathing; climb from flank to spine during turning; stay attached across a large shake; move from local to world frame during a fall; rejoin multiplayer session while hanging. If these fail, do not implement additional giant species.

### 11.5 Multiplayer snapshot design

Host simulates at a **target** 50–60 Hz physics step and sends gameplay state snapshots at an initially evaluated 15–30 Hz, subject to measured bandwidth/CPU and latency. Client visual interpolation runs at render frame rate. Creature state replication is **event based** when possible (`ReactionId`, start time, intensity, seed), with low-frequency pose correction for major attachment frames. Avoid streaming every giant bone over the network.

Sync high-value state: player root/state, authoritative attached zone/socket, anchor status, rope end points and gameplay tension, carried objects, job progress, pest positions, comfort/irritation, event timestamps, team inventory, mission phase. Reconstruct particle effects, fur motion, decorative rope segments, camera shake, cloth and secondary animations locally.

**Host validations:** Can the player reach this socket? Does the tool have the correct target and charges? Is this object already held? Is the job within its valid state window? Are the change magnitudes within bounds? If yes, commit the authoritative event and broadcast feedback.

### 11.6 Ropes and ragdolls — explicit scope control

A physically exact 6-player, multi-rope networking solution is risky. In the MVP, ropes are **constraints between two valid sockets** with host-evaluated maximum length and break rules; clients draw the sagging rope using local splines/Verlet visuals. A **swing rope** is a special-case pendulum/constraint that applies forces to the controlled player motor. Heavy tools have limited host-simulated Rigidbody states. Ragdoll is short-lived with capped impulses and a recover-to-animation transition; remote clients may use blended approximation instead of broadcasting every limb joint.

### 11.7 C# modules / project layout

```text
Assets/_TinyGiantProblems/
  Art/ Characters/ Creatures/ Biomes/ Props/ VFX/
  Audio/ Music/ SFX/ Voice/
  Content/ Creatures/ Contracts/ Tools/ Upgrades/
  Prefabs/ Network/ Players/ Creatures/ Tools/ UI/
  Scenes/ Bootstrap/ GuildBase/ Mossback_Graybox/ Mossback_Final/
  Scripts/
    Core/ GameBootstrap, Time, EventBus, SaveService
    Networking/ SessionCoordinator, NetworkWorld, NetworkMessages
    Player/ PlayerMotor, ClimbMotor, GripController, PlayerRecovery
    Creatures/ CreatureBrain, CreatureState, ZoneRuntime, ReactionDirector
    Traversal/ AttachmentResolver, RopeConstraint, AnchorSocket
    Tools/ ToolController, PestVacuum, SoothingSprayer, Grabber
    Missions/ MissionDirector, ObjectiveRuntime, RewardCalculator
    AI/ PestBrain, AmbientNPC
    UI/ HUD, MissionTablet, Debrief, Accessibility
    Editor/ ValidationTools
  Tests/ EditMode/ PlayMode/ Multiplayer/
```

Prefer a small number of independent assemblies (Core, Gameplay, Networking, UI, Tests) rather than a separate assembly for every folder. Core data definitions should not depend on UI or networking; adapters translate commands/events at boundaries.

### 11.8 Representative contracts and interfaces

```csharp
public interface ICreatureReaction
{
    string Id { get; }
    bool CanStart(CreatureContext context);
    void Begin(CreatureContext context, double networkStartTime);
    void Tick(CreatureContext context, float deltaTime);
    void End(CreatureContext context);
}

public interface IInteractable
{
    bool CanInteract(in InteractionContext context);
    InteractionResult ExecuteOnHost(in InteractionContext context);
}

public readonly struct AttachmentKey
{
    public readonly int CreatureId;
    public readonly int ZoneId;
    public readonly int SocketId;
    // Local pose is transmitted separately to avoid large per-tick payloads.
}
```

This is **architectural pseudocode**, not guaranteed drop-in Unity code: concrete network data serialization and lifecycle logic must be implemented against the pinned Netcode package.

### 11.9 Event-driven creature brain pseudocode

```text
Each server tick:
  Read objectives, zone irritation, comfort, stamina and weather.
  Advance current reaction; do not start contradictory reactions.
  If no major reaction active:
    Evaluate eligible reactions with warning/cooldown/zone constraints.
    Choose a weighted candidate if director budget permits.
    Queue warning event at a synchronized network timestamp.
  When warning expires:
    Apply bounded impulses to only affected players/props.
    Trigger recovery window and reaction cooldown.
  Broadcast compact authoritative state changes and event identifiers.
```

### 11.10 Persistence and security

A **guild profile** stores unlocked gear, cosmetics, creature trust, settings, best mission grades, and discovered field notes. Save after debrief and other safe transitions; use atomic writes and explicit schema versions. Design authoritative mission reward calculation to prevent easy client-side reward inflation, while recognizing that host-authoritative co-op games are not fully cheat-resistant. No personal account or cloud system is required for prototype. Steam Cloud can be evaluated before launch; conflicts must be handled deliberately.

### 11.11 Observability and test harness

Provide an in-editor creature debug panel with mood sliders, force-trigger reaction, active zone overlay, attached players, socket IDs, host/client state and network jitter chart. Add a deterministic replay **event log** (seed, commands, reaction starts, objectives and rewards); it is not a full physics replay, but sufficient to reproduce most gameplay logic bugs. Collect opt-in aggregated diagnostics and scrub direct personal identifiers.

---

## 12. Performance and Reliability Budgets

The following values are **initial test targets**, not promises. Measure them on representative hardware and revise after profiling.

| Area | Starting acceptance target | Test method |
|---|---|---|
| Frame rate | 60 FPS at 1080p on a defined midrange PC preset | Representative encounter and VFX stress scenes |
| Input response | Movement feels immediate locally; no visible climbing lag under normal ping | 0/80/150/250 ms simulated latency tests |
| 4-player co-op | 30-minute session without critical desync or blocking bugs | Two physical machines + simulated clients |
| Multiplayer packets | Stable, measured network budget with no frequent spikes | Unity Profiler + network simulator |
| Load time | Favor fast contract retries over large scene streaming waits | Cold/warm launch measurements |
| Recovery | No player permanently stuck after valid attachment failure | Automated traversal fuzz paths |
| Host loss | Clear handling and checkpoint-safe exit | Disconnect host across each mission phase |

**Hardware test baseline proposal:** GTX 1660 / RTX 2060-level GPU, 16 GB RAM, mainstream 6-core CPU, SSD. Since these are planning assumptions, publish minimum system requirements only after measured builds across multiple actual PCs.

---

## 13. Scope: Prototype → Vertical Slice → Launch

### 13.1 Prototype (the fun test)

**Deliver exactly:** One graybox Mossback; stable walk/climb/hang/fall/brace; one rope; one pest; one care tool; three job sockets; host + three clients; two reactions (sneeze and shake); reset/respawn; mission win and debrief stub.

**Exit gate:** Four people can complete at least three runs, understand why the creature reacted, recover from a mistake, and voluntarily request another round. No serious desync on core climb/attachment. If the fun depends only on scripted jokes, rework the systems.

### 13.2 Vertical slice (the trailer test)

Polished Mossback, one biome, reliable four-player session/invite flow, six job variants built from three job families, four tools, tutorial, one session progression track, sound/VFX/UI, robust recovery, basic settings, 20–30 minute polished mission. Create a 60–90 second trailer from authentic captured co-op gameplay.

**Exit gate:** Players who have never seen the game can onboard within five minutes and play 25 minutes without a developer explaining the controls; most can identify the next goal. Gameplay clips look understandable on mute and entertaining with sound.

### 13.3 Early Access vs 1.0 decision

**Option A: Focused 1.0:** 2 polished giants, 6+ contract templates, 12–15 meaningful upgrades, cosmetic unlocks, good onboarding, stable co-op and accessibility. **Option B: Early Access:** 1–2 giants with a specific, credible content roadmap and already replayable core systems. Do not use Early Access as an excuse for broken networking. **Stretch:** Skywhale, more biomes, dramatic weather transitions, 6-player support if tests prove it stable.

### 13.4 Defer explicitly

Fully autonomous moving giants in seamless open worlds; underwater full-body swimming; procedural giant anatomy; cross-platform console multiplayer; six-player ragdoll physics with every joint synchronized; cosmetics marketplace; persistent shared servers; player-created levels. These are post-validation ideas, not near-term requirements.

---

## 14. Work Breakdown and Milestones

These are *relative milestones* to prioritize dependencies, not calendar promises. For a solo developer working part time, this may be a long project; a small experienced team can parallelize art, engineering and playtesting. Do not equate a prototype schedule with a commercial-release date.

| Sequence | Engineering milestone | Deliverable / acceptance |
|---|---|---|
| M0 | Project foundation | Unity LTS project, repository, test scene, gamepad bindings, build pipeline |
| M1 | Player motor | Walk, jump, camera, climb fixed wall, recover from fall |
| M2 | Creature frame | Graybox giant with zones, sockets, animated body transforms; stable attachment |
| M3 | Reaction mechanics | Telegraphed sneeze/shake with bracing and bounded impulses |
| M4 | Interactive jobs | One pest task, sprayer task, progress and comfort effects |
| M5 | Network prototype | 2-player then 4-player host authority, shared interactions, disconnect handling |
| M6 | Mission loop | Board, three jobs, react, extract, grade, reset |
| M7 | Public-demo polish | Art, sound, onboarding, settings, instrumentation and external testing |
| M8 | Commercial content | Expanded Mossback, second creature, upgrade economy, content variations |

**Parallel workstreams:** Audio and UI may begin during M4/M5. Creature art production should remain low-cost until M2 movement architecture proves stable. Store-page concept art can be explored, but do not announce a release window before M6 passes.

### 14.1 First 10 engineering tickets

1. New Unity 6.3 LTS URP project, Git, assembly definitions, test runner, fixed package manifest.
2. Third-person `PlayerMotor` movement, camera and input action assets.
3. `ClimbableZone` prefab and state transitions (grounded ↔ climbing ↔ falling).
4. `AttachmentResolver` proof of concept on a moving rotating platform.
5. Mossback blockout with 5 named zones and 8 authored anchor sockets.
6. `CreatureBrain` with scripted warning and reaction scheduler, debug sliders.
7. Brace + impulse reaction and recoverable fall; safety tests.
8. First rope between authored sockets and simple swing.
9. `PestBrain` and one interactive care job with readable progress.
10. NGO/MPS host/join test with replicated player attachment and creature event timing.

### 14.2 Acceptance tests that matter

- A player can attach to a moving shoulder for 90 seconds without drift, clipping or unexpected fall.
- With 150 ms artificial round-trip latency, a remote player sees a sneeze in sync within an agreed visual tolerance and ends in a valid recovery state.
- A player who disconnects while holding a pest releases/recovers the authoritative object; mission does not softlock.
- Two players attempting the same handle cannot duplicate the object or complete the job twice.
- Any major creature reaction has at least one discoverable safe response and a clear audiovisual tell.
- A complete mission can reset and replay without reopening Unity or restarting the host process.

---

## 15. Risk Register and Mitigation

| Risk | Severity | Warning sign | Mitigation / kill criterion |
|---|---|---|---|
| Moving animated collision + networking | Critical | Players drift or fall during normal animation | Prove stable zone frames by M2; simplify geometry/animation before adding art |
| Emergent physics is funny only once | High | Playtesters enjoy first clip but quit after 2 sessions | Add readable decisions, varied jobs and team rescues; retest retention |
| Creature reactions feel unfair | High | Repeated deaths without known counterplay | Mandatory warnings, event budget, bounded impulses, recovery windows |
| Art demands overwhelm solo development | High | Months of creature sculpting before fun prototype | Graybox first, art gate after M6 |
| Networking costs/support | Medium | Frequent Relay/connect errors or per-session costs above expectations | Test Steam transport alternatives, optimize snapshot rate, record costs |
| New creature requires rebuilding systems | High | All abilities hard-coded to Mossback | Data-driven zone/action/event contract; require reuse matrix |
| Scope creep from open-world fantasy | High | Core session loop not playable after many new features | Public deferred-feature list and milestone gates |
| Brand/title collision | Medium | Similar existing title or discoverability issue | Trademark and storefront name check before branding spend |
| Multiplayer toxicity | Medium | Players can trap others or waste resources with no recourse | Safe recovery, kick/mute, limited grief interactions |
| Commercial demand unproven | High | Demo interest weak outside friend circle | External playtests and clear market validation before expansion |

---

## 16. Playtesting, Metrics, and Commercial Validation

### 16.1 Research questions

1. Are players entertained by *care tasks* even before an accident?
2. Can they predict giant reactions from natural cues?
3. Do they understand rescue tools without a developer present?
4. Is traversal enjoyable rather than exhausting after 10 minutes?
5. Does a mission create at least one retellable co-op story?
6. Does the combination of cute fantasy and slapstick resonate with streamers and regular friend groups?

### 16.2 Internal telemetry proposals

Measure tutorial completion, time to first successful job, ratio of falls rescued, number of reaction warnings noticed, mission completion rates, time spent lost, disconnect rate, crash-free sessions, second-mission intent and actual replay, and frequency of each tool. Pair these with consented observation and player interviews. Avoid manipulating players to chase a single metric; use data to find friction.

### 16.3 Prototype gates (targets to test, not market facts)

- **Learnability:** ≥80% of fresh testers finish basic movement/first job tutorial unaided.
- **Fairness:** ≥75% of players correctly identify why they were launched after seeing the sequence once.
- **Engagement:** ≥60% of test groups choose a second run when offered without reward pressure.
- **Reliability:** no blocker/desync in repeated 4-player 30-minute internal runs.
- **Clarity:** testers can explain the high-level objective in one sentence.

These thresholds are intentionally opinionated internal targets; refine with real tester samples and better baselines. A five-person friend test is not statistically convincing proof of demand.

### 16.4 Market plan

Build shareable short clips of unscripted rescues and creature reactions. Create a Steam Coming Soon page when the art identity and honest gameplay footage are ready. Use a small closed group to verify multiplayer, then Steam Playtest for broader feedback. Steam Playtest is a Steamworks mechanism for public/limited prerelease testing associated with a main store page; it is not the same as a paid Early Access release. Keep the game premium; test price against comparable titles, content depth and wishlist feedback rather than relying on the initial $14.99 hypothesis.

**Positioning line to test:** “Six tiny workers. One enormous animal. Absolutely no safety training.”

### 16.5 Trailer sequence (concept)

0–8s: peaceful giant seen from a valley; camera reveals tiny technicians attached to its fur. 8–20s: cooperative two-player parasite extraction and clear objective. 20–35s: someone uses the wrong spray; warning tell; the giant inhales. 35–50s: sneeze launches crew, frantic swing-and-rescue, absurd scream. 50–65s: teamwork wins; comforting creature response; quick cuts of other giant/biome if implemented. 65–75s: title, multiplayer premise, Steam wishlist call to action. Film only actual available mechanics; don't imply unbuilt features are playable.

---

## 17. Decisions Log and Open Design Questions

| Decision | Current proposal | Validate by |
|---|---|---|
| Genre | Session-based care + climbing physics co-op | Graybox playtests |
| Tone | Affectionate fantasy spectacle + comedy; not horror | Art/look-dev and user testing |
| Initial crew | 4-player target, 6-player stretch | M5 network and performance testing |
| Player motor | Controlled movement + limited ragdoll | M1/M3 mechanical tests |
| Giant movement | Animated/kinematic with stable zone attachment | M2 proof of concept |
| Roguelite/meta | Light horizontal gear progression; no grind | Vertical slice testing |
| Platform | Windows/Steam, gamepad and KBM | Production build validation |
| Release | Premium PC; no date announced | Market/production gates |

**Highest-priority unanswered questions:** How forgiving should falling be? Is the giant always migrating or sometimes stationary? How much precision should care tools require? Should solo mode include helper drones? Can the first creature support five enjoyable mission variants? How much of the environment outside the body is traversable? These are prototype experiments, not reasons to delay M0.

---

## 18. Implementation Notes / Explicit Next Step

The next artifact after this GDD should be a **Mossback Vertical-Slice Technical Specification** with concrete scene hierarchy, ScriptableObject schemas, finite-state diagrams, network RPC/state structures, player motor parameters, input asset actions, prefab list, and acceptance tests. Then produce a runnable graybox scene, first in single-player and second in host/client. Do **not** commission three polished creature models yet.

**Immediate build order:** create project → build locomotion against a moving platform → attach to named zones → implement two telegraphed reactions → build one complete treatment task → run it with two clients → finish mission/extract/replay. This is the minimum chain that proves or falsifies the concept.

---

## 19. Primary Technical References (checked November 2026)

- Unity, **Unity 6 release support**: https://unity.com/releases/unity-6/support — Unity 6.3 LTS supported through December 2027.
- Unity, **Multiplayer Services SDK / sessions overview**: https://docs.unity.com/en-us/mps-sdk/sessions-overview — session lifecycle and integrations.
- Unity, **Multiplayer Services subservices**: https://docs.unity.com/en-us/mps-sdk/subservices-toc — unification of Lobby, Relay, Matchmaker; standalone SDK deprecation notices.
- Unity, **Relay vs Lobby**: https://docs.unity.com/en-us/mps-sdk/advanced-config/relay-vs-lobby — host/relay architecture tradeoffs.
- Unity, **Unity 6 Netcode for GameObjects package documentation**: https://docs.unity3d.com/Manual/com.unity.netcode.gameobjects.html — verify versions against actual installed editor.
- Unity, **Unity Input System documentation**: https://docs.unity3d.com/Manual/com.unity.inputsystem.html — modern input/rebinding approach.
- Valve, **Steam Datagram Relay**: https://partner.steamgames.com/doc/features/multiplayer/steamdatagramrelay — alternative Steam-centric networking transport, integration to be evaluated separately.
- Valve, **Steam Playtest**: https://partner.steamgames.com/doc/features/playtest — prerelease testing mechanics.

**Document classification:** Internal design proposal. Names, mission numbers, price, content commitments, performance targets, and milestones are tentative until user research and a working multiplayer prototype validate them.


# Part III | Detailed implementation records


## Source: Docs/Mossback-Technical-Specification.md

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


## Source: Docs/Development-Log.md

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


## Source: Docs/3D-Asset-Checklist.md

# 3D asset production checklist

## Imported

- Mossback: supplied `mossback.glb`, preserved in `SourceAssets/Mossback/`.
  Native OBJ, base-color PNG, URP material and reusable prefab are in
  `Assets/_TinyGiantProblems/Art/Creatures/Mossback/`.
- Source mesh: 29,610 vertices and 40,000 triangles; no skeleton or animation clips.
- Installed in `Mossback_FirstPlayable.unity`; scene builder reuses the prefab.
- Current integration keeps prototype traversal colliders, care targets and guild
  decking. These are not a final surface-conforming climbing setup. The generated
  anatomy differs from the original shaggy-herbivore concept; preserve the supplied
  design unless a replacement is requested.

## First-playable priorities (recommended generation order)

1. **Guild technician** — one reusable humanoid, separate helmet and backpack,
   sturdy boots and gloves, neutral A-pose for rigging. Color variants can serve all players.
2. **Mossback harness kit** — straps, buckles, attachment rings, fitted saddle supports,
   modular wooden decking, rope ladders and a boarding/extraction platform.
   Fit this to the imported creature rather than generating a second creature inside the harness.
3. **Bramblemite** — small readable whimsical pest, rounded body and distinct legs;
   neutral pose for crawling and capture animation.
4. **Pest vacuum** — handheld nozzle, grip, hose connector and detachable containment canister.
5. **Grooming tool** — oversized brush/comb with a clear handle and chunky teeth.
6. **Soothing sprayer** — handheld nozzle with grip and detachable fluid tank.
7. **Rope launcher and anchor kit** — launcher, hook, anchor post, attachment ring,
   carabiner and reel. Rope itself is drawn procedurally in the game.
8. **Care targets** — several fur knots, burrs and removable twig/branch pieces.
   A hot spot is primarily a material/decal/VFX task, not a separate solid mesh.
9. **Guild supplies** — supply crate, treatment bottle, spare canister and tool case.
10. **Extraction beacon** — compact guild signal lantern or portable beacon.

## Environment pass

- Three to five stylized tree variants, with separate trunk and canopy where useful.
- Rock/boulder set and a modular stone arch kit.
- Grass, moss, fern, shrub and flower clumps.
- Boarding dock with posts, stairs, railings and signboard.
- Distant hills can remain terrain/procedural meshes; water and clouds chiefly need shaders/VFX.

## Later expansion (not required for the current three-job playable)

- Portable scanner, multi-grabber/tongs, rescue glider, winch and snack launcher.
- Survey drone, lost equipment props and baby-creature retrieval target.
- Guild base/workshop furniture and additional technician cosmetics.
- Shellstrider second creature; Skywhale is a stretch goal.

## Asset handoff

Supply textured GLB or FBX, one asset per file, neutral pose, no pedestal or scenery.
Keep movable pieces separate and label them. Match Mossback's stylized palette.
Characters and pests still need rigging/animation after mesh generation; Mossback
needs a skeleton, weight painting and breathing/walk/sneeze/shake/ear animations.
Review topology and LOD needs before production use.


## Source: Docs/GiantTiny-Import-Report.md

# GiantTiny model import

The subsequent [map redecoration](Map-Redecoration.md) supersedes the initial placement, terrain, deck and prototype-marker notes below.

The [November 13 replacements](Replacement-Models-2026-11-13.md) supersede the original Mossback, fur, deck/ladder, canister, rope-launcher, beacon and glider entries.

Source: `GiantTiny/`, 28 GLB files supplied by the user. Originals and reference PNGs remain untouched.

## Unity assets

Library: `Assets/_TinyGiantProblems/Art/Imported/GiantTiny/`.
Each folder contains the extracted OBJ, embedded base-color texture, URP material, and prefab.
Individual prefabs have a ground-level, centered pivot and a uniformly normalized longest dimension of one unit. Scene instances receive their own appropriate sizes. All assets are static: the supplied files have no skeletons or animation clips.

`python Tools/import_gianttiny.py --export` reproducibly extracts the sources (Python dependencies: NumPy, SciPy, Pillow). Unity menu **Tiny Giant Problems > Prepare GiantTiny Library** builds materials and prefabs. **Apply GiantTiny Models** updates the playable scene. The scene builder also applies the prepared library when present.

## Multi-item sources

| Source | Separate prefabs | Notes |
|---|---|---|
| trees.glb | broad-tree, tall-tree, bent-tree | Detached leaf islands assigned to nearest tree. |
| rocks.glb | rock-arch, large-rock, medium-rock, small-rock | Four independent pieces. |
| Technician cosmetic kit.glb | hat, scarf, pouch | Ready for fitting to a future rig; not automatically worn. |
| Guild workshop furniture kit.glb | shelf, workbench, stool | Three independent pieces. |
| chest.glb | open-chest, closed-chest | Separate static variants, not an animated hinged chest. |
| Platforrms.glb | lower-platform (railed), upper-platform (plain) | The reference ladder is absent from the generated mesh. Existing gameplay ladders remain. |
| plants.glb | plant-group (leafy shrub), fern, flowers, moss | Four usable groups, not five independent reference plants. Fern and flower textures have pale/unfinished areas; only shrub and moss placed. |

The complete original collection prefab remains beside its split pieces for inspection. Use individual pieces in scenes. Partitioning retains every triangle; small detached details are assigned by nearest mesh distance. Kit export asserts total triangle conservation. The inventory records component bounds and counts.

## Playable scene

- New attachment-free Mossback replaces the previous creature art. The creature frame remains at the requested 3× scale and the technician stays human-sized. The new, shorter body is fitted to local dimensions 18 × 18 × 42 (world 54 × 54 × 126), feet on the meadow. It is not stretched to the old model's 180-metre length.
- Imported plain deck meshes replace primitive deck visuals; existing stable collision surfaces, climbing walls, anchors, checkpoint and care rules remain. Decks are fitted to those collision surfaces. Prototype ladder visuals remain because no ladder mesh was supplied.
- Bramblemite and tangled fur replace their care targets. The soothing hot spot remains its existing marker; a burr is not substituted for that different task.
- Vacuum, grooming brush and soothing sprayer switch with the selected tool. Remote crew visuals use their transmitted tool selection and compensate for the giant parent's scale.
- Closed chest, tool case, bottle, canister and cleaned beacon are placed on the harness. Handled supplies compensate for the creature's inherited scale (chest 1.2 m, case 0.75 m, bottle 0.35 m, canister 0.6 m along their longest dimension); the old crate collider is resized to match. The extraction marker still controls extraction.
- Three tree variants, four rock variants, shrub and moss replace or supplement meadow scenery. The boarding dock is ground-side scenery; no boarding mechanic was added.

## Prepared, not active gameplay

Baby creature, branch, burr cluster, glider, multi-grabber, portable scanner, rescue winch, snack launcher, survey drone, workshop furniture, cosmetics, open chest and railed platform are available as individual prefabs. They do not introduce new mechanics.

`textured_mesh.glb` was visually identified as a second Mossback with a built-in harness. It is retained in the library, not placed alongside the attachment-free creature. No technician body or rope-launcher GLB was supplied; the technician retains its prototype body and tether retains its existing visual.

## Source quality and verification

All 28 sources contain 40,000 triangles each. Repeated environmental pieces reuse split meshes/materials. Further LOD/retopology is appropriate before a production optimization pass.

The original extraction-beacon mesh contains 51 disconnected islands and visible floating debris. `beacon-clean` is a separately exported variant using the main 31,618-triangle body. Original geometry is retained for comparison. This removes debris but does not reconstruct missing surface detail.

Source and split meshes were rendered in Unity and compared with supplied references. All kit partitions preserve original UVs/normals and their total triangle counts. Generated preview sheets are in `tmp/gianttiny/`; the scene preview is `Screenshots/Mossback-Imported.png`.

The 16-check expedition smoke test passed in the live Editor, including attachment, walking, climbing, tethering, care and extraction. Its movement checks now account for frame scale; its original drift tolerance is explicitly 0.25 creature-local units (0.75 world metres at 3×). Live checks also confirmed the selected sprayer appears alone, remote crew stay human-sized and display their transmitted tool, and scene renderers have no missing materials. Remote visual checks used a synthetic pose, not a new multiplayer connection test.

After the final prop-size and plank-alignment adjustment, a further live check confirmed a grounded spawn with zero recoveries and the resized crate collider. Windows development build succeeded with zero errors and zero warnings (239,162,997 bytes), at `Builds/Windows/TinyGiantProblems.exe`. The Editor was left out of Play mode with the scene saved.


## Source: Docs/Map-Redecoration.md

# Meadow redecoration

For the subsequent supplied-model replacements, see [Replacement models](Replacement-Models-2026-11-13.md).

The playable map now uses the supplied GiantTiny models throughout its environment and expedition fittings.

- Removed the primitive meadow plane, sphere hills/clouds, cylinder trees, gold trail blocks/posts, target rings, extraction disc, and box-ladder art. Hidden collision objects remain only where gameplay needs them.
- Sculpted a 500 m terrain clearing with softly rolling outskirts, grass/earth materials and a winding ground trail. Imported trees, rock formations, shrubs and moss sit on sampled terrain heights; trunks remain upright.
- Built a guild camp from the boarding dock, workshop furniture, chests, equipment, beacon, parked survey drone and baby creature. These are scenery, not newly implemented missions or tool mechanics.
- Replaced stretched deck slabs with separate modules along a narrower, gently ramped route. Heights follow a conservative surface envelope measured from the supplied Mossback mesh. Each ramp has its own aligned collider. Side access platforms connect to two pairs of rope ladders.
- Replaced safety posts with scaled rescue winches. Equipment and care models have surface-aligned pivots and slopes; the target idle rotation no longer pushes their bases through the planks. The soothing target is an amber-tinted moss patch, not a floating sphere.
- Fitted the supplied hat, scarf and pouch to the technician. Its underlying body remains the prototype because no technician-body model was supplied.
- Reduced the harsh lighting and replaced floating cloud geometry with a sky material. Mossback retains its 3× frame scale.

Reapply with **Tiny Giant Problems > Redecorate Meadow**. The imported-model application and scene builder invoke this pass as well. `MeadowDecorator.Validate()` checks legacy visual removal, terrain placement and anchor placement.

Validation: all 17 live expedition checks passed, including an end-to-end walk over every ramp/seam, attachment, climbing, tethering, care, extraction and replay. The scene audit confirmed 260 grounded scenery roots, eight aligned anchors, and no legacy map visuals. Scenery previews: `Screenshots/Redecorated-Camp.png`; playable close-up: `Screenshots/Redecorated-Walkway.png`.

The updated Windows development player was successfully built at `Builds/Windows/TinyGiantProblems.exe`; build evidence is in `TestResults/redecorated-build.log`. Later Mossback face images are design references only and have not replaced the in-game GLB.


## Source: Docs/Replacement-Models-2026-11-13.md

# Replacement models applied

The seven replacement GLBs supplied on November 13 are imported from `GiantTiny/`, with originals unchanged. Each imported folder's `SourceMaterial.json` records the source SHA-256; all seven hashes were checked against the supplied files.

| Source | Application |
|---|---|
| mossback.glb | Replaces the creature body. Preserves the 3× creature frame and fits the established footprint. Walkway elevations were measured again against the new coat. |
| tangled fur clump.glb | Replaces the groomer care target. |
| Decking and rope ladder kit.glb | Split into plain deck, railed deck and rope ladder, retaining all 699,602 source triangles. Deck tilt and yaw were removed before normalization; the ladder was straightened. New deck modules replace the old walkway art, imported ladder sections replace the generated ladder geometry, and the railed deck is displayed at camp. |
| spare canister.glb | Replaces the harness supply canister at human-handled scale. |
| rope launcher.glb | Added to tool slot 4 (Tether), including remote-crew tool display. Existing tether mechanics remain authoritative. |
| Extraction beacon.glb | Replaces the harness and camp beacons. The replacement is one connected mesh; no debris removal was needed. The legacy `beacon-clean` prefab now contains the complete replacement geometry. |
| glider.glb | Updated prefab and placed as equipment at the guild camp. No flight mechanic was added. |

Base color, normal, metallic/roughness and ambient-occlusion data are preserved. Metallic and roughness channels are repacked for URP metallic/smoothness use. Each replacement source has approximately 700,000 triangles; this pass retains source geometry rather than claiming to be an LOD/retopology pass. Repeated pieces share meshes/materials.

The start landing is level; adjoining modules follow the new back profile. Imported equipment bases follow their deck slopes. Moving creature colliders are synchronized before technician movement so ground casts use the current deck pose between physics ticks. The technician controller's minimum-move threshold is zero so it does not discard tiny platform-motion deltas at high frame rates.

Reimport: `python Tools/import_gianttiny.py --export --only=mossback,tangled-fur-clump,decking-and-rope-ladder-kit,spare-canister,rope-launcher,extraction-beacon,glider`, then `GiantTinyAssets.PrepareUpdated()` and `GiantTinyAssets.Apply()` through the Unity Editor. Individual kit geometry is aligned during extraction and its measured plank height is saved beside the OBJ.

The scene alignment audit passes: 262 grounded scenery roots, eight anchors on walkable surfaces and no legacy map visuals. Screenshots: `Screenshots/Replacement-Models-Walkway.png`, `Screenshots/Mossback-Imported.png`, and `Screenshots/Redecorated-Camp.png`.

All 17 expedition smoke checks pass with the replacements and the movement fix: the 90-second moving-frame check, complete ramp traversal, climbing, tethering, care, extraction and replay. Separate live checks confirm slot 4 activates only the rope launcher and a synthetic remote pose displays it at human scale. This is a remote-visual check, not a new multi-process networking test.

Final alignment places the supplies and harness beacon entirely on the level starting landing. Upright equipment and the walkable surface below extraction were verified after saving. The final Windows build succeeded with zero errors and zero warnings (515,989,598 bytes), including this placement adjustment. Player: `Builds/Windows/TinyGiantProblems.exe`.


## Source: Docs/Harness-Polish-And-Idle-Motion.md

# Harness polish and Mossback idle motion

`MeadowPolish.Apply()` adds timber braces under all four access bridges, warm rope handrails with brass post collars, and continuous ladder side ropes tied into the landings. Rail openings preserve the existing branch routes. These details are visual rigging, not new blocking walls. Three shared generated meshes hold the new rigging. The decorator invokes this pass automatically.

Camp now has a worn earth clearing connected to the approach trail and irregular planting groups. Terrain weights use normalized SmoothStep inputs; the previous use of SmoothStep as an inverse-range function suppressed the dirt paint. The existing imported furniture, equipment and creature models remain in place.

Mossback now has a 7.5-second breathing cycle, slow weight shifts and small turns, a warning tremor, and a decaying reaction shudder. The entire creature frame moves together, including the mesh collider, walkways, rigging and tether sockets. The motion pivots at ground level. This replaces the previous broad sideways sliding motion and independent visual shaking that could separate the body from its platforms.

This is a procedural whole-body idle animation, not a skeletal walking cycle or separately articulated head/legs. The supplied GLB has no skeleton. Breathing amount, period and weight-shift angle are exposed on CreatureFrame.

Validation: all 17 expedition smoke checks passed with the new motion enabled, including 90-second attachment, complete ramp traversal, climbing and extraction. Successive live frame samples confirm both breathing scale and rotation change. All existing 262 scenery roots and eight anchors pass the alignment audit; all 14 added border plants also pass ground alignment. The solid Mossback collider remains enabled. The three rigging meshes contain 43,200 triangles in total.


## Source: Docs/Mossback-Collision.md

# Mossback body collision

The imported body previously had no collider: only authored walkway and climb surfaces blocked movement. A dedicated non-trigger MeshCollider now follows the fitted body and its visual motion. Its 12,000-triangle mesh is simplified from the supplied GLB, retaining the creature silhouette rather than filling the spaces between its legs with a bounding box. Render geometry is unchanged.

Regenerate with `python Tools/build_mossback_collision.py` (NumPy, SciPy and fast-simplification), then call `MossbackCollision.Apply()` in the Editor. The regular `GiantTinyAssets.Apply()` also installs the collider. Regenerate this mesh when the source Mossback changes.

Validation: collider raycasts hit both flanks, front, rear and top. Temporary CharacterControllers driven toward all four sides reported collision contact. The full 17-check expedition smoke suite passed with collision enabled, including moving-platform attachment, entire ramp route, climbing, care and extraction. Surface probes may slide along the irregular coat; a flat-plane distance assertion is not a valid penetration test for this shape.

This fixes body solidity. Harness construction, scenery composition and general visual polish remain separate work.


## Source: Docs/Mossback-Skeleton.md

# Mossback skeletal animation

The stronger head turns and distinct sneeze/shake Animator states are described in [Mossback Animator Behaviors](Mossback-Animator-Behaviors.md), which supersedes the initial small-motion tuning below.

The supplied GLB is preserved. `MossbackRigBuilder.Apply()` creates a 21-joint skeleton and two skinned mesh assets from the imported geometry: the original detailed surface (467,526 imported vertices) and the simplified physics surface. All skin weights are normalized with at most four influences per vertex. The static renderer and original static body collider are disabled; the animated renderer and collider replace them.

Joints: spine, neck, head, jaw, two ears, three tail joints, and hip/knee/foot chains for all four legs. Joint placement follows the model's staggered stance and turned head. The upper back remains rooted to preserve the authored harness and traversal surfaces.

`MossbackSkeleton` drives slow head looks and nods, subtle closed-mouth chewing, staggered ear flicks, tail sway, and one-hoof-at-a-time lifts and settling. Two-bone leg positioning adjusts the feet to the meadow height. This is a living stationary idle, not a traveling walk. The closed source mouth has no modeled oral cavity, so the jaw movement is restrained rather than exposing a nonexistent interior.

The physics mesh is skinned and baked at 20 Hz before technician movement; the render mesh uses normal GPU skinning. BakeMesh scale compensation preserves the existing 3x creature scale. Existing whole-frame breathing and reactions continue around the articulated motion.

Reapply with `MossbackRigBuilder.Apply()`; the standard model application also invokes it. Verify with `MossbackRigBuilder.Validate()` and `ValidateMotion()`. Six sampled poses confirm head, jaw and all four feet deform, with less than 0.00002 local units of drift in fully rooted back vertices. The maximum sampled edge stretch is 1.815x; the ankle transition was widened to remove a sharper stretch found in the initial pass.

All 17 expedition smoke checks passed in Play mode with skeletal animation and collision updates enabled. Successive live samples verified changing head/jaw rotations and advancing frame counts. Animated collider raycasts hit both flanks, front, rear and top. The 16-second `Screenshots/Mossback-Skeleton-Preview.mp4` shows the skeletal idle cycle at 16 fps; it is an offline pose preview, while the game evaluates poses every frame.

The PC render pipeline shadow distance is increased from 50 to 220 world units so Mossback casts a grounding shadow at the game's giant scale and overview camera distance.

The updated Windows player is built at `Builds/Windows/TinyGiantProblems.exe`.


## Source: Docs/Mossback-Animator-Behaviors.md

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


## Source: Docs/World-Collision-And-Bramblemite.md

# Solid world props and animated bramblemite

Imported prop prefabs now carry simplified non-convex collision meshes, capped at 2,500 triangles per source mesh. These preserve openings in arches and furniture instead of filling the objects with bounding boxes. The scene audit finds 295 solid world prop instances, including trees, rocks, plants, camp furniture, supplies, care targets and equipment. Harness rails and braces are also solid.

58 instances use an explicit exception: held/worn art cannot collide with its technician, and deck/ladder art uses the existing fitted traversal collision rather than a second overlapping mesh. Remote technician visuals inherit the disabled colliders from the local visual template. This pass does not make scenery into movable rigid bodies.

The bramblemite prefab has an 18-joint skeleton: shell/thorax, head, two two-joint antennae, and hip/knee chains for six legs. Its skinned render mesh preserves the supplied materials. A separate skinned physics mesh follows the pose at 20 Hz. Idle animation adds leg taps, antenna movement and gentle head/body motion; vacuuming increases its wriggling. Completing the care task disables its collision while the existing care system shrinks the visual; replay restores solidity.

Regeneration:

1. Run `python Tools/build_prop_collision.py` after changing imported OBJ geometry (NumPy and fast-simplification required). The manifest caches source hashes.
2. Run `WorldCollisionBuilder.ApplyLibrary()` in the Unity Editor to update prefab colliders and the bramblemite rig.
3. The model preparation pipeline restores collision/rigging when the collision manifest exists. Map redecoration calls `ApplyScene()` to restore instance collision policy and rigging surfaces.

Validation: 18 normalized, weighted bramblemite joints move within bounded deformation; 295 world props pass the collider audit. Ray/CharacterController probes contact trees, rocks, rock arch, workbench, chest, beacon, bramblemite and canister. Arch probes confirm an open center with solid sides. All 17 expedition checks pass with the new solids. Completed-target and replay collision checks also pass.

All 15 Mossback reaction checks also pass with the new prop/rail colliders. A live vacuum-use probe completed the bramblemite care task and confirmed its animation clock accelerates during treatment.

Preview: `Screenshots/Bramblemite-Animation.mp4` (offline poses from the in-game rig). Windows player: `Builds/Windows/TinyGiantProblems.exe`.

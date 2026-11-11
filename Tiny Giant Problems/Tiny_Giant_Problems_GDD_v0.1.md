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

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

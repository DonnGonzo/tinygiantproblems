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

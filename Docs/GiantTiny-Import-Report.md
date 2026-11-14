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

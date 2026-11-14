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

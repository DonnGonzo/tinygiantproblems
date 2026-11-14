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

# Mossback body collision

The imported body previously had no collider: only authored walkway and climb surfaces blocked movement. A dedicated non-trigger MeshCollider now follows the fitted body and its visual motion. Its 12,000-triangle mesh is simplified from the supplied GLB, retaining the creature silhouette rather than filling the spaces between its legs with a bounding box. Render geometry is unchanged.

Regenerate with `python Tools/build_mossback_collision.py` (NumPy, SciPy and fast-simplification), then call `MossbackCollision.Apply()` in the Editor. The regular `GiantTinyAssets.Apply()` also installs the collider. Regenerate this mesh when the source Mossback changes.

Validation: collider raycasts hit both flanks, front, rear and top. Temporary CharacterControllers driven toward all four sides reported collision contact. The full 17-check expedition smoke suite passed with collision enabled, including moving-platform attachment, entire ramp route, climbing, care and extraction. Surface probes may slide along the irregular coat; a flat-plane distance assertion is not a valid penetration test for this shape.

This fixes body solidity. Harness construction, scenery composition and general visual polish remain separate work.

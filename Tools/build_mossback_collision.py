"""Generate a physics-only mesh in the same coordinates as the rendered OBJ."""
from pathlib import Path
import numpy as np
import fast_simplification
from import_gianttiny import read, ROOT

v, _, _, f, _, _ = read(ROOT / 'GiantTiny/mossback.glb')
v, remap = np.unique(np.round(v, 6), axis=0, return_inverse=True)
f = remap[f]
v, f = fast_simplification.simplify(v, f, target_count=12000)
path = ROOT / 'Assets/_TinyGiantProblems/Art/Collision/MossbackCollision.obj'
path.parent.mkdir(parents=True, exist_ok=True)
with path.with_suffix('.tmp').open('w') as out:
    out.write('o MossbackCollision\n')
    np.savetxt(out, v, fmt='v %.9g %.9g %.9g')
    np.savetxt(out, f + 1, fmt='f %d %d %d')
path.with_suffix('.tmp').replace(path)
print(f'Collision mesh: {len(f)} triangles')

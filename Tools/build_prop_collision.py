"""Bake simplified, non-convex collision surfaces for the imported prop library."""
from pathlib import Path
import hashlib
import json
import numpy as np
import fast_simplification

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/_TinyGiantProblems/Art/Imported/GiantTiny'
DEST = ROOT / 'Assets/_TinyGiantProblems/Art/Collision/Imported'
DEST.mkdir(parents=True, exist_ok=True)
manifest_path = DEST / 'manifest.json'
manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else {}
for source in sorted(SOURCE.rglob('*.obj')):
    if source.parent.name == 'mossback':
        continue  # Already uses its dedicated animated collision mesh.
    key = source.relative_to(SOURCE).as_posix()
    digest = hashlib.sha256(source.read_bytes()).hexdigest()
    target = DEST / key
    if manifest.get(key, {}).get('sha256') == digest and target.exists():
        continue
    vertices, faces = [], []
    with source.open() as stream:
        for line in stream:
            if line.startswith('v '):
                vertices.append(tuple(map(float, line.split()[1:4])))
            elif line.startswith('f '):
                faces.append(tuple(int(part.split('/')[0])-1 for part in line.split()[1:4]))
    v, remap = np.unique(np.round(np.asarray(vertices), 6), axis=0, return_inverse=True)
    f = remap[np.asarray(faces)]
    f = f[(f[:,0] != f[:,1]) & (f[:,1] != f[:,2]) & (f[:,0] != f[:,2])]
    original = len(f)
    if original > 2500:
        v, f = fast_simplification.simplify(v, f, target_count=2500)
    target.parent.mkdir(parents=True, exist_ok=True)
    stage = target.with_suffix('.tmp')
    with stage.open('w') as out:
        out.write('o PropCollision\n')
        np.savetxt(out, v, fmt='v %.9g %.9g %.9g')
        np.savetxt(out, f+1, fmt='f %d %d %d')
    stage.replace(target)
    manifest[key] = dict(sha256=digest, sourceTriangles=original, collisionTriangles=len(f))
    print(f'{key}: {original} -> {len(f)}', flush=True)
manifest_path.write_text(json.dumps(manifest, indent=2))

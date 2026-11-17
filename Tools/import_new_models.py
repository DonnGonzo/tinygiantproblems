"""Decimate and extract the heavy November GLBs (character, claw, mountain).

The source GLBs are 400k+ vertex static meshes. Each is reduced to a game
budget with quadric decimation; UVs and normals follow the surviving vertices
through fast_simplification's collapse replay, so the original textures still
map. Output mirrors import_gianttiny.py: <slug>/<slug>.obj + BaseColor.png +
Normal/MetallicSmoothness/Occlusion maps under Art/Imported/GiantTiny.

Usage: python Tools/import_new_models.py [--only=slug,slug]
"""
import json
import sys
from pathlib import Path

import numpy as np
import fast_simplification as fs

sys.path.insert(0, str(Path(__file__).resolve().parent))
import import_gianttiny as base  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'GiantTiny'
DEST = ROOT / 'Assets/_TinyGiantProblems/Art/Imported/GiantTiny'
# source file -> (slug, target triangles, island pick)
# The character and claw GLBs are turnaround sheets (three views of the
# technician plus colour swatches; a glove claw plus loose parts), so only one
# island is kept: the front-view figure in the middle, or the largest piece.
MODELS = {
    'Character.glp.glb': ('technician', 22000, 'center'),
    'claws.glb': ('climbing-claw', 5000, 'largest'),
    'mountain.glb': ('mountain', 10000, None),
}


def pick_island(v, f, mode):
    if not mode:
        return f
    components, labels = base.islands(v, f)
    if mode == 'largest':
        keep = components[0]
    else:
        big = [c for c in components if c['faces'] > components[0]['faces'] * .3]
        keep = min(big, key=lambda c: abs(c['center'][0]))
    return f[labels == keep['label']]


def weld(v, n, uv, f):
    """Merge vertices by position so the decimator sees one watertight surface
    (splitting at UV seams opened visible cracks). UVs are re-derived per face
    after decimation, so the seam split is not needed here."""
    key = np.round(v, 6)
    _, first, inverse = np.unique(key, axis=0, return_index=True, return_inverse=True)
    return v[first], n[first], uv[first], inverse.reshape(-1)[f]


def decimate(v, n, uv, f, target):
    """Quadric-decimate, then re-derive texture coordinates per face.

    Each simplified triangle samples UVs from the nearest source triangle,
    via barycentric projection of its corners, so a face never straddles two
    UV islands (straddling faces showed the texture's white gutter as cracks).
    Faces are emitted unwelded; Unity recomputes smooth normals on import."""
    from scipy.spatial import cKDTree
    if len(f) > target:
        reduction = 1 - target / len(f)
        pts, faces = fs.simplify(v.astype(np.float64), f.astype(np.int64), target_reduction=reduction, agg=5)
    else:
        pts, faces = v.astype(np.float64), f.astype(np.int64)
    tri = v[f].astype(np.float64)
    tree = cKDTree(tri.mean(1))
    corners = pts[faces]
    _, nearest = tree.query(corners.mean(1), k=6)
    # Prefer the candidate whose plane best matches the new face (smallest
    # corner-to-plane error), which avoids grabbing a face from a fold behind.
    best = nearest[:, 0].copy()
    err_best = np.full(len(faces), np.inf)
    for k in range(nearest.shape[1]):
        cand = nearest[:, k]
        a, b, c = tri[cand, 0], tri[cand, 1], tri[cand, 2]
        nrm = np.cross(b - a, c - a); nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-12)
        err = np.abs(((corners - a[:, None]) * nrm[:, None]).sum(2)).sum(1) + np.linalg.norm(corners.mean(1) - tri[cand].mean(1), axis=1)
        take = err < err_best
        best[take] = cand[take]; err_best[take] = err[take]
    a, b, c = tri[best, 0], tri[best, 1], tri[best, 2]
    v0, v1 = b - a, c - a
    d00 = (v0 * v0).sum(1); d01 = (v0 * v1).sum(1); d11 = (v1 * v1).sum(1)
    denom = np.maximum(d00 * d11 - d01 * d01, 1e-18)
    out_v, out_uv = [], []
    for corner in range(3):
        p = corners[:, corner] - a
        d20 = (p * v0).sum(1); d21 = (p * v1).sum(1)
        bv = (d11 * d20 - d01 * d21) / denom; bw = (d00 * d21 - d01 * d20) / denom
        bv = np.clip(bv, 0, 1); bw = np.clip(bw, 0, 1)
        over = bv + bw > 1
        sm = bv[over] + bw[over]; bv[over] /= sm; bw[over] /= sm
        bu = 1 - bv - bw
        fu = uv[f[best]]
        out_uv.append(bu[:, None] * fu[:, 0] + bv[:, None] * fu[:, 1] + bw[:, None] * fu[:, 2])
        out_v.append(corners[:, corner])
    V = np.stack(out_v, 1).reshape(-1, 3).astype(np.float32)
    UV = np.stack(out_uv, 1).reshape(-1, 2).astype(np.float32)
    fn = np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0])
    fn /= np.maximum(np.linalg.norm(fn, axis=1, keepdims=True), 1e-12)
    N = np.repeat(fn, 3, axis=0).astype(np.float32)
    F = np.arange(len(V)).reshape(-1, 3)
    return V, N, UV, F


def main():
    only = next((a.split('=', 1)[1].split(',') for a in sys.argv if a.startswith('--only=')), None)
    report = {}
    for name, (slug, target, pick) in MODELS.items():
        if only and slug not in only:
            continue
        path = SOURCE / name
        v, n, uv, f, texture, mat = base.read(path)
        tris = len(f)
        f = pick_island(v, f, pick)
        used = np.unique(f)
        remap = np.zeros(len(v), dtype=np.int64); remap[used] = np.arange(len(used))
        v, n, uv, f = v[used], n[used], uv[used], remap[f]
        v, n, uv, f = weld(v, n, uv, f)
        v, n, uv, f = decimate(v, n, uv, f, target)
        folder = DEST / slug
        folder.mkdir(parents=True, exist_ok=True)
        base.export_obj(folder / (slug + '.obj'), v, n, uv, f)
        (folder / 'BaseColor.png').write_bytes(texture)
        base.export_surface_maps(path, folder)
        report[slug] = dict(source=name, sourceTriangles=int(tris), triangles=int(len(f)), vertices=int(len(v)),
                            bounds=[v.min(0).tolist(), v.max(0).tolist()])
        print(slug, report[slug], flush=True)
    out = ROOT / 'Docs/New-Model-Import.json'
    old = json.loads(out.read_text()) if out.exists() else {}
    old.update(report)
    out.write_text(json.dumps(old, indent=2))


if __name__ == '__main__':
    main()

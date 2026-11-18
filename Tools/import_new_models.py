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
    'floaring platform.glb': ('floating-platform', 12000, None),
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



# ---- technician colour regions ---------------------------------------------
# The generated texture's tiny UV islands speckle after decimation, so the
# technician is exported as clean per-part submeshes instead (OBJ usemtl
# groups). Unity gives one material per region, which the in-game Guild Member
# editor recolours. Order matters: it is the submesh/material slot order.
REGIONS = ['Hat', 'Jacket', 'Trousers', 'Gloves', 'Boots', 'Belt', 'Accents', 'Goggles']


def technician_regions(src_v, src_f, src_uv, texture, V, F):
    import io
    from PIL import Image
    from scipy.spatial import cKDTree
    im = np.asarray(Image.open(io.BytesIO(texture)).convert('RGB')).astype(np.float32) / 255
    h, w, _ = im.shape
    c = src_uv[src_f].mean(1)
    col = im[np.clip((c[:, 1] * h).astype(int), 0, h - 1), np.clip((c[:, 0] * w).astype(int), 0, w - 1)]
    r, g, b = col[:, 0], col[:, 1], col[:, 2]
    luma = .3 * r + .59 * g + .11 * b
    # colour family per source face: 0 green, 1 maroon, 2 gold, 3 tan accent, 4 dark
    fam = np.full(len(col), 2)
    fam[(g > r * 1.4) & (g > b * 1.05)] = 0
    fam[(r > g * 2.2) & (b > g * .9)] = 1
    fam[(r > .55) & (g > .42) & (b > .18) & (b > g * .3)] = 3
    fam[luma < .055] = 4
    # normalised technician space: height 1 from the boots, x centred
    mn, mx = src_v.min(0), src_v.max(0)
    scale = 1 / (mx - mn).max()
    def norm(p):
        q = (p - [(mn[0] + mx[0]) / 2, mn[1], (mn[2] + mx[2]) / 2]) * scale
        return q
    src_c = src_v[src_f].mean(1)
    tree = cKDTree(src_c)
    dst_c = V[F].mean(1)
    _, near = tree.query(dst_c, k=24)
    votes = np.zeros((len(F), 5))
    for k in range(near.shape[1]):
        np.add.at(votes, (np.arange(len(F)), fam[near[:, k]]), 1)
    family = votes.argmax(1)
    q = norm(dst_c); y = q[:, 1]; ax = np.abs(q[:, 0])
    label = np.full(len(F), 1)
    label[(family == 0) & (y > .60)] = 0
    label[(family == 0) & (y <= .60)] = 1
    label[family == 1] = 2
    gold = family == 2
    label[gold & (ax > .195) & (y > .29) & (y < .52)] = 3
    label[gold & (y < .21)] = 4
    label[gold & ~((ax > .195) & (y > .29) & (y < .52)) & ~(y < .21)] = 5
    label[family == 3] = 6
    label[(family == 4) & (y > .55)] = 7
    label[(family == 4) & (y <= .55)] = 5
    # smooth: majority of neighbouring faces (shared corner positions), twice
    key = {}
    corners = np.round(V[F].reshape(-1, 3), 5)
    _, cid = np.unique(corners, axis=0, return_inverse=True)
    cid = cid.reshape(-1, 3)
    from collections import defaultdict
    byc = defaultdict(list)
    for fi, tri in enumerate(cid):
        for cc in tri: byc[cc].append(fi)
    for _ in range(2):
        new = label.copy()
        for fi, tri in enumerate(cid):
            nb = [label[o] for cc in tri for o in byc[cc]]
            counts = np.bincount(nb, minlength=len(REGIONS))
            if counts[label[fi]] * 3 < counts.max(): new[fi] = counts.argmax()
        label = new
    return label


def export_obj_regions(path, vertices, normals, uvs, faces, labels):
    staging = path.with_suffix('.obj.tmp')
    with staging.open('w', newline='\n') as out:
        out.write('# Technician, decimated, one usemtl group per colour region\nmtllib ' + path.stem + '.mtl\no ' + path.stem + '\n')
        np.savetxt(out, vertices, fmt='v %.9g %.9g %.9g')
        uv = uvs.copy(); uv[:, 1] = 1 - uv[:, 1]
        np.savetxt(out, uv, fmt='vt %.9g %.9g')
        np.savetxt(out, normals, fmt='vn %.9g %.9g %.9g')
        for i, name in enumerate(REGIONS):
            sel = faces[labels == i]
            out.write('usemtl ' + name + '\n')
            if len(sel) == 0:
                # keep every slot present so submesh order stays fixed
                sel = faces[:1]
            for tri in sel + 1:
                out.write('f ' + ' '.join(f'{v}/{v}/{v}' for v in tri) + '\n')
    staging.replace(path)
    # Unity only splits OBJ groups into submeshes when a material library exists.
    with path.with_suffix('.mtl').open('w', newline='\n') as mtl:
        for name in REGIONS:
            mtl.write('newmtl ' + name + '\nKd 0.8 0.8 0.8\n\n')


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
        src = (v, f, uv)
        v, n, uv, f = weld(v, n, uv, f)
        v, n, uv, f = decimate(v, n, uv, f, target)
        folder = DEST / slug
        folder.mkdir(parents=True, exist_ok=True)
        if slug == 'technician':
            labels = technician_regions(src[0], src[1], src[2], texture, v, f)
            export_obj_regions(folder / (slug + '.obj'), v, n, uv, f, labels)
            print('regions', {name: int((labels == i).sum()) for i, name in enumerate(REGIONS)})
        else:
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

"""Extract the supplied static GLB to Unity's native OBJ/PNG inputs (stdlib only)."""
import json
from pathlib import Path
import shutil
import struct
import sys

source = Path(sys.argv[1])
root = Path(__file__).resolve().parents[1]
dest = root / 'Assets/_TinyGiantProblems/Art/Creatures/Mossback'
dest.mkdir(parents=True, exist_ok=True)
data = source.read_bytes()
magic, version, length = struct.unpack_from('<III', data)
assert magic == 0x46546C67 and version == 2 and length == len(data)
json_size, kind = struct.unpack_from('<II', data, 12)
doc = json.loads(data[20:20 + json_size])
binary_offset = 20 + json_size + 8
binary = data[binary_offset:]
assert not doc.get('skins') and not doc.get('animations'), 'Use a rig-preserving importer for animated assets'
assert len(doc['meshes']) == 1 and len(doc['meshes'][0]['primitives']) == 1
assert all(not any(k in n for k in ('matrix', 'translation', 'rotation', 'scale')) for n in doc['nodes'])

def accessor(index):
    a = doc['accessors'][index]
    view = doc['bufferViews'][a['bufferView']]
    count = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3}[a['type']]
    fmt = '<' + {5126: 'f', 5125: 'I', 5123: 'H'}[a['componentType']] * count
    stride = view.get('byteStride', struct.calcsize(fmt))
    offset = view.get('byteOffset', 0) + a.get('byteOffset', 0)
    return [struct.unpack_from(fmt, binary, offset + i * stride) for i in range(a['count'])]

p = doc['meshes'][0]['primitives'][0]
assert p.get('mode', 4) == 4
positions = accessor(p['attributes']['POSITION'])
normals = accessor(p['attributes']['NORMAL'])
uvs = accessor(p['attributes']['TEXCOORD_0'])
indices = [v[0] + 1 for v in accessor(p['indices'])]
with (dest / 'Mossback.obj').open('w', newline='\n') as out:
    out.write('# Extracted from user-supplied mossback.glb; right-handed source coordinates\no Mossback\n')
    for v in positions:
        out.write('v %.9g %.9g %.9g\n' % v)
    for u, v in uvs:
        out.write('vt %.9g %.9g\n' % (u, 1-v))
    for n in normals:
        out.write('vn %.9g %.9g %.9g\n' % n)
    for i in range(0, len(indices), 3):
        out.write('f ' + ' '.join(f'{j}/{j}/{j}' for j in indices[i:i+3]) + '\n')
image = doc['images'][0]
assert image['mimeType'] == 'image/png'
view = doc['bufferViews'][image['bufferView']]
offset = view.get('byteOffset', 0)
(dest / 'Mossback_BaseColor.png').write_bytes(binary[offset:offset + view['byteLength']])
archive = root / 'SourceAssets/Mossback'
archive.mkdir(parents=True, exist_ok=True)
shutil.copy2(source, archive / 'mossback.glb')
print(f'Extracted {len(positions)} vertices, {len(indices)//3} triangles, base-color texture; original preserved in SourceAssets/Mossback.')

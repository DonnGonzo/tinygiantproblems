"""Inspect and extract the user's static GLBs without modifying source files.

UVs and normals are retained. Spatial kit grouping is explicitly authored after
inspection, never inferred from filenames alone. All exported pieces share the
original texture. See Docs/GiantTiny-Import-Report.md for integration decisions.
"""
import json
from pathlib import Path
import struct
import sys
import hashlib
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components
from scipy.spatial import cKDTree

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'GiantTiny'
DEST=ROOT/'Assets/_TinyGiantProblems/Art/Imported/GiantTiny'
AUDIT=ROOT/'tmp/gianttiny'
# Largest connected pieces, ordered by face count. Small detached details are
# assigned to their nearest primary mesh, retaining every source triangle.
KITS={
    'decking-and-rope-ladder-kit':['railed-deck','plain-deck','rope-ladder'],
    'trees':['broad-tree','tall-tree','bent-tree'],
    'rocks':['rock-arch','large-rock','medium-rock','small-rock'],
    'technician-cosmetic-kit':['hat','scarf','pouch'],
    'guild-workshop-furniture-kit':['shelf','workbench','stool'],
    'chest':['open-chest','closed-chest'],
    'platforrms':['lower-platform','upper-platform'],
    'plants':['plant-group','fern','flowers','moss'],
}

def export_surface_maps(path,folder):
    from PIL import Image,ImageChops
    import io
    data=path.read_bytes();size=struct.unpack_from('<I',data,12)[0]
    doc=json.loads(data[20:20+size]);binary=data[28+size:];mat=doc['materials'][0]
    def texture(index):
        im=doc['images'][doc['textures'][index]['source']];v=doc['bufferViews'][im['bufferView']];offset=v.get('byteOffset',0)
        return Image.open(io.BytesIO(binary[offset:offset+v['byteLength']])).convert('RGB')
    pbr=mat['pbrMetallicRoughness']
    if 'metallicRoughnessTexture' in pbr:
        orm=texture(pbr['metallicRoughnessTexture']['index']);r,g,b=orm.split();zero=Image.new('L',orm.size,0)
        Image.merge('RGBA',(b,zero,zero,ImageChops.invert(g))).save(folder/'MetallicSmoothness.png')
        Image.merge('RGB',(r,r,r)).save(folder/'Occlusion.png')
    if 'normalTexture' in mat: texture(mat['normalTexture']['index']).save(folder/'Normal.png')
    (folder/'SourceMaterial.json').write_text(json.dumps(dict(source=path.name,sha256=hashlib.sha256(data).hexdigest(),material=mat),indent=2))

def read(path):
    data=path.read_bytes()
    magic,version,length=struct.unpack_from('<III',data)
    assert magic==0x46546c67 and version==2 and length==len(data)
    size,kind=struct.unpack_from('<II',data,12)
    doc=json.loads(data[20:20+size]); binary=data[28+size:]
    assert not doc.get('skins') and not doc.get('animations')
    assert len(doc['meshes'])==1 and len(doc['meshes'][0]['primitives'])==1
    assert not doc.get('extensionsRequired')
    assert all(not any(k in n for k in ('matrix','translation','rotation','scale')) for n in doc['nodes'])
    def accessor(i):
        a=doc['accessors'][i]; v=doc['bufferViews'][a['bufferView']]
        assert not a.get('sparse')
        count={'SCALAR':1,'VEC2':2,'VEC3':3}[a['type']]
        dtype=np.dtype({5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']])
        stride=v.get('byteStride',count*dtype.itemsize)
        return np.ndarray((a['count'],count),dtype=dtype,buffer=binary,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(stride,dtype.itemsize)).copy()
    p=doc['meshes'][0]['primitives'][0]
    assert p.get('mode',4)==4
    vertices=accessor(p['attributes']['POSITION']); normals=accessor(p['attributes']['NORMAL'])
    uvs=accessor(p['attributes']['TEXCOORD_0']); faces=accessor(p['indices']).reshape(-1,3)
    assert np.isfinite(vertices).all() and faces.max()<len(vertices)
    mat=doc['materials'][p['material']]['pbrMetallicRoughness']
    image=doc['images'][doc['textures'][mat['baseColorTexture']['index']]['source']]
    assert image['mimeType']=='image/png'
    v=doc['bufferViews'][image['bufferView']]; offset=v.get('byteOffset',0)
    texture=binary[offset:offset+v['byteLength']]
    return vertices,normals,uvs,faces,texture,mat

def islands(vertices,faces):
    _,weld=np.unique(np.round(vertices,6),axis=0,return_inverse=True)
    f=weld[faces]
    x=np.concatenate([f[:,0],f[:,1],f[:,2]])
    y=np.concatenate([f[:,1],f[:,2],f[:,0]])
    count,labels=connected_components(coo_matrix((np.ones(len(x)),(x,y)),shape=(weld.max()+1,weld.max()+1)),directed=False)
    face_labels=labels[f[:,0]]
    groups=[]
    for label in np.unique(face_labels):
        mask=face_labels==label; points=vertices[np.unique(faces[mask])]
        groups.append(dict(label=int(label),faces=int(mask.sum()),min=points.min(0).tolist(),max=points.max(0).tolist(),center=points.mean(0).tolist()))
    return sorted(groups,key=lambda g:-g['faces']),face_labels

def export_obj(path,vertices,normals,uvs,faces):
    used,remap=np.unique(faces,return_inverse=True)
    staging=path.with_suffix('.obj.tmp')
    with staging.open('w',newline='\n') as out:
        out.write('# Static GLB extraction, preserves original right-handed coordinates\no '+path.stem+'\n')
        np.savetxt(out,vertices[used],fmt='v %.9g %.9g %.9g')
        uv=uvs[used].copy(); uv[:,1]=1-uv[:,1]
        np.savetxt(out,uv,fmt='vt %.9g %.9g')
        np.savetxt(out,normals[used],fmt='vn %.9g %.9g %.9g')
        for tri in remap.reshape(-1,3)+1:
            out.write('f '+' '.join(f'{v}/{v}/{v}' for v in tri)+'\n')
    staging.replace(path)

def align_piece(name,v,n,f):
    points=v[np.unique(f)]
    if name=='rope-ladder':
        _,axes=np.linalg.eigh(np.cov(points.T));y=axes[:,2];y*=1 if y[1]>0 else -1
        x=axes[:,1];x*=1 if x[0]>0 else -1;z=np.cross(x,y);z/=np.linalg.norm(z);x=np.cross(y,z)
        rotation=np.stack((x,y,z)); fraction=1.
    else:
        p=v[f];cross=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0]);area=np.linalg.norm(cross,axis=1)
        normal=cross/np.maximum(area[:,None],1e-15);up=normal[:,1]>.5
        keys,rev=np.unique(np.round(normal[up]*10).astype(int),axis=0,return_inverse=True)
        group=rev==np.argmax(np.bincount(rev,weights=area[up]))
        direction=np.average(normal[up][group],axis=0,weights=area[up][group]);direction/=np.linalg.norm(direction)
        axis=np.cross(direction,[0,1,0]);c=direction[1];x,y,z=axis;k=np.array([[0,-z,y],[z,0,-x],[-y,x,0]])
        rotation=np.eye(3)+k+k@k/(1+c)
        transformed=points@rotation.T
        # Remove the source image's in-plane yaw as well as its tilt; otherwise
        # axis-aligned bounds leave triangular gaps between repeated deck tiles.
        _,horizontal=np.linalg.eigh(np.cov(transformed[:,[0,2]].T))
        along=horizontal[:,1];along*=1 if along[0]>0 else -1
        yaw=np.array([[along[0],0,along[1]],[0,1,0],[-along[1],0,along[0]]])
        rotation=yaw@rotation
        transformed=points@rotation.T
        plane=np.average((p.mean(1)[up][group]@rotation.T)[:,1],weights=area[up][group])
        fraction=float((plane-transformed[:,1].min())/np.ptp(transformed[:,1]))
    return v@rotation.T,n@rotation.T,dict(plankFraction=fraction,rotation=rotation.tolist())

def main():
    AUDIT.mkdir(parents=True,exist_ok=True)
    report=[]
    only=next((a.split('=',1)[1].split(',') for a in sys.argv if a.startswith('--only=')),None)
    for path in sorted(SOURCE.glob('*.glb')):
        v,n,uv,f,texture,mat=read(path)
        components,labels=islands(v,f)
        slug=path.stem.lower().replace(' ','-')
        row=dict(source=path.name,slug=slug,triangles=len(f),vertices=len(v),bounds=[v.min(0).tolist(),v.max(0).tolist()],components=components)
        report.append(row)
        if '--export' in sys.argv and (only is None or slug in only):
            folder=DEST/slug;folder.mkdir(parents=True,exist_ok=True)
            export_obj(folder/(slug+'.obj'),v,n,uv,f)
            (folder/'BaseColor.png').write_bytes(texture)
            export_surface_maps(path,folder)
            if slug=='extraction-beacon':
                # Separate optional cleaned variant. Original extraction is retained.
                export_obj(folder/'beacon-clean.obj',v,n,uv,f[labels==components[0]['label']])
            if slug in KITS:
                names=KITS[slug]
                primary=components[:len(names)]
                trees=[cKDTree(v[np.unique(f[labels==c['label']])]) for c in primary]
                assigned={c['label']:i for i,c in enumerate(primary)}
                for c in components[len(names):]:
                    points=v[np.unique(f[labels==c['label']])]
                    assigned[c['label']]=int(np.argmin([tree.query(points)[0].min() for tree in trees]))
                counts=[]
                for i,name in enumerate(names):
                    mask=np.isin(labels,[label for label,index in assigned.items() if index==i])
                    pv,pn=v,n
                    if slug=='decking-and-rope-ladder-kit':
                        pv,pn,alignment=align_piece(name,v,n,f[mask]);(folder/(name+'-alignment.json')).write_text(json.dumps(alignment,indent=2))
                    export_obj(folder/(name+'.obj'),pv,pn,uv,f[mask])
                    counts.append(int(mask.sum()))
                assert sum(counts)==len(f), 'Kit partition lost triangles'
                row['pieces']=dict(zip(names,counts))
        print(path.name, 'islands',len(components),'largest',[(c['faces'],[round(x,2) for x in c['center']]) for c in components[:8]])
    (AUDIT/'inventory.json').write_text(json.dumps(report,indent=2))
    (ROOT/'Docs/GiantTiny-Asset-Inventory.json').write_text(json.dumps(report,indent=2))
    # Read-only reference contact sheet; source images remain untouched.
    from PIL import Image,ImageDraw
    images=sorted(SOURCE.glob('*.png'))
    for start in range(0,len(images),12):
        sheet=Image.new('RGB',(1200,1200),'white'); draw=ImageDraw.Draw(sheet)
        for i,path in enumerate(images[start:start+12]):
            with Image.open(path) as im:
                im.thumbnail((290,250)); x=(i%4)*300; y=(i//4)*400
                sheet.paste(im,(x,y));draw.text((x,y+255),path.name.replace('ChatGPT Image ',''),fill='black')
        sheet.save(AUDIT/f'references-{start//12}.jpg')

if __name__=='__main__': main()

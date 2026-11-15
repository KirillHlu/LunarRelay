"""Read-only GLB geometry audit; outputs reports/previews outside Unity Assets."""
import json, struct, math, hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parent
FILES = ['scifi_dome_station', 'lunar_energy_module_subnautica', 'sputnik_subnautica_style', 'rocket_subnautica_style', 'mars_rover_subnautica', 'lunar_rover_subnautica']
TYPES = {'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
FMT = {5120:'b',5121:'B',5122:'h',5123:'H',5125:'I',5126:'f'}
def mul(a,b): return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
IDENT = [[int(i==j) for j in range(4)] for i in range(4)]
def matrix(n):
    if 'matrix' in n: return [[n['matrix'][j*4+i] for j in range(4)] for i in range(4)]
    x,y,z,w=n.get('rotation',[0,0,0,1]); s=n.get('scale',[1,1,1]); t=n.get('translation',[0,0,0])
    r=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w),t[0]],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w),t[1]],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y),t[2]],[0,0,0,1]]
    for i in range(3):
        for j in range(3): r[i][j]*=s[j]
    return r
def audit(name, source=None, output=None, up='z'):
    destination = Path(output) if output else ROOT
    destination.mkdir(parents=True, exist_ok=True)
    p=Path(source) if source else Path('model_sources')/(name+'.glb'); raw=p.read_bytes()
    magic,version,length=struct.unpack_from('<4sII',raw); assert magic==b'glTF' and version==2 and length==len(raw)
    offset=12; doc=None; binary=b''
    while offset<len(raw):
        size,kind=struct.unpack_from('<II',raw,offset); chunk=raw[offset+8:offset+8+size]; offset+=8+size
        if kind==0x4E4F534A: doc=json.loads(chunk)
        elif kind==0x004E4942: binary=chunk
    def accessor(idx):
        a=doc['accessors'][idx]; assert 'sparse' not in a, 'sparse accessor needs expansion'
        v=doc['bufferViews'][a['bufferView']]; assert v.get('buffer',0)==0
        f='<'+FMT[a['componentType']]*TYPES[a['type']]; size=struct.calcsize(f); stride=v.get('byteStride',size)
        off=v.get('byteOffset',0)+a.get('byteOffset',0); assert off+(a['count']-1)*stride+size<=len(binary)
        return [struct.unpack_from(f,binary,off+i*stride) for i in range(a['count'])]
    triangles=[]; parts=[]; allpos=[]; issues=[]; normal_stats=[]
    def visit(idx,parent):
        n=doc['nodes'][idx]; m=mul(parent,matrix(n))
        if 'mesh' in n:
            mesh=doc['meshes'][n['mesh']]; count=0; partpos=[]
            for pr in mesh['primitives']:
                pos=accessor(pr['attributes']['POSITION']); world=[tuple(sum(m[i][j]*v[j] for j in range(3))+m[i][3] for i in range(3)) for v in pos]; allpos.extend(world); partpos.extend(world)
                assert all(math.isfinite(c) for v in world for c in v), 'non-finite position'
                if 'NORMAL' in pr['attributes']:
                    normals=accessor(pr['attributes']['NORMAL']); assert len(normals)==len(pos)
                    assert all(math.isfinite(c) for v in normals for c in v)
                    lengths=[math.sqrt(sum(c*c for c in v)) for v in normals]
                    normal_stats.append({'node':idx,'min_length':min(lengths),'max_length':max(lengths),'non_unit':sum(abs(k-1)>0.01 for k in lengths)})
                for attr in pr['attributes']:
                    assert len(accessor(pr['attributes'][attr]))==len(pos), 'mismatched attribute count'
                indices=[v[0] for v in accessor(pr['indices'])] if 'indices' in pr else list(range(len(pos)))
                assert all(0<=k<len(pos) for k in indices)
                assert pr.get('mode',4)==4, 'non-triangle primitive'
                assert len(indices)%3==0, 'incomplete triangle'
                material=doc.get('materials',[])[pr['material']] if 'material' in pr else {}
                color=material.get('pbrMetallicRoughness',{}).get('baseColorFactor',[0.7,0.7,0.7,1])
                for i in range(0,len(indices),3): triangles.append({'v':[world[k] for k in indices[i:i+3]],'color':color})
                count+=len(indices)//3
            parts.append({'node':idx,'name':n.get('name',mesh.get('name','unnamed')),'triangles':count,'bounds_min':[min(v[i] for v in partpos) for i in range(3)],'bounds_max':[max(v[i] for v in partpos) for i in range(3)],'world_origin':[m[i][3] for i in range(3)]})
        for child in n.get('children',[]): visit(child,m)
    scene=doc.get('scene',0)
    for idx in doc['scenes'][scene]['nodes']: visit(idx,IDENT)
    lo=[min(p[i] for p in allpos) for i in range(3)]; hi=[max(p[i] for p in allpos) for i in range(3)]
    report={'file':str(p),'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest(),'asset':doc['asset'],'extensionsUsed':doc.get('extensionsUsed',[]),'extensionsRequired':doc.get('extensionsRequired',[]),'bounds_min':lo,'bounds_max':hi,'size_m_gltf_convention':[hi[i]-lo[i] for i in range(3)],'triangles_scene':len(triangles),'vertices_instanced':len(allpos),'mesh_count':len(doc.get('meshes',[])),'node_count':len(doc.get('nodes',[])),'parts':parts,'materials':doc.get('materials',[]),'images':doc.get('images',[]),'animations':len(doc.get('animations',[])),'skins':len(doc.get('skins',[])),'external_buffers':[b.get('uri') for b in doc.get('buffers',[]) if 'uri' in b],'attribute_sets':[list(pr['attributes']) for me in doc.get('meshes',[]) for pr in me['primitives']]}
    report.update({'nodes':doc['nodes'],'normal_stats':normal_stats,'preview_up_axis':up,'issues':issues})
    (destination/(name+'.audit.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
    # Isometric and opposite view, all triangles, flat base-color diagnostic (not PBR).
    views=[]; center=[(hi[i]+lo[i])/2 for i in range(3)]; scale=390/max(hi[i]-lo[i] for i in range(3))
    for sign in [1,-1]:
        projected=[]
        for tri in triangles:
            ps=[]; depths=[]
            for v in tri['v']:
                x,y,z=[v[i]-center[i] for i in range(3)]
                if up=='z': y,z=z,-y
                x*=sign; z*=sign
                ps.extend([250+(x-z)*0.7071*scale,250+(-y*0.8165+(x+z)*0.4082)*scale]); depths.append((x+y+z)*0.5774)
            a,b,c=tri['v']; u=[b[i]-a[i] for i in range(3)]; v=[c[i]-a[i] for i in range(3)]; normal=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]; norm=math.sqrt(sum(k*k for k in normal)); light=0.55+0.45*abs(sum(normal[i]*[0.3,0.85,0.43][i] for i in range(3))/(norm or 1))
            rgb=[round(255*max(0,min(1,k))**(1/2.2)*light) for k in tri['color'][:3]]
            projected.append({'p':ps,'d':sum(depths)/3,'c':rgb})
        projected.sort(key=lambda t:t['d']); views.append(projected)
    with (destination/(name+'.preview.bin')).open('wb') as f:
        f.write(struct.pack('<I',len(views)))
        for view in views:
            f.write(struct.pack('<I',len(view)))
            for t in view: f.write(struct.pack('<6f3B',*t['p'],*t['c']))
    print(name, 'size',report['size_m_gltf_convention'],'triangles',len(triangles),'nodes',len(parts),'materials',len(report['materials']),'images',len(report['images']))
    return report
if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser()
    parser.add_argument('files', nargs='*')
    parser.add_argument('--output',type=Path,default=ROOT)
    parser.add_argument('--up',choices=['y','z'],default='z')
    args=parser.parse_args()
    reports=[audit(Path(p).stem,p,args.output,args.up) for p in args.files] if args.files else [audit(n,output=args.output,up=args.up) for n in FILES]
    (args.output/'audit_summary.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')

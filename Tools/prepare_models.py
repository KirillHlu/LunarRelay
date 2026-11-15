"""Convert supplied GLBs to an offline Unity import format; never modify originals."""
import struct, json, math, sys, hashlib, os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Preflight'))
from audit_glb import matrix
NEW=Path(os.environ.get('LUNAR_RELAY_MODEL_SOURCE',str(ROOT/'model_sources')))
FILES={
 'Habitat':(NEW/'lunar_habitat_conversion_kit_PROVISIONAL_RECREATED.glb'),
 'Sleeping':(NEW/'lunar_sleeping_pod_CONNECTOR_READY.glb'),
 'Terrain':NEW/'lunar_south_polar_ridge_terrain.glb',
 'Power':NEW/'lunar_energy_module_serviced_REPAIRED.glb',
 'Tool':NEW/'lunar_maintenance_torque_driver_REPAIRED.glb',
 'Supplies':NEW/'lunar_shelter_supply_packs_FIXED_nobleed.glb',
 'Solar':NEW/'lunar_solar_array_radiator_CLEAN_FIXED.glb',
 'LifeSupport':NEW/'lunar_life_support_thermal_rack_REPAIRED.glb',
 'Rover':(NEW/'lunar_rover_subnautica.glb')}
FMT={5120:'b',5121:'B',5122:'h',5123:'H',5125:'I',5126:'f'}; NUM={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}
out=ROOT/'Assets/Resources/Models';out.mkdir(parents=True,exist_ok=True)
manifest=[]
for key,path in FILES.items():
 raw=path.read_bytes(); off=12; doc={}; binary=b''
 while off<len(raw):
  size,typ=struct.unpack_from('<II',raw,off); chunk=raw[off+8:off+8+size];off+=8+size
  if typ==0x4e4f534a:doc=json.loads(chunk)
  elif typ==0x004e4942:binary=chunk
 def acc(idx):
  a=doc['accessors'][idx];v=doc['bufferViews'][a['bufferView']];f='<'+FMT[a['componentType']]*NUM[a['type']];stride=v.get('byteStride',struct.calcsize(f));start=v.get('byteOffset',0)+a.get('byteOffset',0)
  return [struct.unpack_from(f,binary,start+i*stride) for i in range(a['count'])]
 with (out/(key+'.lrmodel')).open('wb') as stream:
  def i(v):stream.write(struct.pack('<i',v))
  def floats(v):stream.write(struct.pack('<'+'f'*len(v),*v))
  def blob(b):i(len(b));stream.write(b)
  def s(v):blob(v.encode('utf-8'))
  i(1)
  images=doc.get('images',[]);i(len(images))
  for im in images:
   bv=doc['bufferViews'][im['bufferView']];start=bv.get('byteOffset',0);blob(binary[start:start+bv['byteLength']])
  mats=doc.get('materials',[]);i(len(mats))
  for m in mats:
   p=m.get('pbrMetallicRoughness',{});s(m.get('name','Material'));floats(p.get('baseColorFactor',[1,1,1,1]));floats([p.get('metallicFactor',1),p.get('roughnessFactor',1)]);floats(m.get('emissiveFactor',[0,0,0]));i(int(m.get('doubleSided',False)))
   tex=p.get('baseColorTexture');i(doc['textures'][tex['index']]['source'] if tex else -1)
  meshes=doc['meshes'];i(len(meshes))
  for me in meshes:
   s(me.get('name','Mesh'));i(len(me['primitives']))
   for pr in me['primitives']:
    attrs=pr['attributes'];pos=acc(attrs['POSITION']);i(pr.get('material',0));i(len(pos))
    for v in pos:floats([v[0],v[1],-v[2]])
    normals=acc(attrs['NORMAL']) if 'NORMAL' in attrs else [];i(len(normals))
    for n in normals:floats([n[0],n[1],-n[2]])
    uv=acc(attrs['TEXCOORD_0']) if 'TEXCOORD_0' in attrs else [];i(len(uv))
    for v in uv:floats(v)
    inds=[v[0] for v in acc(pr['indices'])] if 'indices' in pr else list(range(len(pos)));i(len(inds))
    for a in range(0,len(inds),3):
     for k in [inds[a],inds[a+2],inds[a+1]]:i(k)
  nodes=doc['nodes'];parents={c:j for j,n in enumerate(nodes) for c in n.get('children',[])};i(len(nodes))
  for j,n in enumerate(nodes):
   s(n.get('name','Node'));i(parents.get(j,-1));i(n.get('mesh',-1));m=matrix(n)
   # Reflect glTF right-handed Z into Unity left-handed Z.
   for row in range(4):floats([m[row][col]*(-1 if row==2 else 1)*(-1 if col==2 else 1) for col in range(4)])
 manifest.append({'role':key,'source':str(path),'sha256':hashlib.sha256(raw).hexdigest()})
 print(key)
(ROOT/'Preflight/import_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

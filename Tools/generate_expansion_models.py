"""Author original game-ready GLB assets and offline Unity imports, no external packages.
All dimensions are metres. Deterministic geometry; no borrowed game art.
"""
from pathlib import Path
import math,struct,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'GeneratedModels';OUT.mkdir(exist_ok=True)
MAT=[('Ceramic silver',(.78,.82,.81,1),.08,.48),('Graphite',(.035,.065,.085,1),.35,.42),('Brushed aluminium',(.35,.43,.49,1),.75,.3),('Screen',(.025,.10,.14,1),.15,.35),('Cool status',(.14,.65,.78,1),.15,.3),('Service orange',(.77,.33,.08,1),.1,.45),('Leaf green',(.10,.29,.10,1),0,.75),('Leaf fresh',(.19,.43,.12,1),0,.7),('Root substrate',(.11,.075,.045,1),0,1),('Water blue',(.10,.28,.37,1),.15,.35),('LED warm',(.80,.80,.66,1),0,.4)]
def add(a,b):return tuple(x+y for x,y in zip(a,b))
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def mul(a,k):return tuple(x*k for x in a)
def norm(a):return mul(a,1/max(1e-9,math.sqrt(sum(x*x for x in a))))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
class Asset:
 def __init__(self,name):self.name=name;self.parts=[];self.groups=[]
 def group(self,name,parent=-1):self.groups.append((name,parent));return len(self.groups)-1
 def mesh(self,name,faces,material=0,parent=-1):
  vertices=[];normals=[];indices=[]
  for face in faces:
   n=norm(cross(sub(face[1],face[0]),sub(face[2],face[0])));start=len(vertices)
   vertices.extend(face);normals.extend([n]*len(face))
   for i in range(1,len(face)-1):indices.extend((start,start+i,start+i+1))
  self.parts.append((name,vertices,normals,indices,material,parent))
 def box(self,name,c,s,mat=0,parent=-1,bevel=.028):
  half=[v*.5 for v in s];radius=min(bevel,min(half)*.65)
  if radius<=0:radius=min(half)*.08
  core=[v-radius for v in half]
  grid=[[-h,-k-radius*.70710678,-k,k,k+radius*.70710678,h] for h,k in zip(half,core)]
  vs=[];ns=[];ix=[]
  for axis in range(3):
   u=(axis+1)%3;v=(axis+2)%3
   for sign in [-1,1]:
    start=len(vs)
    for y in grid[v]:
     for x in grid[u]:
      q=[0,0,0];q[axis]=sign*half[axis];q[u]=x;q[v]=y
      nearest=[max(-k,min(k,value)) for k,value in zip(core,q)];normal=norm(sub(q,nearest))
      vs.append(add(c,add(nearest,mul(normal,radius))));ns.append(normal)
    for y in range(5):
     for x in range(5):
      a=start+y*6+x;b=a+1;d=a+6;e=d+1
      ix.extend((a,b,e,a,e,d) if sign>0 else (a,e,b,a,d,e))
  self.parts.append((name,vs,ns,ix,mat,parent))
 def rod(self,name,a,b,r,mat=2,parent=-1,n=32):
  axis=norm(sub(b,a));u=norm(cross(axis,(0,0,1) if abs(axis[2])<.9 else (0,1,0)));v=cross(axis,u)
  ring=[add(mul(u,math.cos(i*2*math.pi/n)*r),mul(v,math.sin(i*2*math.pi/n)*r)) for i in range(n)]
  ra=[add(a,p) for p in ring];rb=[add(b,p) for p in ring];faces=[list(reversed(ra)),rb]
  for i in range(n):j=(i+1)%n;faces.append([ra[i],ra[j],rb[j],rb[i]])
  self.mesh(name,faces,mat,parent)
  part=self.parts[-1];smooth=list(part[2]);offset=2*n
  for i in range(n):
   j=(i+1)%n
   for k,index in enumerate([i,j,j,i]):smooth[offset+i*4+k]=norm(ring[index])
  self.parts[-1]=(part[0],part[1],smooth,part[3],part[4],part[5])
 def bolts(self,xs,ys,z,parent=-1):
  for x in xs:
   for y in ys:self.rod('Captive fastener',(x,y,z),(x,y,z+.008),.012,2,parent,6)
 def chassis(self,w,d,h):
  self.box('Equipment plinth',(0,.055,0),(w+.04,.11,d+.04),1)
  self.box('Insulated back',(0,h*.5,-d*.5+.025),(w,h,.05),0)
  for x in [-w*.5+.035,w*.5-.035]:self.box('Structural upright',(x,h*.5,0),(.07,h,d),0)
  self.box('Top cap',(0,h-.035,0),(w,.07,d),0)
  for x in [-w*.36,w*.36]:self.box('Mounting foot',(x,.025,.02),(.14,.05,d+.08),2)
 def display(self,c,w=.24,h=.14,parent=-1):
  self.box('Control bezel',c,(w+.035,h+.035,.025),1,parent)
  self.box('Blank display',add(c,(0,0,.016)),(w,h,.012),3,parent)
  self.box('Status strip',add(c,(0,-h*.7,.025)),(w*.65,.009,.008),4,parent,0)
 def valve(self,c,parent=-1):
  self.rod('Valve stem',add(c,(0,0,-.025)),add(c,(0,0,.028)),.018,2,parent)
  self.rod('Valve knob',add(c,(0,0,.025)),add(c,(0,0,.042)),.047,5,parent,20)
  for i in range(4):a=i*math.pi*.5;self.rod('Knob grip',add(c,(math.cos(a)*.035,math.sin(a)*.035,.045)),add(c,(math.cos(a)*.052,math.sin(a)*.052,.045)),.008,1,parent,8)
 def tank(self,name,x,y,z,r,h,parent=-1,mat=2):
  self.rod(name,(x,y-h*.5,z),(x,y+h*.5,z),r,mat,parent,32)
  for yy in [y-h*.3,y+h*.3]:self.rod('Retaining band',(x,yy-.012,z),(x,yy+.012,z),r+.009,1,parent,32)
  self.rod('Tank neck',(x,y+h*.5,z),(x,y+h*.5+.04,z),r*.28,2,parent)
 def leaf(self,name,base,angle,length,width,parent,mat):
  points=[]
  for i in range(7):
   t=i/6;spread=math.sin(math.pi*t)**.7*width
   center=add(base,(math.cos(angle)*length*t,.15*math.sin(t*math.pi*.85)+.04*t,math.sin(angle)*length*t))
   edge=mul((-math.sin(angle),0,math.cos(angle)),spread)
   points.append((add(center,mul(edge,-1)),add(center,(0,.012*math.sin(t*math.pi),0)),add(center,edge)))
  faces=[]
  for i in range(6):
   for j in range(2):
    f=[points[i][j],points[i+1][j],points[i+1][j+1],points[i][j+1]];faces.extend([f,list(reversed([add(p,(0,-.002,0)) for p in f]))])
  self.mesh(name,faces,mat,parent)
  for i in range(6):self.rod('Leaf vein',points[i][1],points[i+1][1],.0025,7,parent,6)
 def write(self):
  materials=[]
  for i,(name,color,metal,rough) in enumerate(MAT):
   m={'name':name,'pbrMetallicRoughness':{'baseColorFactor':color,'metallicFactor':metal,'roughnessFactor':rough}}
   if i in [4,10]:m['emissiveFactor']=[c*.7 for c in color[:3]]
   materials.append(m)
  blob=bytearray();views=[];access=[];meshes=[]
  def acc(values,typ,fmt,target):
   while len(blob)%4:blob.append(0)
   offset=len(blob)
   for v in values:blob.extend(struct.pack('<'+fmt*(len(v) if isinstance(v,tuple) else 1),*(v if isinstance(v,tuple) else (v,))))
   views.append({'buffer':0,'byteOffset':offset,'byteLength':len(blob)-offset,'target':target})
   a={'bufferView':len(views)-1,'componentType':5126 if fmt=='f' else 5125,'count':len(values),'type':typ}
   if typ=='VEC3':a.update(min=[min(p[k] for p in values) for k in range(3)],max=[max(p[k] for p in values) for k in range(3)])
   access.append(a);return len(access)-1
  nodes=[{'name':self.name,'children':[]}]+[{'name':n,'children':[]} for n,p in self.groups]
  for i,(n,p) in enumerate(self.groups):nodes[p+1 if p>=0 else 0]['children'].append(i+1)
  for name,vs,ns,ix,mat,parent in self.parts:
   pos=acc(vs,'VEC3','f',34962);normal=acc(ns,'VEC3','f',34962);indices=acc(ix,'SCALAR','I',34963)
   meshes.append({'name':name,'primitives':[{'attributes':{'POSITION':pos,'NORMAL':normal},'indices':indices,'material':mat}]})
   nodes.append({'name':name,'mesh':len(meshes)-1});nodes[parent+1 if parent>=0 else 0]['children'].append(len(nodes)-1)
  # Default GLB scene shows a single mature crop. Alternate plant groups are
  # separate scenes, so a standalone viewer never draws three stages on top of each other.
  export_nodes=[dict(n,children=list(n['children'])) if 'children' in n else dict(n) for n in nodes]
  scenes=[{'name':'Equipment ready for use','nodes':[0]}]
  for group_index,(group_name,parent) in enumerate(self.groups):
   if group_name in ['Plants_Seedling','Plants_Wilted']:
    export_nodes[0]['children'].remove(group_index+1)
    scenes.append({'name':group_name+' replacement plants','nodes':[group_index+1]})
  doc={'asset':{'version':'2.0','generator':'Lunar Relay original parametric asset authoring'},'scene':0,'scenes':scenes,'nodes':export_nodes,'meshes':meshes,'materials':materials,'buffers':[{'byteLength':len(blob)}],'bufferViews':views,'accessors':access}
  js=json.dumps(doc,separators=(',',':')).encode();js+=b' '*((-len(js))%4);blob+=b'\0'*((-len(blob))%4)
  glb=struct.pack('<4sII',b'glTF',2,12+8+len(js)+8+len(blob))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(blob),0x004e4942)+blob
  (OUT/(self.name+'.glb')).write_bytes(glb)
  # Same hierarchy and vertices in the project's established offline Unity format.
  with (ROOT/'Assets/Resources/Models'/(self.name+'.lrmodel')).open('wb') as f:
   def i(x):f.write(struct.pack('<i',x))
   def fs(x):f.write(struct.pack('<'+'f'*len(x),*x))
   def st(x):b=x.encode();i(len(b));f.write(b)
   i(1);i(0);i(len(MAT))
   for k,(name,color,metal,rough) in enumerate(MAT):st(name);fs(color);fs([metal,rough]);fs([x*.7 for x in color[:3]] if k in [4,10] else [0,0,0]);i(0);i(-1)
   i(len(self.parts))
   for name,vs,ns,ix,mat,parent in self.parts:
    st(name);i(1);i(mat);i(len(vs))
    for x,y,z in vs:fs((x,y,-z))
    i(len(ns))
    for x,y,z in ns:fs((x,y,-z))
    i(0);i(len(ix))
    for a in range(0,len(ix),3):
     for j in [0,2,1]:i(ix[a+j])
   i(len(nodes));parents={c:j for j,n in enumerate(nodes) for c in n.get('children',[])}
   for j,n in enumerate(nodes):st(n['name']);i(parents.get(j,-1));i(n.get('mesh',-1));fs([int(r==c) for r in range(4) for c in range(4)])
  return {'name':self.name,'parts':len(self.parts),'triangles':sum(len(p[3])//3 for p in self.parts),'sha256':hashlib.sha256(glb).hexdigest()}

def greenhouse():
 a=Asset('Greenhouse');a.chassis(1.12,.52,1.62)
 for y in [.20,.78,1.40]:a.box('Recessed tray shelf',(0,y,0),(1.0,.055,.47),1)
 for y in [.72,1.34]:
  for x in [-.35,0,.35]:a.box('Horticulture LED',(x,y,0),(.27,.025,.40),10)
 for y in [.32,.90]:
  for x in [-.34,0,.34]:
   a.box('Removable root pillow',(x,y-.025,0),(.28,.10,.32),8)
   a.rod('Irrigation emitter',(x-.1,y+.015,-.12),(x-.1,y+.015,-.03),.007,9)
 a.rod('Water manifold',(-.48,.18,-.17),(-.48,1.28,-.17),.014,2)
 for phase,scale in [('Seedling',.36),('Mature',1),('Wilted',.65)]:
  group=a.group('Plants_'+phase)
  for y in [.32,.90]:
   for x in [-.34,0,.34]:
    for j in range(7):a.leaf('Edible leaf',(x,y+.025,0),j*2.399,.14*scale,.052*scale,group,6 if phase=='Wilted' else 7)
 a.display((.20,1.51,.276),.34,.105);a.valve((-.34,1.51,.29));a.bolts([-.48,.48],[.10,1.51],.27)
 return a
def water():
 a=Asset('WaterRecycler');a.chassis(.66,.46,1.23)
 a.tank('Clean water tank',-.16,.43,0,.115,.59,mat=9);a.tank('Used water tank',.16,.43,0,.115,.59,mat=2)
 a.box('Processor housing',(0,.88,-.015),(.54,.25,.30),0)
 for x in [-.19,0,.19]:a.tank('Filter capsule',x,.88,.19,.046,.23,mat=1)
 for x in [-.16,.16]:
  a.rod('Fluid pipe',(x,.72,-.10),(x,1.01,-.10),.013,2);a.valve((x,.29,.14))
 a.display((0,1.10,.246),.28,.12)
 for x in [-.27,.27]:
  for y in [.39,.46,.53]:a.box('Tank level tick',(x,y,.14),(.025,.007,.012),4)
 a.bolts([-.27,.27],[.13,1.15],.24)
 return a
def oxygen():
 a=Asset('OxygenGenerator');a.chassis(.62,.44,1.15)
 a.box('Electrolysis insulated cell stack',(0,.54,0),(.38,.55,.27),0)
 for y in [.34+i*.045 for i in range(10)]:a.box('Stack cooling plate',(0,y,.01),(.42,.014,.30),2)
 a.tank('Oxygen separator',-.22,.57,.01,.055,.49,mat=9);a.tank('Hydrogen separator',.22,.57,.01,.055,.49,mat=2)
 for x in [-.22,.22]:a.rod('Sealed gas plumbing',(x,.84,0),(x,1.04,0),.013,2)
 a.box('Sealed hydrogen outlet',(.24,1.06,-.15),(.09,.08,.10),5)
 a.display((0,.97,.232),.28,.13);a.valve((0,.20,.19));a.bolts([-.25,.25],[.12,1.07],.225)
 return a
def brush():
 a=Asset('DustTool')
 a.box('Contoured insulated grip',(0,.11,0),(.065,.20,.07),1,bevel=.025)
 for y in [.04+i*.024 for i in range(7)]:a.box('Grip tread',(0,y,.038),(.055,.008,.008),2)
 a.rod('Angled neck',(0,.20,0),(0,.29,.05),.024,2)
 a.box('Replaceable brush cartridge',(0,.31,.065),(.20,.055,.10),0)
 for i in range(17):
  for j in range(4):a.rod('Fine antistatic filament',(-.088+i*.011,.337,.027+j*.022),(-.088+i*.011,.377,.027+j*.022),.0023,2,n=6)
 a.box('Orange release tab',(.07,.315,.12),(.035,.022,.018),5)
 a.rod('Tether eye',(-.025,.007,0),(.025,.007,0),.012,5,n=12)
 return a
if __name__=='__main__':
 report=[fn().write() for fn in [greenhouse,water,oxygen,brush]]
 (OUT/'manifest.json').write_text(json.dumps(report,indent=2))
 print(json.dumps(report,indent=2))

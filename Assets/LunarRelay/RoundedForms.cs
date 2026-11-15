using System.Collections.Generic;
using UnityEngine;
namespace LunarRelay {
public static class RoundedForms {
 public static Mesh Box(Vector3 size,float radius){
  var half=size*.5f;radius=Mathf.Min(radius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.65f);var core=half-Vector3.one*radius;
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
  var grids=new float[3][];for(int a=0;a<3;a++)grids[a]=new[]{-half[a],-core[a]-radius*.70710678f,-core[a],core[a],core[a]+radius*.70710678f,half[a]};
  for(int axis=0;axis<3;axis++){int u=(axis+1)%3,v=(axis+2)%3;foreach(int sign in new[]{-1,1}){int start=vertices.Count;
   for(int y=0;y<6;y++)for(int x=0;x<6;x++){var p=Vector3.zero;p[axis]=sign*half[axis];p[u]=grids[u][x];p[v]=grids[v][y];var closest=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));var normal=(p-closest).normalized;vertices.Add(closest+normal*radius);normals.Add(normal);uv.Add(new Vector2((p[u]/size[u])+.5f,(p[v]/size[v])+.5f));}
   for(int y=0;y<5;y++)for(int x=0;x<5;x++){int a=start+y*6+x,b=a+1,d=a+6,e=d+1;triangles.AddRange(sign>0?new[]{a,b,e,a,e,d}:new[]{a,e,b,a,d,e});}
  }}
  var mesh=new Mesh{name="Moulded rounded shell"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
 }
 public static void Soften(GameObject obj,float radius=.14f){var f=obj.GetComponent<MeshFilter>();if(f)f.sharedMesh=Box(Vector3.one,radius);}
}
}

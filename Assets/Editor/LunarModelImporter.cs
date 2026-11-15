using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using UnityEditor;
using UnityEngine.Rendering;

namespace LunarRelay.Editor {
[ScriptedImporter(4,"lrmodel")]
public class LunarModelImporter : ScriptedImporter {
 static string Str(BinaryReader r)=>System.Text.Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
 static Vector3 V3(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 public override void OnImportAsset(AssetImportContext ctx){
  const string shaderPath="Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader";
  ctx.DependsOnArtifact(shaderPath);
  var litShader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
  if(!litShader)throw new Exception("URP Lit shader is not available for model import.");
  using(var r=new BinaryReader(File.OpenRead(ctx.assetPath))){
   if(r.ReadInt32()!=1)throw new Exception("Unsupported model version");
   var textures=new Texture2D[r.ReadInt32()];
   for(int i=0;i<textures.Length;i++){var t=new Texture2D(2,2,TextureFormat.RGBA32,true);t.LoadImage(r.ReadBytes(r.ReadInt32()));t.name="Texture"+i;t.wrapMode=TextureWrapMode.Repeat;ctx.AddObjectToAsset("tex"+i,t);textures[i]=t;}
   var mats=new Material[r.ReadInt32()];
   for(int i=0;i<mats.Length;i++){
    var m=new Material(litShader);m.name=Str(r);m.SetColor("_BaseColor",new Color(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()));m.SetFloat("_Metallic",r.ReadSingle());m.SetFloat("_Smoothness",Mathf.Clamp(1-r.ReadSingle(),.15f,.48f));var e=V3(r);if(e.sqrMagnitude>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(e.x,e.y,e.z));}m.SetFloat("_Cull",r.ReadInt32()!=0?0:2);int ti=r.ReadInt32();if(ti>=0)m.SetTexture("_BaseMap",textures[ti]);ctx.AddObjectToAsset("mat"+i,m);mats[i]=m;
   }
   if(ctx.assetPath.EndsWith("Terrain.lrmodel")){
    var grain=new Texture2D(128,128,TextureFormat.RGBA32,true,true){name="Regolith micrograin",wrapMode=TextureWrapMode.Repeat,anisoLevel=8};
    var pixels=new Color[128*128];var rng=new System.Random(1229);
    for(int k=0;k<pixels.Length;k++){float n=.5f+((float)rng.NextDouble()-.5f)*.24f;pixels[k]=new Color(n,n,n,1);}
    grain.SetPixels(pixels);grain.Apply(true);ctx.AddObjectToAsset("regolith-detail",grain);
    foreach(var m in mats){m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.08f);m.SetTexture("_DetailAlbedoMap",grain);m.SetTextureScale("_DetailAlbedoMap",new Vector2(240,240));m.EnableKeyword("_DETAIL_MULX2");m.SetColor("_BaseColor",new Color(.74f,.77f,.81f,1));}
   }
   int count=r.ReadInt32();var meshes=new List<Mesh[]>();var materials=new List<Material[]>();
   for(int i=0;i<count;i++){
    string name=Str(r);int primitives=r.ReadInt32();var ms=new Mesh[primitives];var mt=new Material[primitives];
    for(int j=0;j<primitives;j++){
     int material=r.ReadInt32(),n=r.ReadInt32();var v=new Vector3[n];for(int k=0;k<n;k++)v[k]=V3(r);
     int nn=r.ReadInt32();var normals=new Vector3[nn];for(int k=0;k<nn;k++)normals[k]=V3(r);
     int un=r.ReadInt32();var uv=new Vector2[un];for(int k=0;k<un;k++)uv[k]=new Vector2(r.ReadSingle(),r.ReadSingle());
     int tn=r.ReadInt32();var tri=new int[tn];for(int k=0;k<tn;k++)tri[k]=r.ReadInt32();
     var mesh=new Mesh{name=name+"_"+j,indexFormat=n>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.vertices=v;mesh.triangles=tri;if(nn==n)mesh.normals=normals;else mesh.RecalculateNormals();if(un==n){mesh.uv=uv;mesh.RecalculateTangents();}mesh.RecalculateBounds();
     bool fixture=name.Contains("Desk")||name.Contains("Cabinet")||name.Contains("Lamp")||name.Contains("LightHousing")||name.Contains("Faceplate");
     if(fixture&&n>=8&&n<=36){var unique=new HashSet<Vector3>(v);var bb=mesh.bounds;bool corners=unique.Count==8;foreach(var point in unique)for(int axis=0;axis<3;axis++)corners &= Mathf.Abs(Mathf.Abs(point[axis]-bb.center[axis])-bb.extents[axis])<.00001f;if(corners&&bb.size.x>.001f&&bb.size.y>.001f&&bb.size.z>.001f){var rounded=RoundedForms.Box(bb.size,.012f);var rv=rounded.vertices;for(int k=0;k<rv.Length;k++)rv[k]+=bb.center;rounded.vertices=rv;rounded.RecalculateBounds();DestroyImmediate(mesh);mesh=rounded;}}
     ctx.AddObjectToAsset("mesh"+i+"_"+j,mesh);ms[j]=mesh;mt[j]=mats[material];
    }meshes.Add(ms);materials.Add(mt);
   }
   int nc=r.ReadInt32();var nodes=new GameObject[nc];var parent=new int[nc];var local=new Matrix4x4[nc];
   for(int i=0;i<nc;i++){
    var go=new GameObject(Str(r));nodes[i]=go;parent[i]=r.ReadInt32();int mi=r.ReadInt32();var m=new Matrix4x4();for(int row=0;row<4;row++)for(int col=0;col<4;col++)m[row,col]=r.ReadSingle();local[i]=m;
    if(mi>=0)for(int j=0;j<meshes[mi].Length;j++){GameObject part=j==0?go:new GameObject("Primitive"+j);if(j>0)part.transform.SetParent(go.transform,false);part.AddComponent<MeshFilter>().sharedMesh=meshes[mi][j];part.AddComponent<MeshRenderer>().sharedMaterial=materials[mi][j];}
   }
   var root=new GameObject(Path.GetFileNameWithoutExtension(ctx.assetPath));
   for(int i=0;i<nc;i++){
    var t=nodes[i].transform;t.SetParent(parent[i]<0?root.transform:nodes[parent[i]].transform,false);var m=local[i];t.localPosition=m.GetColumn(3);t.localRotation=m.rotation;t.localScale=m.lossyScale;
   }ctx.AddObjectToAsset("root",root);ctx.SetMainObject(root);
  }
 }
}
}

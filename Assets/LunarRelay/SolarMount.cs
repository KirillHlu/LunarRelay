using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 int correctedSolarMounts;
 void RebuildSolarMount(Transform solar){
  var tilt=Find(solar,"ArrayTilt");
  var panel=new GameObject("Photovoltaic panel rear-mounted assembly").transform;panel.SetParent(tilt,false);
  // Keep the axle and drive on the mast. Move the complete panel and its rear hardware
  // ahead of that axle, then connect them with two rear stand-off brackets.
  var children=new System.Collections.Generic.List<Transform>();foreach(Transform child in tilt)children.Add(child);
  foreach(var child in children){
   if(child==panel||child.name.StartsWith("PV_Tilt_Axis_Bar")||child.name.StartsWith("PV_Tilt_PivotHub")||child.name.StartsWith("PV_Tilt_OrangeBushing"))continue;
   child.SetParent(panel,false);
  }
  panel.localPosition=new Vector3(0,0,-.38f);
  var axle=Find(solar,"PV_Tilt_Axis_Bar");var material=axle.GetComponent<MeshRenderer>().sharedMaterial;
  foreach(float x in new[]{-.18f,.18f})AirlockPart(tilt,"Rear panel stand-off bracket",new Vector3(x,0,-.14f),new Vector3(.045f,.12f,.28f),material);
  AirlockPart(tilt,"Rear panel mounting bridge",new Vector3(0,0,-.27f),new Vector3(.42f,.18f,.04f),material);
  // Validate the actual transformed mast/drive vertices against the panel plane.
  var face=Find(solar,"SolarFace");bool clear=true;
  foreach(var filter in Find(solar,"Solar_Array_Module").GetComponentsInChildren<MeshFilter>()){
   if(filter.transform.IsChildOf(panel))continue;
   foreach(var vertex in filter.sharedMesh.vertices){var point=face.InverseTransformPoint(filter.transform.TransformPoint(vertex));if(point.z<.025f)clear=false;}
  }
  if(clear)correctedSolarMounts++;else Debug.LogError("Solar mast or drive crosses the photovoltaic face plane.");
 }
}
}

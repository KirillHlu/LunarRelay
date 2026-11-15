using UnityEngine;
using UnityEngine.Rendering;
namespace LunarRelay {
public sealed class NavigationOutlineVisual:MonoBehaviour {}
public sealed partial class GameSession {
 Transform cartridgeSocket; StationAction cartridgeService; Material objectiveMaterial;
 StationAction objective; MeshFilter[] outlinedMeshes; 
 readonly System.Collections.Generic.List<GameObject> outlineVisuals=new System.Collections.Generic.List<GameObject>();
 Transform navigationHabitat,navigationSleeping,navigationPower,outlineRoot,previousOutlineRoot;string navigationLabel="";
 StationAction Available(ActionKind kind)=>actions.Find(a=>a&&!a.Used&&a.Kind==kind);
 bool Carrying(ActionKind kind)=>(held&&held.Kind==kind)||backpack.Exists(a=>a&&a.Kind==kind);
 StationAction Need(ActionKind kind,StationAction destination)=>Carrying(kind)?destination:Available(kind);
 StationAction Objective(){if(WaitingForNextTask)return null;var target=BaseObjective();var routine=Sim.Endless&&!Sim.Storm?RoutineTarget():null;return routine&&(!target||target==shelter)?routine:target;}
 StationAction BaseObjective(){
  if(Sim.Storm)return Sheltered?null:shelter;
  if(Sim.Expanded&&!Sim.ScrubberRepaired&&!Sim.FilterChecked)return Available(ActionKind.Diagnose);
  if(Sim.Expanded&&Sim.BreakerTripped)return Sim.MainPower?Available(ActionKind.Main):Need(ActionKind.Tool,Available(ActionKind.Main));
  if(Sim.Expanded&&(!Sim.FanOn||Sim.SensorFault)){if(!Sim.AirflowChecked||!Sim.SensorChecked)return Available(ActionKind.Diagnose);if(Sim.MainPower&&Sim.AirOn)return Available(ActionKind.Air);return Need(ActionKind.Tool,Available(ActionKind.Diagnose));}
  if(!Sim.ScrubberRepaired){
   if(Sim.MainPower&&Sim.AirOn)return Available(ActionKind.Air);
   return !airCartridgeReady?Need(ActionKind.Cartridge,cartridgeService):Need(ActionKind.Tool,Available(Sim.Expanded?ActionKind.Diagnose:ActionKind.RepairAir));
  }
  if(!Sim.AirOn)return Available(ActionKind.Air);
  if(Sim.RoverOn)return Available(ActionKind.Rover);
  if(!Sim.ArrayRepaired){if(!Carrying(ActionKind.Tool))return Available(ActionKind.Tool);return Available(Sim.MainPower?ActionKind.Main:ActionKind.RepairArray);}
  if(!Sim.MainPower)return Available(ActionKind.Main);
  if(Sim.Expanded&&Sim.Energy<2)return LowPowerTarget();
  if(Sim.Packs<3)return Need(ActionKind.Pack,Available(ActionKind.InstallPack));
  return Sim.CycleHour<5&&!Sheltered?shelter:null;
 }
 Transform DestinationBuilding(StationAction target){
  if(!target)return null;var point=target.transform.position;
  foreach(var building in new[]{navigationHabitat,navigationSleeping,navigationPower})if(building&&target.transform.IsChildOf(building))return building;
  if(new Vector2(point.x+7.65f,point.z).sqrMagnitude<2.45f*2.45f)return navigationSleeping;
  if(new Vector2(point.x,point.z).sqrMagnitude<1.8f*1.8f||(point.x>=-4.13f&&point.x<=4.15f&&Mathf.Abs(point.z)<.95f))return navigationHabitat;
  if(new Vector2(point.x-8,point.z-3.6f).sqrMagnitude<2.2f*2.2f)return navigationPower;
  return null;
 }
 Transform NavigationRoot(StationAction target){
  return target?target.transform:null;
 }
 string NavigationLabel(Transform root){
  if(root==navigationHabitat)return UI("ОСНОВНАЯ БАЗА","MAIN HABITAT");
  if(root==navigationSleeping)return UI("ЖИЛОЙ МОДУЛЬ","LIVING MODULE");
  if(root==navigationPower)return UI("ЭНЕРГОМОДУЛЬ","POWER MODULE");
  return UI("ЦЕЛЬ","GOAL");
 }
 void LateUpdate(){
  SyncToolGlove();
  objective=Playing?Objective():null;outlineRoot=objective?NavigationRoot(objective):null;
  if(outlineRoot==previousOutlineRoot)return;
  foreach(var visual in outlineVisuals)if(visual){visual.SetActive(false);Destroy(visual);}outlineVisuals.Clear();
  previousOutlineRoot=outlineRoot;navigationLabel=objective?L(objective.Label):"";if(!outlineRoot)return;
  if(!objectiveMaterial){var shader=Resources.Load<Shader>("ObjectiveOutline");if(!shader)return;objectiveMaterial=new Material(shader);}
  outlinedMeshes=outlineRoot.GetComponentsInChildren<MeshFilter>();
  foreach(var mesh in outlinedMeshes){
   if(!mesh||!mesh.sharedMesh||!mesh.gameObject.activeInHierarchy||mesh.GetComponent<NavigationOutlineVisual>())continue;
   var source=mesh.GetComponent<MeshRenderer>();if(!source||!source.enabled)continue;
   if(outlineRoot!=objective.transform&&mesh.GetComponentInParent<Rigidbody>())continue;
   var visual=new GameObject("Yellow destination outline");visual.layer=2;visual.AddComponent<NavigationOutlineVisual>();visual.transform.SetParent(mesh.transform,false);
   visual.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;var renderer=visual.AddComponent<MeshRenderer>();
   var materials=new Material[mesh.sharedMesh.subMeshCount];for(int i=0;i<materials.Length;i++)materials[i]=objectiveMaterial;renderer.sharedMaterials=materials;
   renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;outlineVisuals.Add(visual);
  }
 }
 // Service actions consume directly from the backpack; the player's other held item stays put.
 StationAction ServiceItem(ActionKind kind){
  if(held&&held.Kind==kind){var item=held;held=null;return item;}
  var stored=backpack.Find(a=>a&&a.Kind==kind);if(stored)backpack.Remove(stored);return stored;
 }
 void SeatItem(StationAction item,Transform socket,bool replace){
  item.gameObject.SetActive(true);item.Used=true;item.transform.SetParent(socket.parent,true);
  item.transform.rotation=socket.rotation;item.transform.localScale=socket.localScale;
  var bounds=LocalBounds(item.transform);var target=socket.TransformPoint(LocalBounds(socket).center);
  item.transform.position=target-item.transform.TransformVector(bounds.center);
  if(!replace)item.transform.position+=socket.up*(bounds.extents.y+.025f);
  var rb=item.GetComponent<Rigidbody>();if(rb)rb.isKinematic=true;SetHeldColliders(item,false);
  if(replace)foreach(var renderer in socket.GetComponentsInChildren<MeshRenderer>())renderer.enabled=false;
 }
 void ObjectiveMarker(){
  if(!objective||!outlineRoot||!Playing)return;
  var point=outlineRoot.TransformPoint(LocalBounds(outlineRoot).center);
  var vp=Player.View.WorldToViewportPoint(point);float x=vp.x*1440,y=(1-vp.y)*900;
  if(vp.z<0){x=vp.x<.5f?1380:60;y=430;}
  x=Mathf.Clamp(x,24,1416);y=Mathf.Clamp(y,24,876);
  // Bracket marker works at screen edges and through occlusion; the mesh outline obeys depth.
  Box(x-10,y-10,20,2,amber);Box(x-10,y+8,20,2,amber);Box(x-10,y-10,2,20,amber);Box(x+8,y-10,2,20,amber);
  float labelX=Mathf.Clamp(x-150,24,1116),labelY=Mathf.Clamp(y+17,24,796);
  Box(labelX,labelY,300,70,ink);Text(labelX+10,labelY+4,280,42,navigationLabel,micro,amber);Text(labelX+10,labelY+46,280,23,Vector3.Distance(Player.transform.position,point).ToString("0.0")+" m",micro,paper);
 }
}
}

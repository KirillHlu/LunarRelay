using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 Transform rebuiltAirlock;
 Transform AirlockPart(Transform parent,string name,Vector3 center,Vector3 size,Material material){
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;DestroyImmediate(go.GetComponent<Collider>());
  go.transform.SetParent(parent,false);go.transform.localPosition=center;go.transform.localScale=size;go.GetComponent<MeshRenderer>().sharedMaterial=material;if(name=="Lamp housing"||name=="Recessed handle"||name=="Reinforcement rail"){go.GetComponent<MeshFilter>().sharedMesh=RoundedForms.Box(size,.018f);go.transform.localScale=Vector3.one;}return go.transform;
 }
 Material AirlockMaterial(Transform habitat,string source,Color fallback){
  var existing=Find(habitat,source);if(existing&&existing.GetComponent<MeshRenderer>())return existing.GetComponent<MeshRenderer>().sharedMaterial;
  var material=new Material(Resources.Load<GameObject>("Models/Habitat").GetComponentInChildren<MeshRenderer>().sharedMaterial);material.SetColor("_BaseColor",fallback);return material;
 }
 void RebuildAirlock(Transform habitat){
  var shell=AirlockMaterial(habitat,"Airlock_Sidewall_Near",new Color(.6f,.67f,.7f));
  var trim=AirlockMaterial(habitat,"Airlock_East_HatchFrameTop",new Color(.04f,.1f,.14f));
  var door=AirlockMaterial(habitat,"AirlockOuterHatch_Door",new Color(.7f,.75f,.76f));
  var signal=AirlockMaterial(habitat,"Airlock_CeilingLamp_Diffuser_0",Color.cyan);
  // Remove the entire old corridor and its transformed door geometry, not isolated gaps.
  foreach(var t in habitat.GetComponentsInChildren<Transform>()){
   bool old=t.name.StartsWith("Airlock_")||t.name.StartsWith("AirlockInner")||t.name.StartsWith("AirlockOuter");
   bool control=t.name.StartsWith("Airlock_Interlock");
   if(old&&!control)t.gameObject.SetActive(false);
  }
  rebuiltAirlock=new GameObject("Rebuilt sealed airlock module").transform;rebuiltAirlock.SetParent(habitat,false);
  AirlockPart(rebuiltAirlock,"Continuous floor",new Vector3(2.8f,.09f,0),new Vector3(2.7f,.18f,1.96f),shell);
  AirlockPart(rebuiltAirlock,"Continuous roof",new Vector3(2.8f,2.45f,0),new Vector3(2.7f,.24f,1.96f),shell);
  foreach(float z in new[]{-.92f,.92f})AirlockPart(rebuiltAirlock,"Continuous side wall",new Vector3(2.8f,1.2f,z),new Vector3(2.7f,2.5f,.16f),shell);
  foreach(float x in new[]{1.55f,4.05f}){
   // Clear aperture is z +/-0.50 and y .22..2.10. The leaf overlaps all four edges.
   foreach(float z in new[]{-.74f,.74f})AirlockPart(rebuiltAirlock,"Solid door jamb",new Vector3(x,1.16f,z),new Vector3(.24f,2.36f,.48f),shell);
   AirlockPart(rebuiltAirlock,"Solid lintel",new Vector3(x,2.34f,0),new Vector3(.24f,.48f,1.96f),shell);
   AirlockPart(rebuiltAirlock,"Threshold",new Vector3(x,.17f,0),new Vector3(.30f,.10f,1.96f),trim);
   // Trim projects 15 mm into the aperture; no face shares the shell plane.
   foreach(float z in new[]{-.525f,.525f})AirlockPart(rebuiltAirlock,"Recessed seal jamb",new Vector3(x,1.1275f,z),new Vector3(.28f,1.915f,.08f),trim);
   AirlockPart(rebuiltAirlock,"Recessed seal header",new Vector3(x,2.125f,0),new Vector3(.28f,.08f,1.13f),trim);
  }
  foreach(float x in new[]{2.1f,3.5f}){
   AirlockPart(rebuiltAirlock,"Lamp housing",new Vector3(x,2.30f,0),new Vector3(.46f,.08f,.26f),trim);
   AirlockPart(rebuiltAirlock,"Lamp lens",new Vector3(x,2.25f,0),new Vector3(.38f,.025f,.18f),signal);
  }
  inner=RebuiltHatch("Inner pressure hatch",1.59f,ActionKind.DoorInner,-90,true,door,trim);
  outer=RebuiltHatch("Outer pressure hatch",4.04f,ActionKind.DoorOuter,90,false,door,trim);
 }
 StationAction RebuiltHatch(string name,float x,ActionKind kind,float swing,bool open,Material face,Material trim){
  var pivot=new GameObject(name).transform;pivot.SetParent(rebuiltAirlock,false);pivot.localPosition=new Vector3(x,0,.58f);
  AirlockPart(pivot,"Solid overlapping leaf",new Vector3(0,1.19f,-.58f),new Vector3(.14f,2.02f,1.18f),face);
  foreach(float side in new[]{-.085f,.085f}){
   AirlockPart(pivot,"Reinforcement rail",new Vector3(side,1.19f,-.58f),new Vector3(.025f,1.8f,.045f),trim);
   AirlockPart(pivot,"Recessed handle",new Vector3(side*1.6f,1.14f,-.93f),new Vector3(.06f,.23f,.09f),trim);
  }
  var action=Act(pivot,kind,name,true);action.Moving=pivot;action.ClosedEuler=Vector3.zero;action.OpenEuler=new Vector3(0,swing,0);action.Open=open;
  pivot.localRotation=Quaternion.Euler(open?action.OpenEuler:action.ClosedEuler);return action;
 }
}
}

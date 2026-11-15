using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LunarRelay {
public sealed partial class GameSession {
 Light polarSun;readonly List<Bounds> solarEquipmentBounds=new List<Bounds>();
 Transform Model(string role,Vector3 p,Quaternion q){var go=Instantiate(Resources.Load<GameObject>("Models/"+role),p,q);go.name=role;if(role=="Habitat")navigationHabitat=go.transform;else if(role=="Sleeping")navigationSleeping=go.transform;else if(role=="Power")navigationPower=go.transform;return go.transform;}
 Transform Find(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
 Transform FindContains(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name.Contains(name))return t;return null;}
 StationAction Act(Transform t,ActionKind kind,string label,bool box=false){if(!t){Debug.LogError("Missing action: "+label);return null;}var a=t.gameObject.AddComponent<StationAction>();a.Kind=kind;a.Label=label;actions.Add(a);if(box){var bounds=LocalBounds(t);var c=t.gameObject.AddComponent<BoxCollider>();c.center=bounds.center;c.size=Vector3.Max(bounds.size,new Vector3(.06f,.06f,.06f));}return a;}
 Bounds LocalBounds(Transform root){var b=new Bounds();bool first=true;foreach(var f in root.GetComponentsInChildren<MeshFilter>()){var m=f.sharedMesh;if(!m)continue;var mat=root.worldToLocalMatrix*f.transform.localToWorldMatrix;var bb=m.bounds;for(int i=0;i<8;i++){var v=bb.center+Vector3.Scale(bb.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));v=mat.MultiplyPoint3x4(v);if(first){b=new Bounds(v,Vector3.zero);first=false;}else b.Encapsulate(v);}}return b;}
 void Portable(StationAction a){var b=LocalBounds(a.transform);var c=a.gameObject.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;var rb=a.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.mass=a.Kind==ActionKind.Pack?12:1;a.gameObject.AddComponent<LunarBody>();}
 StationAction Door(Transform habitat,string prefix,Vector3 pivot,ActionKind kind,float closed,bool open){var g=new GameObject(prefix+" pivot").transform;g.SetParent(habitat,false);g.localPosition=pivot;var list=habitat.GetComponentsInChildren<Transform>();foreach(var t in list)if(t.name==prefix+"_Door"||t.name.StartsWith(prefix+"_AttachedDetail"))t.SetParent(g,true);var a=Act(g,kind,prefix.Replace("Hatch"," hatch"),true);a.Moving=g;a.OpenEuler=Vector3.zero;a.ClosedEuler=new Vector3(0,closed,0);a.Open=open;g.localRotation=Quaternion.Euler(open?a.OpenEuler:a.ClosedEuler);return a;}
 void LightAt(Vector3 p,float intensity,float range){var g=new GameObject("Cabin practical light");g.transform.position=p;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.72f,.89f,1);l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;l.shadowBias=.025f;l.shadowNormalBias=.12f;l.shadowResolution=LightShadowResolution.Low;cabinLights.Add(l);cabinBrightness.Add(intensity);}
 void Label(string text,Vector3 p){var g=new GameObject("Station label");g.transform.position=p;var tm=g.AddComponent<TextMesh>();g.layer=2;tm.text=L(text);g.AddComponent<LocalizedStationLabel>().English=text;tm.font=Resources.Load<Font>("Fonts/PT_Sans-Web-Regular");tm.fontSize=48;tm.characterSize=.006f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.65f,.78f,.88f);g.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;g.AddComponent<FaceCadet>();}
 void BuildWorld(){
  Physics.gravity=new Vector3(0,-1.62f,0);RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.16f,.21f);RenderSettings.fog=false;
  var sunlight=new GameObject("Low polar Sun").AddComponent<Light>();polarSun=sunlight;sunlight.type=LightType.Directional;sunlight.transform.rotation=Quaternion.Euler(8,-58,0);sunlight.intensity=1.7f;sunlight.color=new Color(1,.96f,.88f);sunlight.shadows=LightShadows.Soft;sunlight.shadowBias=.008f;sunlight.shadowNormalBias=.04f;QualitySettings.shadowDistance=60;
  var terrain=Model("Terrain",Vector3.zero,Quaternion.identity);GradeGround(terrain);CombineStatic(terrain);
  var habitat=Model("Habitat",Vector3.zero,Quaternion.identity);
  RebuildAirlock(habitat);
  shelter=Door(habitat,"StormShelterInnerHatch",new Vector3(-1.59f,.2f,.49f),ActionKind.DoorShelter,-90,true);
  // Replace the supplied placeholder engineering cabinet with the supplied working rack.
  foreach(var t in habitat.GetComponentsInChildren<Transform>())if(t.name.StartsWith("EngineeringCabinet")||t.name.StartsWith("EngineeringBay_"))t.gameObject.SetActive(false);
  var panel=Find(habitat,"Airlock_InterlockPanel_Display");Act(panel,ActionKind.Cycle,"Cycle airlock pressure",true);
  var slots=habitat.GetComponentsInChildren<Transform>();int slot=0;foreach(var t in slots)if(t.name.StartsWith("ShelterSupplySlot_N_0_")&&slot++<3)Act(t,ActionKind.InstallPack,"Secure water pack in shelter",true);
  CombineStatic(habitat);
  var sleeping=Model("Sleeping",new Vector3(-.1f,0,0),Quaternion.Euler(0,180,0));
  // Refit the unused desk bay for the greenhouse; clear the lower shelf at the O2 bay.
  foreach(var part in sleeping.GetComponentsInChildren<Transform>())if(part.name.StartsWith("SleepingPod_SmallDesk")||part.name.StartsWith("SleepingPod_Desk")||part.name=="WallSupplyShelf_0"||part.name=="WallSupplyRetainingLip_0"||part.name.StartsWith("WallSupplyShelf_WallBracket_0")||part.name.StartsWith("WallSupplyShelf_ExtraSupport_0"))part.gameObject.SetActive(false);
  CombineStatic(sleeping);
  var rack=Model("LifeSupport",new Vector3(0,.22f,-1.18f),Quaternion.Euler(0,180,0));
  var air=Act(Find(rack,"ELECTRICAL_ISOLATOR_MOVABLE_PIVOT"),ActionKind.Air,"Toggle air-cleaner power",true);air.Moving=air.transform;
  var oxy=Act(Find(rack,"OXYGEN_VALVE_ROTATABLE_PIVOT"),ActionKind.Oxygen,"Turn stored-oxygen regulator",true);oxy.Moving=oxy.transform;
  Act(Find(rack,"STATUS_BLANK_SCREEN"),ActionKind.RepairAir,"Service CO2 cleaner: cartridge, then driver",true);
  var cartridge=Act(Find(rack,"CO2_SEPARATE_MATCHING_SPARE"),ActionKind.Cartridge,"Spare CO2 cartridge");Portable(cartridge);
  cartridgeSocket=Find(rack,"CO2_REMOVABLE_CARTRIDGE_A");
  cartridgeService=Act(cartridgeSocket,ActionKind.RepairAir,"Service CO2 cleaner: cartridge, then driver",true);
  RebuildRackCap(rack);CombineStatic(rack);
  var tool=Model("Tool",new Vector3(-6.4f,.36f,.65f),Quaternion.identity);Portable(Act(tool,ActionKind.Tool,"Torque driver"));CombineStatic(tool);
  for(int i=0;i<3;i++){
   var supplies=Model("Supplies",Vector3.zero,Quaternion.identity);var water=Find(supplies,"WaterPack");water.SetParent(null,true);Destroy(supplies.gameObject);water.position=new Vector3(-7.2f+i*.5f,.32f,1.2f);Portable(Act(water,ActionKind.Pack,"Sealed water pack"));
  }
  var power=Model("Power",new Vector3(8,.12f,3.6f),Quaternion.Euler(0,180,0));
  var main=Act(Find(power,"MainPowerIsolator_Pivot"),ActionKind.Main,"Toggle main power bus",true);main.Moving=main.transform;
  var thermal=Act(Find(power,"Switch_ThermalControl_HingePivot"),ActionKind.Thermal,"Toggle thermal control",true);thermal.Moving=thermal.transform;
  var roverSwitch=Act(Find(power,"Switch_RoverCharging_HingePivot"),ActionKind.Rover,"Toggle rover charging",true);roverSwitch.Moving=roverSwitch.transform;
  var powerAir=Act(Find(power,"Switch_LifeSupport_HingePivot"),ActionKind.Air,"Toggle air-cleaner power",true);powerAir.Moving=powerAir.transform;
  Act(Find(power,"RepairPort_SeparateHingedCap_Pivot"),ActionKind.RepairArray,"Repair array connection with torque driver",true);
  Act(Find(power,"BatteryCassette_Installed_Movable"),ActionKind.ReplaceBattery,"Fit spare battery to isolated bus",true);
  var spare=Find(power,"BatteryCassette_SeparateMatchingSpare_Movable");Portable(Act(spare,ActionKind.Battery,"Spare battery: 2 kWh"));
  // A habitat control surface uses the supplied switch geometry; main isolation remains external.
  Act(Find(habitat,"AirlockGauge_Face"),ActionKind.Info,"Airlock pressure appears on your suit display",true);
  ConfigurePanelLevers(power);CombineStatic(power);
  // Each imported asset contains BOTH a solar array and a radiator, spanning 4.90 m.
  // Place four complete pairs in two rows instead of overlapping them every 3.30 m.
  for(int i=0;i<4;i++){
   var solar=Model("Solar",new Vector3(6+(i%2)*5.8f,0,-5-(i/2)*3f),Quaternion.identity);
   RebuildSolarMount(solar);
   AddPanelService(solar,i);
   foreach(var name in new[]{"Solar_Array_Module","Radiator_Module"}){
    var module=Find(solar,name);var bounds=new Bounds();bool first=true;
    foreach(var renderer in module.GetComponentsInChildren<MeshRenderer>()){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
    solarEquipmentBounds.Add(bounds);
   }
   CombineStatic(solar);
  }
  var rover=Model("Rover",new Vector3(13,.05f,5),Quaternion.Euler(90,28,0));var roverBounds=LocalBounds(rover);float minY=float.PositiveInfinity;foreach(var filter in rover.GetComponentsInChildren<MeshFilter>())foreach(var vertex in filter.sharedMesh.vertices)minY=Mathf.Min(minY,filter.transform.TransformPoint(vertex).y);rover.position+=Vector3.up*(-.04f-minY);CombineStatic(rover);
  LightAt(new Vector3(0,2.35f,0),1.35f,4);LightAt(new Vector3(2.8f,2.15f,0),1.1f,3.2f);LightAt(new Vector3(-2.8f,2.1f,0),1.15f,3.2f);LightAt(new Vector3(-7.5f,2.4f,0),1.65f,5.2f);
  BuildEngineeringWorld();BuildHandControls();
  Label("01  AIR CONTROL",new Vector3(0,2.28f,-.8f));Label("02  SHELTER",new Vector3(-2.7f,1.9f,0));Label("TOOLS + WATER",new Vector3(-6.6f,1.7f,.8f));Label("03  POWER SERVICE",new Vector3(8,3.1f,1));Label("AIRLOCK\nE: pressure cycle",new Vector3(3.25f,1.75f,-.7f));
 }
 void GradeGround(Transform root){foreach(var f in root.GetComponentsInChildren<MeshFilter>()){var m=Instantiate(f.sharedMesh);var v=m.vertices;for(int i=0;i<v.Length;i++){var p=f.transform.TransformPoint(v[i]);float d=Mathf.Max(Mathf.Abs(p.x-2)-14,Mathf.Abs(p.z)-9);float blend=1-Mathf.Clamp01(d/4);p.y=Mathf.Lerp(p.y,-.05f,blend);v[i]=f.transform.InverseTransformPoint(p);}m.vertices=v;m.RecalculateNormals();m.RecalculateBounds();f.sharedMesh=m;}}
 void CombineStatic(Transform root){
  var byMaterial=new Dictionary<Material,List<CombineInstance>>();
  foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
   if(!f.gameObject.activeInHierarchy||!f.sharedMesh||f.GetComponentInParent<AnimatedAssembly>())continue;var renderer=f.GetComponent<MeshRenderer>();if(!renderer)continue;
   var a=f.GetComponentInParent<StationAction>();
   if(a){if(!a.GetComponent<Rigidbody>()&&!a.GetComponent<Collider>()&&!f.GetComponent<Collider>())f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;continue;}
   var mat=renderer.sharedMaterial;if(!byMaterial.ContainsKey(mat))byMaterial[mat]=new List<CombineInstance>();byMaterial[mat].Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});renderer.enabled=false;
  }
  foreach(var pair in byMaterial){var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name=root.name+" combined"};mesh.CombineMeshes(pair.Value.ToArray(),true,true);var go=new GameObject("Static "+pair.Key.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;go.AddComponent<MeshCollider>().sharedMesh=mesh;}
 }
}
public sealed class FaceCadet:MonoBehaviour {void LateUpdate(){var camera=Camera.main;if(!camera)camera=UnityEngine.Object.FindFirstObjectByType<Camera>();if(camera){transform.rotation=Quaternion.LookRotation(transform.position-camera.transform.position);var t=GetComponent<TextMesh>();float d=Vector3.Distance(transform.position,camera.transform.position);t.color=new Color(.65f,.78f,.88f,Mathf.Clamp01((12-d)/4));}}}
}

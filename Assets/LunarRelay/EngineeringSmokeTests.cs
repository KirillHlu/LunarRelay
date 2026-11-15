using System.Collections;
using System.IO;
using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 IEnumerator EngineeringRuntimeChecks(){
  var profile=Resources.Load<UnityEngine.Rendering.VolumeProfile>("OutpostLook");
  Verify(profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom)&&bloom.intensity.value==0,"bloom cannot wash out equipment visibility");
  foreach(var action in actions)if(action.Kind==ActionKind.Greenhouse||action.Kind==ActionKind.WaterRecycler||action.Kind==ActionKind.OxygenGenerator){
   var material=action.GetComponent<MeshRenderer>().sharedMaterial;
   Verify(material.GetFloat("_Metallic")==0&&material.GetFloat("_SpecularHighlights")==0&&material.GetFloat("_EnvironmentReflections")==0&&!material.IsKeywordEnabled("_EMISSION"),"matte non-emissive screen: "+action.Kind);
  }
  foreach(var lever in FindObjectsByType<LeverMotion>(FindObjectsSortMode.None)){
   lever.Set(false,true);float upper=lever.Grip.position.y;lever.Set(true,true);Verify(lever.Grip.position.y<upper-.03f,"lever enabled position is physically below disabled position: "+lever.Kind);Verify(Quaternion.Angle(lever.transform.localRotation,Quaternion.identity)<.01f,"lever faceplate stays fixed: "+lever.Kind);lever.Set(lever.EnabledState,true);
  }
  var roverControl=ActionFor(ActionKind.Rover);var motion=roverControl.GetComponent<LeverMotion>();bool lampWasOn=headLamp.enabled;headLamp.enabled=true;bool roverWasOn=Sim.RoverOn;Sim.RoverOn=false;motion.Set(false,true);yield return Photograph("12-levers-off",new Vector3(7.9f,.9f,6.25f),roverControl.transform.position);Sim.RoverOn=true;motion.Set(true,true);yield return Photograph("13-levers-on",new Vector3(7.9f,.9f,6.25f),roverControl.transform.position);Sim.RoverOn=roverWasOn;motion.Set(motion.EnabledState,true);headLamp.enabled=lampWasOn;
  Verify(removedUpperRackBars==4,"four coplanar upper rack bars removed");Verify(rackCap.GetComponent<MeshFilter>().sharedMesh.bounds.size.x>.90f&&rackCap.GetComponent<MeshFilter>().sharedMesh.bounds.size.z>.69f,"rack cap overhang separates exterior side planes");
  bool rackLampBefore=headLamp.enabled;headLamp.enabled=true;
  yield return Photograph("14-rack-cap-left",new Vector3(-.65f,.27f,-.15f),new Vector3(0,2.05f,-1.18f));
  yield return Photograph("15-rack-cap-right",new Vector3(.65f,.27f,-.15f),new Vector3(0,2.05f,-1.18f));headLamp.enabled=rackLampBefore;
  var legacy=Sim;Sim=new Simulation(2);Sim.EnableExpansion(2);started=true;paused=true;
  Sim.SuitEquipped=true;Sim.HintLevel=0;Player.Place(new Vector3(10,.27f,8));
  Verify(Objective()!=null&&NavigationRoot(Objective())==Objective().transform,"exact control guidance works without optional hints");
  paused=false;
  yield return Photograph("10-guidance-main-base",new Vector3(10,.27f,8),new Vector3(0,1.4f,0));
  Verify(Playing&&outlineVisuals.Count>0&&objectiveMaterial.shader.isSupported,"destination outline has active renderers during gameplay");
  paused=true;Player.Place(new Vector3(0,.27f,.4f));Verify(NavigationRoot(Objective())==Objective().transform,"guidance switches to equipment inside destination building");
  Player.Place(new Vector3(2.9f,.27f,0));float originalPressure=lockPressure;lockPressure=0;Verify(NavigationRoot(Objective())==Objective().transform,"vacuum airlock does not outline the building around the player");lockPressure=originalPressure;
  Sim.ScrubberRepaired=true;Player.Place(new Vector3(10,.27f,9));Verify(NavigationRoot(Objective())==Objective().transform,"power task outlines switch itself from outside");
  paused=false;yield return Photograph("11-guidance-power",new Vector3(10,.27f,9),new Vector3(8,1.4f,3.6f));paused=true;
  Player.Place(new Vector3(8,.27f,5.5f));Verify(NavigationRoot(Objective())==Objective().transform,"near power module guidance switches to control");
  var livingControl=ActionFor(ActionKind.Greenhouse);Player.Place(new Vector3(0,.27f,.4f));Verify(NavigationRoot(livingControl)==livingControl.transform,"remote greenhouse target outlines actual display");
  Sim.Hours=5.5;Player.Place(new Vector3(10,.27f,8));Verify(Objective()==shelter&&NavigationRoot(Objective())==Objective().transform,"storm guidance targets shelter hatch");
  Sim=new Simulation(2);Sim.EnableExpansion(2);Player.Place(new Vector3(0,.27f,.45f));
  Verify(farm&&cropVisuals.Length==3&&Resources.Load<GameObject>("Models/WaterRecycler")&&Resources.Load<GameObject>("Models/OxygenGenerator"),"four generated engineering assets imported");
  Verify(panelService.Count==4&&ActionFor(ActionKind.Brush),"all four panels have service points and a real cleaning tool");
  Sim.Delivered[5]=0;EngineeringPresentation();Verify(farmLampMaterial.GetColor("_EmissionColor").maxColorComponent==0,"greenhouse lamps stop emitting without delivered power");
  Sim.Delivered[5]=.8;EngineeringPresentation();Verify(farmLampMaterial.GetColor("_EmissionColor").maxColorComponent>.5,"greenhouse lamps respond to delivered power");
  Sim.CropDays=28;EngineeringPresentation();Verify(cropVisuals[1].gameObject.activeSelf&&!cropVisuals[0].gameObject.activeSelf,"mature crop switches actual 3D plant group");
  Sim.CropHealth=.2;EngineeringPresentation();Verify(cropVisuals[2].gameObject.activeSelf,"poor crop health switches wilted 3D plant group");Sim.CropHealth=1;Sim.CropDays=18;EngineeringPresentation();
  var guidanceState=Sim;Sim=new Simulation(0);Sim.EnableExpansion(0);Sim.ScrubberRepaired=Sim.ArrayRepaired=Sim.FanOn=true;Sim.Energy=1;Sim.RoverOn=false;Sim.SolarKW=0;Sim.Hours=3;
  Verify(Objective()==LoadControl(5)&&!NextTask().Contains("E"),"low power names and highlights greenhouse without input instructions");Sim.GreenhouseLevel=0;Sim.LightingLevel=1;Verify(Objective()==LoadControl(3),"low power switches to cabin lighting");Sim.LightingLevel=.5;Verify(Objective()==null&&NextTask().Contains("1.0"),"shadow wait has a countdown and no false target");Sim.Hours=4.1;Sim.SolarKW=3.6;Sim.LoadKW=2;Verify(Objective()==null,"charging does not request unnecessary switching");Sim.Hours=5.5;Player.Place(new Vector3(-2.7f,.27f,0));Verify(Objective()==null&&!NextTask().Contains("Cabin lights"),"sheltered storm keeps hold-position priority");Sim=guidanceState;
  var idlePrevious=Sim;Sim=new Simulation(0);Sim.EnableExpansion(0);Sim.Endless=true;Sim.ScrubberRepaired=Sim.ArrayRepaired=true;Sim.RoverOn=false;Sim.Packs=3;Sim.Energy=5;Sim.Hours=4.2;Player.Place(new Vector3(-2.7f,.27f,0));paused=false;
  UpdateIdleTasks(0);Verify(WaitingForNextTask&&IdleTaskMessage().Contains("10"),"endless starts ten-second gap between assignments");UpdateIdleTasks(9.9f);Verify(!Sim.RoutineAssigned&&!Sim.SensorFault,"no assignment or sensor fault before ten seconds");paused=true;UpdateIdleTasks(20);Verify(!Sim.RoutineAssigned,"assignment countdown freezes on pause");paused=false;UpdateIdleTasks(.11f);Verify(Sim.RoutineAssigned&&Objective().Kind==ActionKind.CleanPanel&&!Sim.SensorFault,"after ten seconds existing panel-cleaning job is assigned");
  var cleaning=Objective();Use(cleaning);Verify(Sim.RoutineAssigned&&!Sim.TaskWaitActive,"unsuccessful interaction does not complete assigned job");var cleanTool=ActionFor(ActionKind.Brush);Pickup(cleanTool);StoreHeld();Sim.SuitEquipped=true;Player.Place(new Vector3(6,.27f,-6));Sim.PanelDust[panelService.IndexOf(cleaning)]=.4;Use(cleaning);Verify(Sim.TaskWaitActive&&!Sim.RoutineAssigned&&Objective()==null,"completing routine job starts next ten-second gap");
  var savedWait=JsonUtility.FromJson<Simulation>(JsonUtility.ToJson(Sim));Verify(savedWait.TaskWaitActive&&!savedWait.SensorFault,"saved interval does not manufacture sensor faults");backpack.Remove(cleanTool);cleanTool.gameObject.SetActive(true);cleanTool.transform.SetParent(null,true);cleanTool.transform.position=new Vector3(-6.8f,.285f,-1.5f);SetHeldColliders(cleanTool,true);held=null;
  paused=true;Sim=idlePrevious;
  var brush=ActionFor(ActionKind.Brush);Pickup(brush);StoreHeld();Sim.SuitEquipped=true;
  Player.Place(new Vector3(6,.27f,-6));double before=Sim.PanelDust[0];Use(panelService[0]);Verify(Sim.PanelDust[0]<before&&backpack.Contains(brush),"E cleans a panel using the tool from backpack");
  var suit=ActionFor(ActionKind.SuitCheck);Sim.SuitEquipped=false;Use(suit);Verify(Sim.SuitEquipped,"physical suit station equips EVA reserves");
  Player.Place(new Vector3(0,.27f,.45f));Use(ActionFor(ActionKind.Diagnose));Verify(!showStatus&&Sim.FilterChecked&&Sim.SensorChecked&&Sim.AirflowChecked,"E diagnoses equipment without opening PDA");
  var tool=ActionFor(ActionKind.Tool);Pickup(tool);StoreHeld();Sim.AirOn=false;Sim.FanOn=false;Sim.SensorFault=true;Use(ActionFor(ActionKind.Diagnose));Verify(Sim.FanOn&&!Sim.SensorFault,"isolated physical service repairs fan and sensor");
  Sim.MainPower=false;Sim.BreakerTripped=true;Use(ActionFor(ActionKind.Main));Verify(!Sim.BreakerTripped&&!Sim.MainPower,"physical main control resets isolated breaker");Sim.MainPower=true;Sim.AirOn=true;
  Sim.CropDays=28;Sim.CropHealth=1;int harvestBefore=Sim.Harvests;Use(ActionFor(ActionKind.Greenhouse));Verify(Sim.Harvests==harvestBefore+1&&!showStatus,"E harvests without a menu");
  foreach(var hand in actions){var control=hand.GetComponent<HandControl>();if(!control)continue;double beforeSetting=control.Setting<8?LoadSetting(control.Setting):control.Setting==8?(Sim.PriorityPower?1:0):Sim.MealLevel;Use(hand);double afterSetting=control.Setting<8?LoadSetting(control.Setting):control.Setting==8?(Sim.PriorityPower?1:0):Sim.MealLevel;Verify(beforeSetting!=afterSetting&&!showStatus,"physical switch changes setting: "+hand.Label);}
  double waterSetting=Sim.WaterLevel;Use(ActionFor(ActionKind.WaterRecycler));Verify(Sim.WaterLevel!=waterSetting&&!showStatus,"physical recycler changes its setting");
  double oxygenSetting=Sim.OxygenLevel;Use(ActionFor(ActionKind.OxygenGenerator));Verify(Sim.OxygenLevel!=oxygenSetting&&!showStatus,"physical oxygen generator changes its setting");
  RunDiagnostic(2);Verify(Sim.SensorChecked&&diagnosticResult.Contains("kPa"),"reference measurement records actual sensor values");
  Sim.Endless=true;saveOverride=Path.Combine(SmokePath,"engineering-save-test.json");double savedEnergy=Sim.Energy;double savedWater=Sim.CleanWater;SaveMission();
  Sim.Energy=.1;Sim.CleanWater=.2;backpack.Clear();Verify(LoadMission(),"mission save loads successfully");
  Verify(Sim.Endless,"save restores endless mode");
  Verify(System.Math.Abs(Sim.Energy-savedEnergy)<1e-8&&System.Math.Abs(Sim.CleanWater-savedWater)<1e-8&&Sim.SensorChecked&&backpack.Contains(brush)&&paused,"load restores resources, diagnostics, backpack and pause state");
  saveOverride=null;backpack.Clear();brush.gameObject.SetActive(true);brush.transform.SetParent(null,true);brush.transform.position=new Vector3(-6.8f,.285f,-1.5f);SetHeldColliders(brush,true);held=null;
  Sim.LightingLevel=1;EngineeringPresentation();headLamp.enabled=false;
  yield return Photograph("06a-room-lamp-off",new Vector3(-8.0f,.27f,0),new Vector3(-5.3f,1.0f,0));
  headLamp.enabled=true;
  yield return Photograph("06b-room-lamp-on",new Vector3(-8.0f,.27f,0),new Vector3(-5.3f,1.0f,0));
  headLamp.enabled=false;
  yield return Photograph("07-greenhouse",new Vector3(-7.5f,.27f,.35f),new Vector3(-7.5f,1.1f,1.55f));
  yield return Photograph("08-water-recycler",new Vector3(-6.65f,.27f,.10f),new Vector3(-6.25f,.9f,1.25f));
  yield return Photograph("09-oxygen-generator",new Vector3(-6.65f,.27f,-.10f),new Vector3(-6.25f,.9f,-1.25f));
  yield return DecorChecks();
  showStatus=false;paused=true;Sim=legacy;
 }
}
}

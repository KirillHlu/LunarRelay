using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace LunarRelay {
public sealed partial class GameSession {
 readonly List<Light> cabinLights=new List<Light>();
 readonly List<float> cabinBrightness=new List<float>();
 readonly Dictionary<Renderer,MaterialPropertyBlock> statusDisplays=new Dictionary<Renderer,MaterialPropertyBlock>();
 float nextPresentationUpdate; bool reducedEffects;
 void FinishPresentation(){
  RenderSettings.ambientMode=AmbientMode.Trilight;
  RenderSettings.ambientSkyColor=new Color(.11f,.13f,.16f);
  RenderSettings.ambientEquatorColor=new Color(.075f,.09f,.11f);
  RenderSettings.ambientGroundColor=new Color(.045f,.052f,.065f);
  RenderSettings.reflectionIntensity=.55f;
  var bounce=new SphericalHarmonicsL2();bounce.AddAmbientLight(new Color(.23f,.25f,.27f));RenderSettings.ambientProbe=bounce;
  FillLight(new Vector3(.65f,1.25f,-.35f),1.05f,3.1f);FillLight(new Vector3(-7.2f,1.6f,.25f),.9f,3.6f);
  Player.View.clearFlags=CameraClearFlags.SolidColor;
  Player.View.backgroundColor=new Color(.002f,.003f,.005f);
  Player.View.allowHDR=true;
  var cameraData=Player.View.GetUniversalAdditionalCameraData();
  cameraData.renderPostProcessing=true;
  cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
  cameraData.antialiasingQuality=AntialiasingQuality.High;
  var volume=new GameObject("Outpost color and optics").AddComponent<Volume>();
  volume.isGlobal=true;volume.priority=10;volume.sharedProfile=Resources.Load<VolumeProfile>("OutpostLook");
  headLamp.enabled=false;headLamp.intensity=1.6f;headLamp.shadows=LightShadows.Soft;
  headLamp.shadowBias=.003f;headLamp.shadowNormalBias=.03f;headLamp.GetUniversalAdditionalLightData().usePipelineSettings=false;headLamp.GetUniversalAdditionalLightData().softShadowQuality=SoftShadowQuality.High;
  foreach(var action in actions){
   if(action.Kind==ActionKind.RepairAir||action.Kind==ActionKind.Cycle){
    var renderer=action.GetComponent<Renderer>();
    if(renderer){var material=new Material(renderer.sharedMaterial);material.EnableKeyword("_EMISSION");renderer.sharedMaterial=material;statusDisplays[renderer]=new MaterialPropertyBlock();}
   }
  }
  StartCoroutine(CaptureReflections());
  // A dedicated establishing view makes the menu part of the actual outpost.
  Player.View.transform.position=new Vector3(17,4.6f,-14);
  Player.View.transform.rotation=Quaternion.LookRotation(new Vector3(1,1.1f,0)-Player.View.transform.position);
 }
 IEnumerator CaptureReflections(){
  yield return null;
  foreach(var p in new[]{new Vector3(0,1.5f,0),new Vector3(-7.5f,1.5f,0)}){
   var probe=new GameObject("Cabin reflection capture").AddComponent<ReflectionProbe>();
   probe.transform.position=p;probe.size=new Vector3(5,4,5);probe.boxProjection=true;
   probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
   probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;
   probe.cullingMask=~(1<<2);probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=Color.black;
   probe.nearClipPlane=.1f;probe.farClipPlane=25;probe.intensity=.6f;probe.RenderProbe();
   yield return null;
  }
 }
 void FillLight(Vector3 position,float intensity,float range){var light=new GameObject("Reflected cabin task light").AddComponent<Light>();light.transform.position=position;light.type=LightType.Point;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;light.color=new Color(.88f,.94f,1);cabinLights.Add(light);cabinBrightness.Add(intensity);}
 void UpdatePresentation(){
  if(Player==null)return;
  foreach(var light in cabinLights){int index=cabinLights.IndexOf(light);float fraction=Sim.MainPower?(float)Sim.ServedFraction:.14f;
   light.intensity=Mathf.Lerp(light.intensity,cabinBrightness[index]*1.25f*Mathf.Max(.24f,fraction),Time.unscaledDeltaTime*3);
   light.color=Color.Lerp(light.color,Sim.MainPower?new Color(.88f,.94f,1):new Color(1,.26f,.07f),Time.unscaledDeltaTime*2);
  }
  if(Time.unscaledTime<nextPresentationUpdate)return;nextPresentationUpdate=Time.unscaledTime+.2f;
  foreach(var entry in statusDisplays){if(!entry.Key)continue;var action=entry.Key.GetComponent<StationAction>();
   bool ready=action.Kind==ActionKind.RepairAir?Sim.ScrubberRepaired&&Sim.AirOn&&Sim.MainPower:cycleRemaining<=0;
   var color=ready?new Color(.12f,.8f,.64f):new Color(1,.32f,.06f);
   entry.Value.SetColor("_BaseColor",color*.16f);entry.Value.SetColor("_EmissionColor",color*1.4f);entry.Key.SetPropertyBlock(entry.Value);
  }
 }
 void RestoreFirstPerson(){Player.Place(new Vector3(0,.26f,.25f),180);Player.View.transform.localPosition=new Vector3(0,1.48f,0);Player.View.transform.localRotation=Quaternion.identity;}
 string NextTask(){
   if(Sim.Expanded){
    if(WaitingForNextTask)return IdleTaskMessage();
    if(Sim.Storm&&Sheltered)return UI("Оставайтесь в убежище до конца тревоги. Не выходите наружу.","Stay sheltered until the drill ends. Do not go outside.");
    if(!Sim.Storm&&Sim.Energy<2&&Sim.ScrubberRepaired&&Sim.AirOn&&Sim.FanOn&&!Sim.SensorFault&&!Sim.BreakerTripped&&Sim.ArrayRepaired&&Sim.MainPower)return LowPowerInstruction();
    var target=Objective();if(target)return TaskMeaning(target);
    return WaitingForNextTask?IdleTaskMessage():UI("Следите за состоянием базы.","Monitor the base reserves.");
   }
  if(Sim.Storm)return Sheltered?(Sim.Endless?UI("Оставайтесь в убежище до конца тревоги.","Stay sheltered until this drill ends."):L("Hold position in the shelter until hour 7.")):L("Return to the stocked storm shelter.");
  if(!Sim.ScrubberRepaired){if(!airCartridgeReady)return Sim.AirOn?L("Isolate the air cleaner before service."):L("Fit the spare CO2 cartridge at the rack screen.");return L("Secure the cover with the torque driver.");}
  if(!Sim.AirOn)return L("Restore air-cleaner power.");
  if(Sim.RoverOn)return L("Switch off rover charging at the power module.");
  if(!Sim.ArrayRepaired)return Sim.MainPower?L("Bring the driver outside; isolate main power."):L("Repair the connection, then restore main power.");
  if(!Sim.MainPower)return L("Restore the main power bus.");
  if(Sim.Packs<3)return L("Carry water to the shelter: ")+Sim.Packs+L(" of 3 secured.");
  if(Sim.CycleHour<5)return L("Base prepared. Be in the shelter before hour 5.");
  return Sim.Endless?UI("Следите за ресурсами. Следующий цикл начнётся автоматически.","Monitor reserves. The next cycle starts automatically."):L("Monitor essential loads through the end of the shift.");
 }
 string FocusState(){if(!focus)return "";var hands=HandsPrompt(focus);if(hands!=null)return hands;switch(focus.Kind){
  case ActionKind.Tool:case ActionKind.Pack:case ActionKind.Cartridge:case ActionKind.Battery:return useRussian?"E — ПОДОБРАТЬ / R — УБРАТЬ В РЮКЗАК":"E — PICK UP / R — STORE IN BACKPACK";
  case ActionKind.Air:return Sim.AirOn?L("ONLINE  /  E to isolate"):L("ISOLATED  /  E to restore");
  case ActionKind.Main:return Sim.MainPower?L("BUS ONLINE  /  E to isolate"):L("BUS ISOLATED  /  E to restore");
  case ActionKind.Rover:return Sim.RoverOn?L("CHARGING  /  1.8 kW"):L("CHARGING PAUSED");
  case ActionKind.Thermal:return Sim.ThermalOn?L("THERMAL CONTROL ON"):L("THERMAL CONTROL OFF");
  case ActionKind.RepairAir:return Sim.ScrubberRepaired?L("SERVICE COMPLETE"):airCartridgeReady?L("CARTRIDGE FITTED / DRIVER REQUIRED"):(Carrying(ActionKind.Cartridge)?(useRussian?"E — УСТАНОВИТЬ КАРТРИДЖ":"E — INSTALL CARTRIDGE"):L("REPLACEMENT CARTRIDGE REQUIRED"));
  case ActionKind.RepairArray:return Sim.ArrayRepaired?L("CONNECTION SECURED"):L("ISOLATE MAIN BUS BEFORE SERVICE");
  case ActionKind.Cycle:return cycleRemaining>0?L("CYCLING / HATCHES LOCKED"):lockPressure>50?L("CABIN PRESSURE / E to vent"):L("VACUUM / E to pressurize");
  default:return focus.Used?L("COMPLETE"):L("WITHIN REACH");
 }}
}
}

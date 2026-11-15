using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LunarRelay {
public sealed partial class GameSession:MonoBehaviour {
 public Simulation Sim;public PlayerRig Player;public bool Playing=>started&&!paused&&!showInventory&&!showStatus&&!showLog&&!showSources&&!Sim.Aborted&&!Sim.Complete;
 bool started,paused,showLog,showSources,airCartridgeReady;int seed=1;float wallTime,toastUntil,cycleRemaining,lockPressure=101.3f;
 string toast="",lastStage="";StationAction focus,held;readonly List<StationAction> actions=new List<StationAction>();readonly List<string> history=new List<string>();
 readonly List<Transform> doorTransforms=new List<Transform>();StationAction inner,outer,shelter;Light headLamp;AudioSource audioSource;AudioClip click;
 public bool Outside {
  get {var p=Player.transform.position;if(p.y<-.2f||p.y>3)return true;
   bool habitat=p.x*p.x+p.z*p.z<1.66f*1.66f;
   bool shelterZone=p.x>=-4.13f&&p.x<=-1.45f&&Mathf.Abs(p.z)<.95f;
   bool pod=(p.x+7.65f)*(p.x+7.65f)+p.z*p.z<2.45f*2.45f;
   bool connector=p.x>=-5.3f&&p.x<=-3.95f&&Mathf.Abs(p.z)<.65f;
   bool airlock=p.x>=1.45f&&p.x<=4.15f&&Mathf.Abs(p.z)<.88f;
   return !(habitat||shelterZone||pod||connector||(airlock&&lockPressure>50));
  }
 }
 public bool Sheltered {get {var p=Player.transform.position;return p.x>=-3.95f&&p.x< -1.7f&&Mathf.Abs(p.z)<.85f&&p.y>=0&&p.y<2.3f;}}
 public string SmokePath="";
 void Awake(){
  if(!Application.isMobilePlatform&&Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke")<0)Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);
  if(Application.isMobilePlatform){Application.targetFrameRate=30;QualitySettings.shadowDistance=25;QualitySettings.shadows=ShadowQuality.HardOnly;Screen.orientation=ScreenOrientation.LandscapeLeft;}InitializeLanguage();seed=1+PlayerPrefs.GetInt("LunarRelaySeed",0);Sim=new Simulation(seed);if(Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke")<0)Sim.EnableExpansion(seed);Sim.Endless=Sim.Expanded&&PlayerPrefs.GetInt("LunarRelayEndless",1)==1;BuildWorld();BuildPlayer();BuildAudio();FinishPresentation();Log(L("Welcome to a future Moon-base training exercise. All readings are simulated."));
  #if UNITY_WEBGL && !UNITY_EDITOR
  if(Application.absoluteURL.Contains("smoke=1"))StartCoroutine(SmokeTest());
  #else
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke")>=0){SmokePath=Path.Combine(Application.dataPath,"..","Smoke");Directory.CreateDirectory(SmokePath);StartCoroutine(Array.IndexOf(Environment.GetCommandLineArgs(),"--mobile-only")>=0?MobileOnlyChecks():SmokeTest());}
  #endif
 }
 void Update(){
  MobileUpdate();UpdatePresentation();EngineeringPresentation();PersistenceShortcuts();DecorUpdate();
  var kb=Keyboard.current;if(kb!=null){if(kb.iKey.wasPressedThisFrame)ToggleInventory();if(kb.escapeKey.wasPressedThisFrame)Shortcut(false);if(kb.tabKey.wasPressedThisFrame&&started)Shortcut(true);if(kb.f12Key.wasPressedThisFrame)ScreenCapture.CaptureScreenshot("LunarRelay-screenshot.png");}
  foreach(var a in actions)if(a&&a.Moving&&a.Kind.ToString().StartsWith("Door"))a.Moving.localRotation=Quaternion.Slerp(a.Moving.localRotation,Quaternion.Euler(a.Open?a.OpenEuler:a.ClosedEuler),Time.deltaTime*5);
  if(!Playing)return;
  wallTime+=Time.deltaTime;
  if(cycleRemaining>0){cycleRemaining-=Time.deltaTime;lockPressure=Mathf.MoveTowards(lockPressure,cycleToCabin?101.3f:0,Time.deltaTime*15);if(cycleRemaining<=0){lockPressure=cycleToCabin?101.3f:0;Tell(L("Airlock cycle complete. Check the pressure before opening a hatch."));}}
  Sim.Step(Time.deltaTime*40,Outside,Sheltered);
  UpdateIdleTasks(Time.deltaTime);
  if(polarSun)polarSun.intensity=Sim.Shadow?.025f:1.7f;
  string stage=Sim.CycleHour<2?L("PREPARE"):Sim.Shadow?L("LOCAL SHADOW"):Sim.CycleHour<5?L("RECOVERY"):Sim.Storm?L("SHELTER DRILL"):L("FINAL CHECKS");
  if(stage!=lastStage){lastStage=stage;Log(StageExplanation());Tell(StageExplanation());}
  if(Sim.Aborted||Sim.Complete){UpdateCursor();Log(Sim.Outcome);}
  if(Player.transform.position.y< -10){Player.Place(new Vector3(0,.3f,.1f),180);Tell(L("Training position reset to the habitat."));}
 }
 int lastShortcutFrame=-1;
 void Shortcut(bool guide){if(lastShortcutFrame==Time.frameCount)return;lastShortcutFrame=Time.frameCount;if(guide){if(!started||Sim.Complete||Sim.Aborted)return;showInventory=false;showLog=false;showSources=false;paused=false;showStatus=!showStatus;}else if(showStatus)showStatus=false;else if(showInventory)showInventory=false;else if(showSources)showSources=false;else if(showLog)showLog=false;else if(started)paused=!paused;UpdateCursor();}
 void OnApplicationFocus(bool hasFocus){if(!hasFocus&&started&&Playing&&string.IsNullOrEmpty(SmokePath)){paused=true;UpdateCursor();}}
 bool cycleToCabin;
 void UpdateCursor(){Cursor.lockState=Playing&&!Application.isMobilePlatform?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!Playing||Application.isMobilePlatform;}
 void StartShift(){RestoreFirstPerson();started=true;paused=false;UpdateCursor();Tell(Sim.Expanded?UI("Подойдите к экрану очистителя и нажмите E для проверки.","Approach the air-cleaner screen and press E to inspect."):L("Pick up the torque driver in the sleeping pod. TAB opens your field guide."));}
 string StageExplanation(){if(Sim.CycleHour<2)return L("Prepare: fix air cleaning, stop rover charging and stock the shelter.");if(Sim.Shadow)return L("The ridge blocks the Sun. Batteries now carry every load.");if(Sim.CycleHour<5)return L("Sunlight has returned. Repair the exterior array connection, then return inside.");if(Sim.Storm)return L("Particle-event exercise: stay in the stocked shelter. This event did not cause the shadow.");return Sim.Endless?UI("Поддерживайте базу до следующего снабжения в начале нового цикла.","Keep the base running until scheduled supplies arrive at the next cycle."):L("Final checks: keep air cleaning and thermal control powered until hour 8.");}
 void Log(string s){history.Add(s);if(history.Count>30)history.RemoveAt(0);}
 void Tell(string s){toast=s;toastUntil=Time.unscaledTime+7;}
 public void UpdateFocus(){focus=null;if(Physics.Raycast(Player.View.transform.position,Player.View.transform.forward,out var hit,2.8f,~(1<<2),QueryTriggerInteraction.Collide))focus=hit.collider.GetComponentInParent<StationAction>();if(focus&&focus.Used&&focus.Kind!=ActionKind.InstallPack)focus=null;}
 public void UseFocus(){if(focus)Use(focus);}
 public void ToggleLamp(){headLamp.enabled=!headLamp.enabled;}
 void ExecuteUse(StationAction a){
  if(a==null)return;Beep();if(HandsUse(a))return;if(a.Used)return;if(EngineeringUse(a))return;
  switch(a.Kind){
   case ActionKind.Tool:case ActionKind.Cartridge:case ActionKind.Pack:case ActionKind.Battery:Pickup(a);break;
   case ActionKind.Main:Sim.MainPower=!Sim.MainPower;AnimateSwitch(a,Sim.MainPower);Tell(Sim.MainPower?L("Power bus online."):L("Power isolated. Cabin systems are now off too. Work quickly."));break;
   case ActionKind.Air:Sim.AirOn=!Sim.AirOn;AnimateSwitch(a,Sim.AirOn);Tell(L("Air-cleaner power: ")+(Sim.AirOn?L("ON"):L("OFF")));break;
   case ActionKind.Thermal:Sim.ThermalOn=!Sim.ThermalOn;AnimateSwitch(a,Sim.ThermalOn);Tell(L("Thermal-control power: ")+(Sim.ThermalOn?L("ON"):L("OFF")));break;
   case ActionKind.Rover:Sim.RoverOn=!Sim.RoverOn;AnimateSwitch(a,Sim.RoverOn);Tell(Sim.RoverOn?L("Rover charging uses 1.8 kW."):L("Rover charging paused. More power stays available for the crew."));break;
   case ActionKind.Oxygen:Sim.O2Valve=!Sim.O2Valve;AnimateSwitch(a,Sim.O2Valve);Tell(L("Stored-oxygen regulator: ")+(Sim.O2Valve?L("OPEN"):L("CLOSED")));break;
   case ActionKind.RepairAir:
    if(Sim.ScrubberRepaired){Tell(L("Air cleaner is repaired. Check its power and CO2 trend."));break;}
    if(Sim.MainPower&&Sim.AirOn){Unsafe(L("Turn air-cleaner power OFF before service."));break;}
    if(!airCartridgeReady){if(Carrying(ActionKind.Cartridge)){SeatItem(ServiceItem(ActionKind.Cartridge),cartridgeSocket,true);airCartridgeReady=true;Tell(L("Fresh cartridge seated. Use the torque driver to secure the service cover."));}else Tell(L("Fit the spare CO2 cartridge from the rack first."));}
    else if(Carrying(ActionKind.Tool)){Sim.ScrubberRepaired=true;Sim.Repairs++;Tell(L("Air cleaner repaired. Turn its power ON and watch CO2 fall."));Log(L("Cartridge changed with the circuit isolated. CO2 removal can run again."));}else Tell(L("Use the torque driver to secure this cover."));break;
   case ActionKind.RepairArray:
    if(Sim.ArrayRepaired){Tell(L("Array connection is already secure."));break;}
    if(Sim.MainPower){Unsafe(L("Isolate the main power bus before repairing its connection."));break;}
    if(Carrying(ActionKind.Tool)){Sim.ArrayRepaired=true;Sim.Repairs++;Tell(L("Array connection secured. Restore main power before returning inside."));Log(L("Repaired array raises available sunlit power from 1.1 to 3.6 kW (scenario design values)."));}else Tell(L("Bring the torque driver."));break;
   case ActionKind.ReplaceBattery:
    if(Sim.MainPower){Unsafe(L("Isolate main power before changing a battery."));break;}
    if(Carrying(ActionKind.Battery)){SeatItem(ServiceItem(ActionKind.Battery),a.transform,true);Sim.Energy=Math.Min(Sim.Capacity,Sim.Energy+2);a.Used=true;Tell(L("Spare battery connected: 2 kWh added to the bus reserve."));Log(L("Used the finite spare battery; it cannot be reused."));}else Tell(L("Fit the spare battery from the service shelf."));break;
   case ActionKind.InstallPack:
    if(Carrying(ActionKind.Pack)){if(Sim.Expanded&&!Sim.FillShield(Sim.Packs)){Tell("Not enough clean water: 4 L is needed to fill this shielding block.");break;}SeatItem(ServiceItem(ActionKind.Pack),a.transform,false);a.Used=true;Sim.Packs++;Tell(L("Water secured in shelter slot. It is still usable drinking water."));Log(L("Stowed water pack ")+Sim.Packs+L(". Added mass helps the illustrative shelter index."));}else Tell(L("Carry a sealed water pack here from the sleeping pod."));break;
   case ActionKind.DoorInner:ToggleDoor(a,true);break;
   case ActionKind.DoorOuter:ToggleDoor(a,false);break;
   case ActionKind.DoorShelter:a.Open=!a.Open;break;
   case ActionKind.Cycle:
    if(Sim.Expanded&&lockPressure>50&&!Sim.SuitEquipped){Tell(UI("Проверьте скафандр на приборе шлюза перед выходом.","Check and equip your suit at the airlock gauge before venting."));break;}
    if(!Sealed(inner)||!Sealed(outer)){Tell(L("Close both airlock hatches and wait until they seal before cycling pressure."));break;}
    if(cycleRemaining>0)break;cycleToCabin=lockPressure<50;cycleRemaining=7;Tell(cycleToCabin?L("Filling the airlock. Both hatches stay locked."):L("Recovering air, then venting the airlock. Suit check complete."));break;
   default:Tell(a.Label);break;
  }
 }
 void Unsafe(string s){Sim.UnsafeAttempts++;Tell(s);Log(L("Safety interlock: ")+s);}
 bool Sealed(StationAction hatch)=>!hatch.Open&&Quaternion.Angle(hatch.Moving.localRotation,Quaternion.Euler(hatch.ClosedEuler))<2;
 void AnimateSwitch(StationAction a,bool on){foreach(var control in actions)if(control&&control.Kind==a.Kind&&control.Moving){var lever=control.GetComponent<LeverMotion>();if(lever)lever.Set(on);else control.Moving.localRotation=Quaternion.Euler(on?new Vector3(0,0,25):new Vector3(0,0,-25));}}
 void ToggleDoor(StationAction a,bool isInner){
  if(a.Open){if(Vector3.Distance(Player.transform.position,new Vector3(isInner?1.59f:4.04f,Player.transform.position.y,0))<.45f){Tell(L("Step clear of the hatch before closing it."));return;}a.Open=false;return;}
  if(cycleRemaining>0){Tell(L("Pressure cycle in progress."));return;}
  if(isInner?(!Sealed(outer)||lockPressure<99):(!Sealed(inner)||lockPressure>1)){Tell(isInner?L("Close the outer hatch and fill the airlock first."):L("Close the inner hatch and vent the airlock first."));return;}
  a.Open=true;
 }
 void Pickup(StationAction a){Equip(a);if(held==a)Tell(L("Carrying: ")+L(a.Label)+L(". R stores it in the backpack; I opens inventory."));}
 void SetHeldColliders(StationAction a,bool on){foreach(var c in a.GetComponentsInChildren<Collider>())c.enabled=on;}
 public void Drop(){if(!held)return;var item=held;held=null;ReleaseItem(item);}
 void ConsumeHeld(){if(held){held.Used=true;held.gameObject.SetActive(false);held=null;}}
 void BuildAudio(){audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;var samples=new float[2205];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*660/44100)*.06f*(1-i/(float)samples.Length);click=AudioClip.Create("Suit control tone",samples.Length,1,44100,false);click.SetData(samples,0);}
 void Beep(){if(audioSource&&click)audioSource.PlayOneShot(click);}
 void BuildPlayer(){var go=new GameObject("Cadet");Player=go.AddComponent<PlayerRig>();Player.Game=this;Player.Body=go.AddComponent<CharacterController>();Player.Body.height=1.62f;Player.Body.radius=.20f;Player.Body.center=new Vector3(0,.81f,0);Player.Body.stepOffset=.25f;Player.Body.skinWidth=.025f;Player.Body.slopeLimit=50;var view=new GameObject("Suit view");view.transform.SetParent(go.transform,false);view.transform.localPosition=new Vector3(0,1.48f,0);view.tag="MainCamera";Player.View=view.AddComponent<Camera>();Player.View.nearClipPlane=.04f;Player.View.farClipPlane=450;Player.View.fieldOfView=76;view.AddComponent<AudioListener>();var lampMount=new GameObject("Suit lamp mount");lampMount.transform.SetParent(view.transform,false);lampMount.transform.localPosition=new Vector3(.18f,.08f,.02f);headLamp=lampMount.AddComponent<Light>();headLamp.type=LightType.Spot;headLamp.range=16;headLamp.spotAngle=75;headLamp.intensity=3;headLamp.color=new Color(.8f,.94f,1);Player.Place(new Vector3(0,.26f,.25f),180);}
}
public sealed class LunarBody:MonoBehaviour {Rigidbody rb;void Awake(){rb=GetComponent<Rigidbody>();}void FixedUpdate(){if(rb&&!rb.isKinematic)rb.AddForce(Vector3.down*1.62f,ForceMode.Acceleration);}}
}

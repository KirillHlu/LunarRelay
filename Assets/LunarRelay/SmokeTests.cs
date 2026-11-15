using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 #if UNITY_WEBGL && !UNITY_EDITOR
 [System.Runtime.InteropServices.DllImport("__Internal")]static extern void LR_ReportSmoke(string text);
 #endif
 readonly List<string> smokeResults=new List<string>();bool smokeOK=true;
 StationAction Action(ActionKind kind){return actions.Find(a=>a&&a.Kind==kind&&!a.Used);}
 void Verify(bool condition,string label){smokeResults.Add((condition?"PASS ":"FAIL ")+label);if(!condition)smokeOK=false;Debug.Log(smokeResults[smokeResults.Count-1]);}
 IEnumerator Photograph(string name,Vector3 position,Vector3 target){Player.View.transform.localPosition=new Vector3(0,1.48f,0);Player.Place(position);Player.View.transform.rotation=Quaternion.LookRotation(target-Player.View.transform.position);yield return null;yield return null;
  #if UNITY_WEBGL && !UNITY_EDITOR
  LR_ReportSmoke("STAGE "+name);
  #else
  var camera=Player.View;var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;LateUpdate();camera.Render();RenderTexture.active=rt;var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(SmokePath,name+"-scene.png"),texture.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;Destroy(rt);Destroy(texture);
  #endif
  yield return new WaitForSecondsRealtime(.4f);
 }
 IEnumerator WalkTo(Vector3 target){int count=0;while(Vector2.Distance(new Vector2(Player.transform.position.x,Player.transform.position.z),new Vector2(target.x,target.z))>.10f&&count++<320){var delta=target-Player.transform.position;delta.y=0;Player.Body.Move(delta.normalized*.055f+Vector3.down*.025f);yield return null;}Verify(count<320,"walkable route to "+target);}
 IEnumerator SmokeTest(){
  yield return new WaitForSecondsRealtime(2);
  #if !UNITY_WEBGL || UNITY_EDITOR
  ScreenCapture.CaptureScreenshot(Path.Combine(SmokePath,"00-menu.png"));yield return new WaitForSecondsRealtime(.5f);
  #endif
  RestoreFirstPerson();started=true;paused=true;lastStage="PREPARE";
  Verify(itemIcons.Count==5,"five rendered equipment thumbnails");
  bool languageBefore=useRussian;useRussian=true;Verify(L("BACKPACK")=="РЮКЗАК"&&L("Torque driver")=="Ремонтный инструмент","Russian labels loaded");useRussian=false;Verify(L("BACKPACK")=="BACKPACK","English language available");useRussian=languageBefore;
  Player.Place(new Vector3(0,.27f,.45f));Verify(Player.SetCrouch(true)&&Player.Body.height<1.1f,"crouch reduces physical capsule");
  var ceiling=new GameObject("Smoke low clearance");ceiling.transform.position=Player.transform.position+Vector3.up*1.37f;var roof=ceiling.AddComponent<BoxCollider>();roof.size=new Vector3(1,.15f,1);Physics.SyncTransforms();
  Verify(!Player.SetCrouch(false)&&Player.Crouching,"cannot stand through low ceiling");Destroy(ceiling);yield return null;Verify(Player.SetCrouch(false)&&Player.Body.height>1.6f,"stand when clearance returns");
  var items=actions.FindAll(a=>a.Kind==ActionKind.Tool||a.Kind==ActionKind.Pack||a.Kind==ActionKind.Cartridge||a.Kind==ActionKind.Battery);
  var originalPositions=new List<Vector3>();var originalRotations=new List<Quaternion>();var originalParents=new List<Transform>();
  foreach(var item in items){originalPositions.Add(item.transform.position);originalRotations.Add(item.transform.rotation);originalParents.Add(item.transform.parent);Pickup(item);StoreHeld();}
  Verify(backpack.Count==6&&held==null,"six distinct objects stored in backpack");
  Equip(items[0]);Verify(held==items[0]&&backpack.Count==5&&held.gameObject.activeSelf,"equip from backpack without duplication");
  Equip(items[1]);Verify(held==items[1]&&backpack.Count==5&&backpack.Contains(items[0]),"swap held and stored items");StoreHeld();
  var extra=new GameObject("Smoke capacity item").AddComponent<StationAction>();extra.Kind=ActionKind.Tool;Equip(extra);Verify(!StoreHeld()&&held==extra&&backpack.Count==6,"full backpack preserves held object");held=null;Destroy(extra.gameObject);
  var stored=backpack[0];DropStored(stored);Verify(!backpack.Contains(stored)&&stored.gameObject.activeSelf&&!stored.GetComponent<Rigidbody>().isKinematic,"drop from backpack restores physical object");
  foreach(var item in items){backpack.Remove(item);item.gameObject.SetActive(true);item.GetComponent<Rigidbody>().isKinematic=true;SetHeldColliders(item,true);int i=items.IndexOf(item);item.transform.SetParent(originalParents[i],true);item.transform.SetPositionAndRotation(originalPositions[i],originalRotations[i]);}
  ToggleInventory();Verify(showInventory&&!Playing,"inventory pauses simulation");showInventory=false;paused=true;Physics.SyncTransforms();
  Verify(Resources.Load<Shader>("ObjectiveOutline")!=null&&Resources.Load<Shader>("ObjectiveOutline").isSupported,"objective outline shader included and supported");
  paused=false;lastShortcutFrame=-1;Shortcut(true);Verify(showStatus&&!Playing&&Cursor.visible,"Tab opens monitoring and releases cursor");
  yield return null;Shortcut(true);Verify(!showStatus&&Playing,"Tab closes monitoring and resumes movement");
  yield return null;Shortcut(true);yield return null;Shortcut(false);Verify(!showStatus&&!paused,"Escape closes monitoring without stacking pause");paused=true;
  Verify(Player.View.backgroundColor.maxColorComponent<.01f,"black lunar sky");
  Verify(Resources.Load<UnityEngine.Rendering.VolumeProfile>("OutpostLook")!=null,"color profile included in build");
  Verify(cabinLights.FindAll(l=>l.shadows!=LightShadows.None).Count==4,"four shadowed practical lights");
  foreach(var part in rebuiltAirlock.GetComponentsInChildren<MeshFilter>()){
   if(part.name=="Recessed seal jamb")Verify(Mathf.Abs(Mathf.Abs(part.transform.localPosition.z)-part.transform.localScale.z*.5f-.5f)>.01f,"trim jamb separated from wall plane");
   if(part.name=="Recessed seal header")Verify(Mathf.Abs(part.transform.localPosition.y-part.transform.localScale.y*.5f-2.10f)>.01f,"trim header separated from lintel plane");
  }
  yield return Photograph("00-open-frame",new Vector3(.45f,.27f,-.18f),new Vector3(2.4f,1.35f,0));
  inner.Open=false;inner.Moving.localRotation=Quaternion.Euler(inner.ClosedEuler);Physics.SyncTransforms();
  foreach(var hatch in new[]{inner,outer})foreach(float side in new[]{-1f,1f})foreach(float skew in new[]{-.35f,0,.35f}){
   bool sealedView=true;float x=hatch.transform.position.x;
   foreach(float y in new[]{.23f,.28f,1.1f,2.04f,2.095f})foreach(float z in new[]{-.499f,-.47f,0,.47f,.499f}){
    var target=new Vector3(x,y,z);var origin=target+new Vector3(side*.45f,0,skew);
    sealedView &= Physics.Raycast(origin,(target-origin).normalized,(target-origin).magnitude+.25f,~(1<<2),QueryTriggerInteraction.Ignore);
   }
   Verify(sealedView,"closed rebuilt hatch perimeter / "+hatch.Kind+" / "+side+" / "+skew);
  }
  yield return Photograph("00a-outer-exterior-left",new Vector3(5.25f,.27f,-.55f),new Vector3(4.04f,1.25f,0));
  yield return Photograph("00b-outer-exterior-right",new Vector3(5.25f,.27f,.55f),new Vector3(4.04f,1.25f,0));
  yield return Photograph("00c-inner-cabin",new Vector3(.25f,.27f,0),new Vector3(1.59f,1.25f,0));
  inner.Open=true;inner.Moving.localRotation=Quaternion.Euler(inner.OpenEuler);Physics.SyncTransforms();
  foreach(float end in new[]{1.55f,4.05f})foreach(float z in new[]{-.85f,-.4f,0,.4f,.85f}){
   Verify(Physics.Raycast(new Vector3(end-.35f,2.16f,z),Vector3.right,.7f,~(1<<2),QueryTriggerInteraction.Ignore),"sealed airlock header at "+end+" / "+z);
  }
  yield return Photograph("00-corridor-header",new Vector3(3.0f,.27f,0),new Vector3(4.06f,2.15f,0));
  Verify(correctedSolarMounts==4,"all four masts and drives are behind the photovoltaic face");
  bool separate=solarEquipmentBounds.Count==8;
  for(int a=0;a<solarEquipmentBounds.Count;a++)for(int b=a+1;b<solarEquipmentBounds.Count;b++){
   var first=solarEquipmentBounds[a];var second=solarEquipmentBounds[b];
   float gapX=Mathf.Max(second.min.x-first.max.x,first.min.x-second.max.x);
   float gapZ=Mathf.Max(second.min.z-first.max.z,first.min.z-second.max.z);
   separate &= Mathf.Max(gapX,gapZ)>=.6f;
  }
  Verify(separate,"all eight solar arrays and radiators have at least 0.6m clearance");
  yield return Photograph("00-panel-front",new Vector3(6,.27f,-7),new Vector3(6,1.7f,-5.4f));
  yield return Photograph("00-panel-rear",new Vector3(6,.27f,-3),new Vector3(6,1.7f,-5));
  yield return Photograph("00-solar-layout",new Vector3(5.5f,.27f,-11),new Vector3(11.4f,1.2f,-5.8f));
  Verify(actions.Count>=20,"interactive equipment bindings");Verify(inner&&outer&&shelter,"three hatch bindings");
  foreach(var action in actions)if(action&&(action.Kind!=ActionKind.Info||action.GetComponent<HandControl>()))Verify(Reachable(action),"raycast access: "+action.Label);
  yield return Photograph("01-habitat",new Vector3(0,.27f,.45f),new Vector3(0,1.3f,-1.3f));
  Sim.MainPower=false;yield return new WaitForSecondsRealtime(1.5f);yield return Photograph("01b-emergency",new Vector3(0,.27f,.45f),new Vector3(0,1.3f,-1.3f));Sim.MainPower=true;yield return new WaitForSecondsRealtime(1.5f);
  yield return WalkTo(new Vector3(-2.7f,.27f,0));yield return WalkTo(new Vector3(-6.2f,.27f,0));
  Verify(!Outside&&!Sheltered,"sleeping pod is pressurized but not storm shelter");
  yield return Photograph("02-sleeping-pod",new Vector3(-5.7f,.27f,0),new Vector3(-8,1.1f,.2f));
  Use(Action(ActionKind.Tool));Verify(held&&held.Kind==ActionKind.Tool,"tool pickup");Verify(held&&Vector3.Dot((held.transform.TransformPoint(heldTipPoint)-held.transform.TransformPoint(heldRearPoint)).normalized,Player.View.transform.forward)>.97f,"driver points forward along sightline");Verify(toolGripVisual&&Vector3.Distance(toolGripVisual.position,held.transform.TransformPoint(heldGripPoint))<.001f,"pressure glove anchored to driver grip");
  yield return Photograph("02a-held-driver",new Vector3(-5.7f,.27f,0),new Vector3(-8,1.4f,0));
  Drop();yield return new WaitForSecondsRealtime(.7f);Verify(held==null,"tool release and lunar Rigidbody");
  yield return WalkTo(new Vector3(-2.7f,.27f,0));yield return WalkTo(new Vector3(0,.27f,0));
  int unsafeBefore=Sim.UnsafeAttempts;Use(Action(ActionKind.RepairAir));Verify(!Sim.ScrubberRepaired&&Sim.UnsafeAttempts==unsafeBefore+1,"live air service prevented");
  Verify(Objective().Kind==ActionKind.Air,"objective first targets air isolation");
  var cartridge=Action(ActionKind.Cartridge);Use(cartridge);StoreHeld();Use(cartridgeService);
  Verify(!airCartridgeReady&&backpack.Contains(cartridge),"live service does not consume backpack cartridge");
  Use(Action(ActionKind.Air));Verify(Objective()==cartridgeService,"carried cartridge targets its socket");
  Use(Action(ActionKind.Tool));var keptTool=held;Use(cartridgeService);
  Verify(held==keptTool&&!backpack.Contains(cartridge)&&cartridge.Used&&cartridge.gameObject.activeSelf,"E seats backpack cartridge and keeps held tool");
  Verify(Vector3.Distance(cartridge.transform.TransformPoint(LocalBounds(cartridge.transform).center),cartridgeSocket.TransformPoint(LocalBounds(cartridgeSocket).center))<.001f&&Quaternion.Angle(cartridge.transform.rotation,cartridgeSocket.rotation)<.01f,"cartridge exactly aligned with socket");
  Verify(cartridge.GetComponent<Rigidbody>().isKinematic&&!cartridge.GetComponent<Collider>().enabled,"installed cartridge cannot fall or be picked twice");
  Use(Action(ActionKind.RepairAir));Use(Action(ActionKind.Air));Verify(Sim.ScrubberRepaired&&Sim.AirOn,"cartridge plus tool repair and power restore");Drop();
  for(int i=0;i<3;i++){Use(Action(ActionKind.Pack));Use(Action(ActionKind.InstallPack));}Verify(Sim.Packs==3,"three physical packs seated once");
  yield return WalkTo(new Vector3(2.9f,.27f,0));
  unsafeBefore=Sim.UnsafeAttempts;Use(outer);Verify(!outer.Open,"outer hatch interlock at cabin pressure");
  Use(inner);Verify(!inner.Open,"inner hatch closes clear of cadet");yield return new WaitForSecondsRealtime(1);Use(Action(ActionKind.Cycle));paused=false;yield return new WaitForSecondsRealtime(8);paused=true;Verify(lockPressure<1&&cycleRemaining<=0,"airlock vent cycle");Use(outer);yield return new WaitForSecondsRealtime(1);yield return WalkTo(new Vector3(5.1f,.27f,0));Verify(Outside,"surface reachable through supplied hatch geometry");
  yield return Photograph("03-lunar-outpost",new Vector3(8,.22f,-9),new Vector3(0,1.2f,0));
  Player.Place(new Vector3(-3,.25f,4));Verify(Outside&&!Sheltered,"outside west wall is vacuum, not shelter");
  Use(Action(ActionKind.Rover));Use(Action(ActionKind.Tool));Use(Action(ActionKind.RepairArray));Verify(!Sim.ArrayRepaired,"live connection repair prevented");Use(Action(ActionKind.Main));Use(Action(ActionKind.RepairArray));Use(Action(ActionKind.Main));Verify(Sim.ArrayRepaired&&Sim.MainPower&&!Sim.RoverOn,"isolated power repair and load shedding");Drop();
  yield return Photograph("04-power-module",new Vector3(8,.3f,8.7f),new Vector3(8,1.8f,5.2f));
  Player.Place(new Vector3(2.9f,.27f,0));outer.Open=false;yield return new WaitForSecondsRealtime(1);Use(Action(ActionKind.Cycle));paused=false;yield return new WaitForSecondsRealtime(8);paused=true;Verify(lockPressure>99,"airlock fill cycle");Use(inner);yield return new WaitForSecondsRealtime(.8f);yield return WalkTo(new Vector3(0,.27f,0));
  var good=new Simulation(7){ScrubberRepaired=true,ArrayRepaired=true,Packs=3,RoverOn=false};good.Step(8*3600,false,true);Verify(good.Complete,"complete prepared mission");
  var bad=new Simulation(7);bad.Step(8*3600,true,false);Verify(bad.Aborted,"unsafe scenario produces training abort");
  #if !UNITY_WEBGL || UNITY_EDITOR
  Player.Place(new Vector3(0,.27f,.45f),180);paused=false;toastUntil=0;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(SmokePath,"06-hud.png"));yield return new WaitForSecondsRealtime(1);paused=true;
  #endif
  Sim=good;yield return Photograph("05-mission-debrief",new Vector3(-2.8f,.27f,0),new Vector3(-3.5f,1,.5f));
  yield return EngineeringRuntimeChecks();
  yield return MobileRuntimeChecks();
  var report=string.Join("\n",smokeResults)+"\n"+(smokeOK?"ALL CHECKS PASSED":"CHECKS FAILED");
  #if UNITY_WEBGL && !UNITY_EDITOR
  LR_ReportSmoke(report);
  #else
  File.WriteAllText(Path.Combine(SmokePath,"smoke-results.txt"),report+"\nAutomated runtime checks, not a manual playthrough or WebGL browser test.\n");
  Application.Quit(smokeOK?0:2);
  #endif
 }
 bool Reachable(StationAction action){
  var collider=action.GetComponent<Collider>();if(!collider)return false;var target=collider.bounds.center;
  foreach(float radius in new[]{.55f,.85f,1.2f,1.7f,2.1f})for(int i=0;i<24;i++){
   float angle=i*Mathf.PI/12;var foot=new Vector3(target.x+Mathf.Cos(angle)*radius,.27f,target.z+Mathf.Sin(angle)*radius);
   if(Physics.CheckCapsule(foot+Vector3.up*.25f,foot+Vector3.up*1.38f,.18f,~(1<<2),QueryTriggerInteraction.Ignore))continue;
   var eye=foot+Vector3.up*1.48f;var ray=target-eye;if(ray.magnitude>2.8f)continue;
   if(Physics.Raycast(eye,ray.normalized,out var hit,2.8f,~(1<<2),QueryTriggerInteraction.Collide)&&hit.collider.GetComponentInParent<StationAction>()==action)return true;
  }return false;
 }
 Vector2 MobileScreenPoint(RectTransform rect,Vector2 offset){return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(offset));}
 void MobileTestTick(){mobileInputFrame=-1;MobileUpdate(true);}
 void MobileTestTouch(UnityEngine.InputSystem.Touchscreen device,int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 point){
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=id,phase=phase,position=point});
  UnityEngine.InputSystem.InputSystem.Update();MobileTestTick();
 }
 Vector2 MobileTestGuiPoint(float x,float y){var area=Application.isMobilePlatform?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);float scale=Mathf.Min(area.width/1440,area.height/900);return new Vector2(area.x+(area.width-1440*scale)*.5f+x*scale,area.y+area.height-(area.height-900*scale)*.5f-y*scale);}
 IEnumerator MobileOnlyChecks(){yield return new WaitForSecondsRealtime(2);yield return MobileRuntimeChecks();File.WriteAllText(Path.Combine(SmokePath,"mobile-results.txt"),string.Join("\n",smokeResults)+"\n"+(smokeOK?"ALL CHECKS PASSED":"CHECKS FAILED"));Application.Quit(smokeOK?0:2);}
 IEnumerator MobileRuntimeChecks(){
  var backgroundBefore=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;RestoreFirstPerson();
  var previousSim=Sim;bool wasPaused=paused;float fov=Player.View.fieldOfView;var position=Player.transform.position;float yaw=Player.transform.eulerAngles.y;
  Sim=new Simulation(9);started=true;paused=showStatus=showInventory=showLog=showSources=false;Player.Place(new Vector3(0,.27f,0));
  var device=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
  MobileTestTick();Canvas.ForceUpdateCanvases();MobileTestTick();
  Verify(mobileCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode==UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize,"touch HUD uses Canvas Scaler");
  Verify(mobileSafe.anchorMin.x>=0&&mobileSafe.anchorMax.x<=1,"touch HUD respects safe area anchors");
  foreach(var pair in mobileButtons){var r=pair.Value.rectTransform;Verify(r.rect.width>=96,"touch target at least96 reference units: "+pair.Key);}
  var previewObject=new GameObject("Touch HUD verification camera");var previewCamera=previewObject.AddComponent<Camera>();previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.12f,.15f,.18f);previewCamera.cullingMask=1<<29;previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=5;
  foreach(var t in mobileCanvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
  var previewRT=new RenderTexture(1600,900,24);previewCamera.targetTexture=previewRT;mobileCanvas.renderMode=RenderMode.ScreenSpaceCamera;mobileCanvas.worldCamera=previewCamera;mobileCanvas.planeDistance=1;Canvas.ForceUpdateCanvases();previewCamera.Render();
  var activeRT=RenderTexture.active;RenderTexture.active=previewRT;var previewImage=new Texture2D(1600,900,TextureFormat.RGB24,false);previewImage.ReadPixels(new Rect(0,0,1600,900),0,0);previewImage.Apply();File.WriteAllBytes(Path.Combine(SmokePath,"android-touch-hud.png"),previewImage.EncodeToPNG());RenderTexture.active=activeRT;
  mobileCanvas.renderMode=RenderMode.ScreenSpaceOverlay;mobileCanvas.worldCamera=null;previewCamera.targetTexture=null;previewRT.Release();Destroy(previewRT);Destroy(previewImage);Destroy(previewObject);Canvas.ForceUpdateCanvases();
  var move=MobileScreenPoint(mobileStick,Vector2.zero);var look=new Vector2(Screen.safeArea.center.x+Screen.safeArea.width*.12f,Screen.safeArea.center.y);
  MobileTestTouch(device,91,UnityEngine.InputSystem.TouchPhase.Began,move);
  MobileTestTouch(device,91,UnityEngine.InputSystem.TouchPhase.Moved,MobileScreenPoint(mobileStick,new Vector2(0,70)));
  Verify(mobileMove.y>.65f&&mobileLook==Vector2.zero,"joystick moves forward without camera drag");
  MobileTestTouch(device,92,UnityEngine.InputSystem.TouchPhase.Began,look);
  MobileTestTouch(device,92,UnityEngine.InputSystem.TouchPhase.Moved,look+new Vector2(45,0));
  Verify(mobileMove.y>.65f&&mobileLook.x>0,"simultaneous joystick and right-finger look");
  var jump=MobileScreenPoint(mobileButtons["jump"].rectTransform,Vector2.zero);
  MobileTestTouch(device,93,UnityEngine.InputSystem.TouchPhase.Began,jump);
  Verify(MobileJump&&mobileOwners[93]=="jump"&&mobileOwners[91]=="move"&&mobileOwners[92]=="look","third finger activates shared jump action without stealing stick or camera");MobileJump=false;
  MobileTestTouch(device,93,UnityEngine.InputSystem.TouchPhase.Moved,look);
  Verify(mobileOwners.ContainsKey(93)&&mobileOwners[93]=="jump"&&!MobileJump,"button finger stays captured and action fires only once");
  MobileTestTouch(device,93,UnityEngine.InputSystem.TouchPhase.Ended,look);
  MobileTestTouch(device,94,UnityEngine.InputSystem.TouchPhase.Began,look+new Vector2(120,0));
  float before=mobileFov;
  MobileTestTouch(device,94,UnityEngine.InputSystem.TouchPhase.Moved,look+new Vector2(220,0));
  Verify(mobileFov<before&&mobileLook==Vector2.zero&&mobileMove.y>.65f,"pinch zoom works alongside joystick and suppresses look");
  MobileTestTouch(device,94,UnityEngine.InputSystem.TouchPhase.Moved,look+new Vector2(Screen.height*3,0));
  Verify(mobileFov==20,"pinch FOV minimum is20 degrees");
  mobileFov=89;
  MobileTestTouch(device,94,UnityEngine.InputSystem.TouchPhase.Moved,look+new Vector2(100,0));
  Verify(mobileFov==90,"pinch FOV maximum is90 degrees");
  MobileTestTouch(device,94,UnityEngine.InputSystem.TouchPhase.Canceled,look);
  Verify(!mobileOwners.ContainsKey(94),"canceled touch releases pinch ownership");
  var tablet=MobileScreenPoint(mobileButtons["tablet"].rectTransform,Vector2.zero);
  lastShortcutFrame=-1;MobileTestTouch(device,95,UnityEngine.InputSystem.TouchPhase.Began,tablet);
  Verify(showStatus&&!Playing&&mobileMove==Vector2.zero&&mobileLook==Vector2.zero&&!mobileCanvas.enabled,"tablet blocks movement and look and clears pending gestures");
  showStatus=false;MobileTestTick();
  Verify(mobileMove==Vector2.zero&&mobileLook==Vector2.zero,"fingers held while closing menu cannot resume stale movement");
  MobileTestTouch(device,91,UnityEngine.InputSystem.TouchPhase.Ended,move);MobileTestTouch(device,92,UnityEngine.InputSystem.TouchPhase.Ended,look);MobileTestTouch(device,95,UnityEngine.InputSystem.TouchPhase.Ended,tablet);
  MobileTestTouch(device,96,UnityEngine.InputSystem.TouchPhase.Began,move);MobileTestTouch(device,96,UnityEngine.InputSystem.TouchPhase.Moved,MobileScreenPoint(mobileStick,new Vector2(-90,0)));
  Verify(mobileMove.x<-.95f,"fresh gesture works after tablet closes");
  MobileTestTouch(device,96,UnityEngine.InputSystem.TouchPhase.Ended,move);
  Verify(mobileMove==Vector2.zero,"lifting joystick finger immediately stops movement");
  lastInventoryFrame=-1;MobileAction("pack");Verify(showInventory&&!Playing,"touch inventory button opens existing backpack and blocks play");MobileTestTick();showInventory=false;MobileTestTick();lastShortcutFrame=-1;MobileAction("pause");Verify(paused&&!Playing,"touch pause button opens existing pause menu");MobileTestTick();paused=false;MobileTestTick();
  Player.Place(new Vector3(100,100,100));var target=new GameObject("Touch interaction test");target.transform.position=Player.View.transform.position+Player.View.transform.forward*.6f;var targetCollider=target.AddComponent<BoxCollider>();targetCollider.size=Vector3.one*.2f;var targetAction=target.AddComponent<StationAction>();targetAction.Kind=ActionKind.Rover;Physics.SyncTransforms();UpdateFocus();bool rover=Sim.RoverOn;if(focus!=targetAction){Physics.Raycast(Player.View.transform.position,Player.View.transform.forward,out var blocked,2.8f,~(1<<2),QueryTriggerInteraction.Collide);Debug.Log("TOUCH FOCUS: "+(blocked.collider?blocked.collider.name:"none")+" at "+blocked.distance+" eye "+Player.View.transform.position+" forward "+Player.View.transform.forward+" expected "+target.transform.position);}
  Verify(focus==targetAction,"touch interaction fixture has unobstructed focus");MobileAction("use");Verify(Sim.RoverOn!=rover,"touch interaction uses existing equipment action");target.SetActive(false);Destroy(target);
  bool crouch=Player.Crouching;MobileAction("crouch");Verify(MobileCrouch!=crouch,"touch crouch requests existing character stance");MobileCrouch=false;
  placingKind=0;decorValid=false;Verify(!MobileButtonAvailable("place")&&!MobileButtonAvailable("use"),"invalid decor placement disables confirm and world use");decorValid=true;
  MobileAction("rotate");float angle=decorAngle;MobileAction("place");MobileAction("cancel");
  Verify(mobilePlace&&mobileCancel&&angle%15==0,"decor touch buttons route placement rotation and cancellation");placingKind=-1;decorValid=false;ResetMobileGestures();
  Verify(!System.Text.RegularExpressions.Regex.IsMatch(MobileText("WASD move; E use; Tab tablet\nE — inspect · F5",true),@"\b(?:WASD|E|Tab|F5)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase),"mobile labels omit keyboard shortcuts");
  mobileUITest=true;paused=true;var menuPoint=MobileTestGuiPoint(720,292);MobileTestTouch(device,108,UnityEngine.InputSystem.TouchPhase.Began,menuPoint);MobileTestTouch(device,108,UnityEngine.InputSystem.TouchPhase.Ended,menuPoint);
  Verify(mobileMenuReleaseFrame==Time.frameCount&&mobileMenuStart==menuPoint&&mobileMenuPoint==menuPoint,"menu touch records one released tap independently of mouse input");
  MobileTestTouch(device,109,UnityEngine.InputSystem.TouchPhase.Began,menuPoint);MobileTestTouch(device,109,UnityEngine.InputSystem.TouchPhase.Canceled,menuPoint);
  Verify(mobileMenuFinger==-1&&mobileMenuReleaseFrame<0,"canceled menu touch cannot buy or activate a button");
  if(mobileGuiRepaints>0){
  float sensitivityBefore=MobileSensitivity;var resume=MobileTestGuiPoint(720,292);
  MobileTestTouch(device,101,UnityEngine.InputSystem.TouchPhase.Began,resume);MobileTestTouch(device,101,UnityEngine.InputSystem.TouchPhase.Ended,resume);yield return null;
  Verify(!paused,"touch tap activates existing IMGUI resume button without mouse emulation");
  paused=true;var slider=MobileTestGuiPoint(720,395);MobileTestTouch(device,102,UnityEngine.InputSystem.TouchPhase.Began,slider);MobileTestTouch(device,102,UnityEngine.InputSystem.TouchPhase.Moved,MobileTestGuiPoint(980,395));yield return null;
  Verify(MobileSensitivity>2,"touch drag adjusts existing sensitivity slider");MobileTestTouch(device,102,UnityEngine.InputSystem.TouchPhase.Ended,slider);MobileSensitivity=sensitivityBefore;
  paused=false;showInventory=true;selectedSlot=0;var slot=MobileTestGuiPoint(492,369);MobileTestTouch(device,103,UnityEngine.InputSystem.TouchPhase.Began,slot);MobileTestTouch(device,103,UnityEngine.InputSystem.TouchPhase.Ended,slot);yield return null;
  Verify(selectedSlot==1,"touch selects an inventory slot without mouse emulation");
  showInventory=false;showStatus=true;Sim.EnableExpansion(9);shopPage=false;var shop=MobileTestGuiPoint(1105,231);MobileTestTouch(device,104,UnityEngine.InputSystem.TouchPhase.Began,shop);MobileTestTouch(device,104,UnityEngine.InputSystem.TouchPhase.Ended,shop);yield return null;
  Verify(shopPage,"touch opens existing shop page");
  string profileBefore=JsonUtility.ToJson(decorProfile);decorProfile.Credits=200;int ownedBefore=decorProfile.Owned[0];var buy=MobileTestGuiPoint(292,589);MobileTestTouch(device,105,UnityEngine.InputSystem.TouchPhase.Began,buy);MobileTestTouch(device,105,UnityEngine.InputSystem.TouchPhase.Ended,buy);yield return null;
  Verify(decorProfile.Credits==150&&decorProfile.Owned[0]==ownedBefore+1,"touch shop purchase charges exactly once");
  var placeButton=MobileTestGuiPoint(442,589);MobileTestTouch(device,106,UnityEngine.InputSystem.TouchPhase.Began,placeButton);MobileTestTouch(device,106,UnityEngine.InputSystem.TouchPhase.Ended,placeButton);yield return null;
  Verify(DecorPlacementActive&&!showStatus,"touch shop inventory enters existing decor placement");CancelDecor();decorProfile=JsonUtility.FromJson<DecorProfile>(profileBefore);SaveDecor();
  showStatus=false;showLog=true;scroll=Vector2.zero;var guide=MobileTestGuiPoint(650,500);MobileTestTouch(device,107,UnityEngine.InputSystem.TouchPhase.Began,guide);MobileTestTouch(device,107,UnityEngine.InputSystem.TouchPhase.Moved,MobileTestGuiPoint(650,350));yield return null;
  Verify(scroll.y>50,"touch drag scrolls existing field guide");MobileTestTouch(device,107,UnityEngine.InputSystem.TouchPhase.Ended,MobileTestGuiPoint(650,350));showLog=false;
  }else Debug.Log("SKIP native IMGUI touch activation/slider/scroll tests: hidden or batch player has no GUI repaint. Requires visible game or Android device.");
  mobileUITest=false;
  UnityEngine.InputSystem.InputSystem.RemoveDevice(device);mobileCanvas.enabled=false;Player.View.fieldOfView=fov;Player.Place(position,yaw);Sim=previousSim;paused=wasPaused;MobileCrouch=MobileJump=false;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=backgroundBefore;UpdateCursor();
  yield return null;
 }

}
}

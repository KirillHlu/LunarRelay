using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LunarRelay {
public sealed partial class GameSession {
 public bool MobileCrouch,MobileJump;
 bool mobilePlace,mobileCancel;
 Canvas mobileCanvas; RectTransform mobileSafe,mobileStick,mobileKnob;
 readonly Dictionary<string,RawImage> mobileButtons=new Dictionary<string,RawImage>();
 readonly Dictionary<int,string> mobileOwners=new Dictionary<int,string>();
 readonly Dictionary<int,Vector2> mobilePrevious=new Dictionary<int,Vector2>();
 readonly HashSet<int> mobileAlive=new HashSet<int>();
 readonly List<int> mobileReleased=new List<int>();
 readonly List<int> mobileLooks=new List<int>();
 Vector2 mobileMove,mobileLook,mobileLookVelocity;
 float mobileFov=-1; bool mobileWasPlaying;
 Texture2D mobileDisc;
 public float MobileSensitivity=1;

 RectTransform MobileGraphic(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Texture2D texture,Color color){
  var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
  var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=anchor;rt.anchoredPosition=position;rt.sizeDelta=size;
  var image=go.GetComponent<RawImage>();image.texture=texture;image.color=color;image.raycastTarget=false;return rt;
 }
 Texture2D MobileIcon(string name){
  const int size=96;var pixels=new Color[size*size];var lines=new List<Vector4>();
  System.Action<float,float,float,float> line=(a,b,c,d)=>lines.Add(new Vector4(a,b,c,d));
  System.Action<float,float,float,float> rect=(a,b,c,d)=>{line(a,b,c,b);line(c,b,c,d);line(c,d,a,d);line(a,d,a,b);};
  switch(name){
   case "tablet":rect(25,16,71,80);line(39,25,57,25);line(35,44,61,44);line(35,55,61,55);line(35,66,54,66);break;
   case "pause":rect(30,24,38,72);rect(58,24,66,72);break;
   case "pack":rect(25,24,71,76);rect(37,15,59,24);rect(34,50,62,67);line(31,35,65,35);break;
   case "use":line(28,68,22,46);line(22,46,30,42);line(30,42,37,51);line(37,51,37,24);line(37,24,44,22);line(44,22,47,44);line(47,44,53,29);line(53,29,60,31);line(60,31,59,47);line(59,47,67,38);line(67,38,74,42);line(74,42,68,65);line(68,65,57,76);line(57,76,37,76);line(37,76,28,68);break;
   case "crouch":line(44,32,54,32);line(42,44,60,52);line(60,52,74,52);line(43,44,35,59);line(35,59,55,66);line(55,66,66,78);line(35,59,24,76);break;
   case "jump":line(48,70,48,24);line(48,24,30,42);line(48,24,66,42);line(25,76,71,76);break;
   case "lamp":rect(26,38,52,58);line(52,38,63,28);line(63,28,63,68);line(63,68,52,58);line(72,35,80,30);line(73,48,84,48);line(72,61,80,66);break;
   case "store":rect(24,48,72,75);line(48,17,48,57);line(35,44,48,57);line(61,44,48,57);break;
   case "drop":line(48,20,48,60);line(35,47,48,60);line(61,47,48,60);line(24,76,72,76);break;
   case "place":line(22,49,40,67);line(40,67,76,29);break;
   case "cancel":line(28,28,68,68);line(28,68,68,28);break;
   case "rotate":line(27,64,22,43);line(22,43,35,25);line(35,25,59,23);line(59,23,74,43);line(74,43,74,61);line(74,61,62,51);line(74,61,82,48);break;
  }
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   var p=new Vector2(x,y);float distance=999;
   foreach(var l in lines){var a=new Vector2(l.x,l.y);var d=new Vector2(l.z,l.w)-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.01f,d.sqrMagnitude));distance=Mathf.Min(distance,Vector2.Distance(p,a+d*t));}
   pixels[(size-1-y)*size+x]=new Color(.83f,.91f,.96f,Mathf.Clamp01(2.7f-distance));
  }
  var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.name="Touch icon "+name;tex.SetPixels(pixels);tex.Apply();return tex;
 }
 void BuildMobileControls(){
  mobileDisc=new Texture2D(128,128,TextureFormat.RGBA32,false);var pixels=new Color[128*128];
  for(int y=0;y<128;y++)for(int x=0;x<128;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f));float edge=Mathf.Clamp01(63-r);float rim=Mathf.Clamp01((r-59)*.5f);pixels[y*128+x]=Color.Lerp(new Color(.045f,.065f,.085f,.72f),new Color(.48f,.63f,.72f,.8f),rim)*new Color(1,1,1,edge);}
  mobileDisc.SetPixels(pixels);mobileDisc.Apply();
  var root=new GameObject("Android touch controls",typeof(Canvas),typeof(CanvasScaler));mobileCanvas=root.GetComponent<Canvas>();mobileCanvas.renderMode=RenderMode.ScreenSpaceOverlay;mobileCanvas.sortingOrder=50;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,800);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
  mobileSafe=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();mobileSafe.SetParent(root.transform,false);
  mobileStick=MobileGraphic("Movement",mobileSafe,Vector2.zero,new Vector2(154,159),new Vector2(228,228),mobileDisc,Color.white);
  mobileKnob=MobileGraphic("Stick",mobileStick,new Vector2(.5f,.5f),Vector2.zero,new Vector2(88,88),mobileDisc,new Color(.65f,.84f,.95f,.9f));
  AddMobileButton("tablet",new Vector2(1,1),new Vector2(-172,-72),96);
  AddMobileButton("pause",Vector2.one,new Vector2(-60,-72),96);
  AddMobileButton("pack",Vector2.one,new Vector2(-284,-72),96);
  AddMobileButton("use",new Vector2(1,0),new Vector2(-112,256),112);
  AddMobileButton("jump",new Vector2(1,0),new Vector2(-64,108),96);
  AddMobileButton("crouch",new Vector2(1,0),new Vector2(-184,88),96);
  AddMobileButton("lamp",new Vector2(1,0),new Vector2(-304,88),96);
  AddMobileButton("store",new Vector2(1,0),new Vector2(-240,228),96);
  AddMobileButton("drop",new Vector2(1,0),new Vector2(-352,228),96);
  AddMobileButton("place",new Vector2(1,0),new Vector2(-112,256),112);
  AddMobileButton("rotate",new Vector2(1,0),new Vector2(-240,228),96);
  AddMobileButton("cancel",new Vector2(1,0),new Vector2(-184,88),96);
  MobileSensitivity=PlayerPrefs.GetFloat("LunarRelayTouchSensitivity",1);
 }
 void AddMobileButton(string name,Vector2 anchor,Vector2 position,float size){
  var rt=MobileGraphic(name,mobileSafe,anchor,position,Vector2.one*size,mobileDisc,Color.white);
  mobileButtons[name]=rt.GetComponent<RawImage>();MobileGraphic(name+" icon",rt,new Vector2(.5f,.5f),Vector2.zero,Vector2.one*(size*.64f),MobileIcon(name),Color.white);
 }
 bool MobileButtonAvailable(string name){
  if(DecorPlacementActive)return name=="tablet"||name=="pause"||name=="pack"||name=="rotate"||name=="cancel"||(name=="place"&&decorValid);
  switch(name){case "place":case "rotate":case "cancel":return false;case "use":return focus||MobileDecorTarget();case "store":return held&&backpack.Count<BackpackCapacity;case "drop":return held;default:return true;}
 }
 bool MobileDecorTarget()=>Physics.Raycast(Player.View.transform.position,Player.View.transform.forward,out var h,2.8f,~(1<<2))&&h.collider.GetComponentInParent<PlacedDecor>();
 string MobileHit(Vector2 screen){
  foreach(var pair in mobileButtons){var r=pair.Value.rectTransform;if(r.gameObject.activeSelf&&RectTransformUtility.ScreenPointToLocalPointInRectangle(r,screen,null,out var p)&&p.magnitude<=r.rect.width*.5f+6)return pair.Key;}
  if(RectTransformUtility.ScreenPointToLocalPointInRectangle(mobileStick,screen,null,out var point)&&point.magnitude<134)return "move";
  return screen.x>Screen.safeArea.center.x?"look":"ignore";
 }
 void MobileAction(string name){
  if(!MobileButtonAvailable(name))return;
  switch(name){case "use":MobileInteract();break;case "tablet":Shortcut(true);break;case "pause":Shortcut(false);break;case "pack":ToggleInventory();break;case "store":StoreHeld();break;case "drop":Drop();break;case "lamp":ToggleLamp();break;case "jump":MobileJump=true;break;case "crouch":MobileCrouch=!Player.Crouching;break;case "place":mobilePlace=true;break;case "rotate":decorAngle+=15;break;case "cancel":mobileCancel=true;break;}
 }
 void ResetMobileGestures(){mobileOwners.Clear();mobilePrevious.Clear();mobileMove=mobileLook=mobileLookVelocity=Vector2.zero;MobileJump=false;mobilePlace=mobileCancel=false;mobileFov=Player?Player.View.fieldOfView:60;}
 Rect mobileLastSafe;Vector2 mobileLastSize;
 int mobileInputFrame=-1;
 void MobileUpdate(bool test=false){
  if((!Application.isMobilePlatform&&!test)||mobileInputFrame==Time.frameCount)return;mobileInputFrame=Time.frameCount;
  if(!mobileCanvas)BuildMobileControls();
  var safe=Screen.safeArea;var size=new Vector2(Screen.width,Screen.height);if(safe!=mobileLastSafe||size!=mobileLastSize){ResetMobileGestures();mobileLastSafe=safe;mobileLastSize=size;}mobileSafe.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);mobileSafe.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);mobileSafe.offsetMin=mobileSafe.offsetMax=Vector2.zero;
  mobileCanvas.enabled=Playing;
  if(!Playing){ReadMobileMenu();ResetMobileGestures();mobileWasPlaying=false;return;}
  mobileMenuFinger=-1;
  if(!mobileWasPlaying){ResetMobileGestures();mobileWasPlaying=true;}
  UpdateFocus();
  foreach(var pair in mobileButtons){string name=pair.Key;bool placement=name=="place"||name=="rotate"||name=="cancel";bool visible=placement==DecorPlacementActive||name=="tablet"||name=="pause"||name=="pack";
   if(name=="store"||name=="drop")visible&=held;pair.Value.gameObject.SetActive(visible);
   bool active=MobileButtonAvailable(name);var tint=!active?new Color(.5f,.55f,.6f,.35f):(mobileOwners.ContainsValue(name)||name=="crouch"&&Player.Crouching||name=="lamp"&&headLamp.enabled)?new Color(.5f,.8f,1,1):Color.white;pair.Value.color=Color.Lerp(pair.Value.color,tint,1-Mathf.Exp(-(name=="use"?28:18)*Time.unscaledDeltaTime));pair.Value.transform.GetChild(0).GetComponent<RawImage>().color=active?Color.white:new Color(.7f,.75f,.8f,.3f);
  }
  mobileMove=mobileLook=Vector2.zero;mobileAlive.Clear();mobileLooks.Clear();var screen=Touchscreen.current;
  if(screen!=null)foreach(var touch in screen.touches){int id=touch.touchId.ReadValue();if(!touch.press.isPressed)continue;var point=touch.position.ReadValue();mobileAlive.Add(id);
   if(touch.press.wasPressedThisFrame&&!mobileOwners.ContainsKey(id)){string owner=MobileHit(point);if(owner=="move"&&mobileOwners.ContainsValue("move"))owner="ignore";mobileOwners[id]=owner;mobilePrevious[id]=point;if(owner!="move"&&owner!="look"&&owner!="ignore")MobileAction(owner);if(!Playing){ResetMobileGestures();mobileCanvas.enabled=false;return;}}
   if(!mobileOwners.TryGetValue(id,out var action))continue;
   if(action=="move"&&RectTransformUtility.ScreenPointToLocalPointInRectangle(mobileStick,point,null,out var local)){float distance=local.magnitude;mobileMove=distance<9?Vector2.zero:local.normalized*Mathf.Clamp01((distance-9)/75);}
   if(action=="look")mobileLooks.Add(id);
  }
  mobileReleased.Clear();foreach(var pair in mobileOwners)if(!mobileAlive.Contains(pair.Key))mobileReleased.Add(pair.Key);foreach(int id in mobileReleased){mobileOwners.Remove(id);mobilePrevious.Remove(id);}
  if(screen!=null&&mobileLooks.Count>=2){
   int a=mobileLooks[0],b=mobileLooks[1];Vector2 pa=TouchPosition(a),pb=TouchPosition(b);
   // A fresh second finger starts a new pinch baseline without a FOV jump.
   float old=Vector2.Distance(mobilePrevious[a],mobilePrevious[b]),now=Vector2.Distance(pa,pb);
   mobileFov=Mathf.Clamp(mobileFov-(now-old)/Mathf.Max(1,Screen.safeArea.height)*100,20,90);mobileLookVelocity=Vector2.zero;
  }else if(mobileLooks.Count==1){int id=mobileLooks[0];var delta=TouchPosition(id)-mobilePrevious[id];var target=delta/Mathf.Max(1,Screen.safeArea.height)*100*MobileSensitivity/Mathf.Max(.001f,Time.unscaledDeltaTime);
   mobileLookVelocity=Vector2.Lerp(mobileLookVelocity,target,1-Mathf.Exp(-35*Time.unscaledDeltaTime));mobileLook=mobileLookVelocity*Time.unscaledDeltaTime;
  }else mobileLookVelocity=Vector2.zero;
  foreach(int id in mobileLooks)mobilePrevious[id]=TouchPosition(id);
  mobileKnob.anchoredPosition=mobileMove*84;
  Player.View.fieldOfView=Mathf.Lerp(Player.View.fieldOfView,mobileFov,1-Mathf.Exp(-14*Time.unscaledDeltaTime));
 }
 Vector2 TouchPosition(int id){foreach(var t in Touchscreen.current.touches)if(t.touchId.ReadValue()==id&&t.press.isPressed)return t.position.ReadValue();return mobilePrevious[id];}
 public void ReadMobileInput(out Vector2 move,out Vector2 look){MobileUpdate();move=mobileMove;look=mobileLook;}
 void MobileInteract(){if(Physics.Raycast(Player.View.transform.position,Player.View.transform.forward,out var hit,2.8f,~(1<<2))){var decor=hit.collider.GetComponentInParent<PlacedDecor>();if(decor){BeginDecor(decor.Kind,decor.Id);return;}}UpdateFocus();UseFocus();}
 // Existing tablet/menu rendering is IMGUI. Route touch directly because the project
 // uses Input System only; IMGUI otherwise receives no touch-to-mouse events.
 int mobileGuiRepaints;float mobileMenuReleasedAt;
 bool mobileUITest;bool MobileMenuInput=>Application.isMobilePlatform||mobileUITest;
 int mobileMenuFinger=-1,mobileMenuReleaseFrame=-1,mobileMenuConsumedFrame=-1,mobileScrollFrame=-1;
 Vector2 mobileMenuStart,mobileMenuPoint,mobileMenuDelta;Rect? mobileScrollClip;
 void ReadMobileMenu(){
  mobileMenuDelta=Vector2.zero;var device=Touchscreen.current;if(device==null)return;
  foreach(var touch in device.touches){int id=touch.touchId.ReadValue();var point=touch.position.ReadValue();
   if(mobileMenuFinger<0&&touch.press.wasPressedThisFrame){mobileMenuFinger=id;mobileMenuStart=mobileMenuPoint=point;mobileMenuReleaseFrame=-1;}
   if(id!=mobileMenuFinger)continue;
   mobileMenuDelta=point-mobileMenuPoint;mobileMenuPoint=point;
   if(touch.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled){mobileMenuFinger=-1;mobileMenuReleaseFrame=-1;return;}
   if(touch.press.wasReleasedThisFrame){mobileMenuReleasedAt=Time.unscaledTime;mobileMenuReleaseFrame=Time.frameCount;mobileMenuFinger=-1;return;}
  }
 }
 Vector2 MobileGuiPoint(Vector2 screen)=>GUIUtility.ScreenToGUIPoint(new Vector2(screen.x,Screen.height-screen.y));
 bool MobileGUIButton(Rect rect){
  if(!GUI.enabled||Event.current.type!=EventType.Repaint||mobileMenuReleaseFrame<0||Time.unscaledTime-mobileMenuReleasedAt>.4f||mobileMenuConsumedFrame==Time.frameCount)return false;
  if(mobileScrollClip.HasValue&&!mobileScrollClip.Value.Contains(new Vector2(mobileMenuPoint.x,Screen.height-mobileMenuPoint.y)))return false;
  var start=MobileGuiPoint(mobileMenuStart);var end=MobileGuiPoint(mobileMenuPoint);
  if(!rect.Contains(start)||!rect.Contains(end)||Vector2.Distance(start,end)>18)return false;
  mobileMenuConsumedFrame=Time.frameCount;mobileMenuReleaseFrame=-1;return true;
 }
 float TouchSlider(Rect rect,float value,float min,float max){
  if(MobileMenuInput&&mobileMenuFinger>=0&&GUI.enabled){var start=MobileGuiPoint(mobileMenuStart);var point=MobileGuiPoint(mobileMenuPoint);var hit=rect;hit.yMin-=12;hit.yMax+=12;if(hit.Contains(start))value=Mathf.Lerp(min,max,Mathf.Clamp01((point.x-rect.x)/rect.width));}
  return GUI.HorizontalSlider(rect,value,min,max,sliderTrack,sliderThumb);
 }
 Vector2 TouchScrollView(Rect rect,Vector2 value,Rect content){
  if(MobileMenuInput){
   var a=GUIUtility.GUIToScreenPoint(rect.min);var b=GUIUtility.GUIToScreenPoint(rect.max);mobileScrollClip=Rect.MinMaxRect(a.x,a.y,b.x,b.y);
   if(mobileMenuFinger>=0&&Event.current.type==EventType.Repaint&&mobileScrollFrame!=Time.frameCount&&rect.Contains(MobileGuiPoint(mobileMenuStart))){var end=MobileGuiPoint(mobileMenuPoint);var prior=MobileGuiPoint(mobileMenuPoint-mobileMenuDelta);value.y=Mathf.Clamp(value.y-(end.y-prior.y),0,Mathf.Max(0,content.height-rect.height));mobileScrollFrame=Time.frameCount;}
  }
  return GUI.BeginScrollView(rect,value,content);
 }
 void EndTouchScrollView(){GUI.EndScrollView();mobileScrollClip=null;}

 void OnDestroy(){
  if(mobileCanvas){foreach(var raw in mobileCanvas.GetComponentsInChildren<RawImage>(true))if(raw.texture&&raw.texture!=mobileDisc)Destroy(raw.texture);Destroy(mobileCanvas.gameObject);}
  if(mobileDisc)Destroy(mobileDisc);
 }

 string MobileText(string text,bool test=false){
  if(!Application.isMobilePlatform&&!test)return text;
  if(text=="E")return "";
  if(text.Contains(" — мониторинг; управление")||text.Contains("Tab opens monitoring; controls"))return UI("Настройки базы — в планшете. Управление — в справке.","Base settings are in the tablet. See the field guide for controls.");
  text=System.Text.RegularExpressions.Regex.Replace(text,@"(?im)^.*WASD[^\n]*(?:\n|$)",UI("Джойстик — движение · справа — обзор · два пальца — масштаб\n","Joystick to move · drag right to look · pinch to zoom\n"));
  text=System.Text.RegularExpressions.Regex.Replace(text,@"(?i)\b(?:press|нажмите) E\b",UI("используйте кнопку действия","use the action button"));
  text=text.Replace("with E","with the action button").Replace("по E","кнопкой действия");
  text=System.Text.RegularExpressions.Regex.Replace(text,@"(?i)\bE\s*[—–-]\s*","");
  text=System.Text.RegularExpressions.Regex.Replace(text,@"(?i)\bE (?:on|seats)\b",UI("Действие","Interact"));
  text=System.Text.RegularExpressions.Regex.Replace(text,@"(?i)\bTAB\b",UI("Планшет","Tablet"));
  text=System.Text.RegularExpressions.Regex.Replace(text,@"(?i)\bE\b",UI("Действие","Interact"));
  text=System.Text.RegularExpressions.Regex.Replace(text,@"(?i)\b(?:ESC|F5|F9|WASD)\b","");
  text=text.Replace("I · close","Simulation paused").Replace("I · закрыть","Время приостановлено").Replace("по E","кнопкой действия").Replace("with Q","with the release button").Replace("with R","with the store button").Replace("· /", "");
  return text;
 }
}
}

using System.Collections.Generic;
using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 Texture2D panelGrain; GUIStyle sliderTrack,sliderThumb;
 readonly Dictionary<int,float> hoverAmounts=new Dictionary<int,float>();
 readonly Color surface=new Color(.07f,.20f,.31f,.84f),paper=new Color(.87f,.94f,.98f);
 void Styles(){
  if(stylesReady)return;stylesReady=true;
  var font=Resources.Load<Font>("Fonts/PT_Sans-Web-Regular");
  var bold=Resources.Load<Font>("Fonts/PT_Sans-Web-Bold");
  small=new GUIStyle(GUI.skin.label){font=font,fontSize=16,wordWrap=true};small.normal.textColor=Color.white;
  body=new GUIStyle(small){fontSize=18};heading=new GUIStyle(body){font=bold,fontSize=23};
  title=new GUIStyle(heading){fontSize=46};micro=new GUIStyle(small){fontSize=15};
  button=new GUIStyle(GUI.skin.button){font=bold,fontSize=17,wordWrap=true,padding=new RectOffset(18,18,8,8),border=new RectOffset()};
  foreach(var state in new[]{button.normal,button.hover,button.active,button.focused}){state.background=Texture2D.blackTexture;state.textColor=paper;}
  sliderTrack=new GUIStyle(GUI.skin.horizontalSlider);sliderTrack.normal.background=PanelTexture(surface);sliderTrack.fixedHeight=6;sliderTrack.margin=new RectOffset(0,0,12,0);
  sliderThumb=new GUIStyle(GUI.skin.horizontalSliderThumb);sliderThumb.fixedWidth=18;sliderThumb.fixedHeight=28;sliderThumb.normal.background=PanelTexture(paper);sliderThumb.hover.background=PanelTexture(amber);sliderThumb.active.background=PanelTexture(amber);
  panelGrain=new Texture2D(64,64,TextureFormat.RGBA32,false);panelGrain.wrapMode=TextureWrapMode.Repeat;var random=new System.Random(18);
  for(int y=0;y<64;y++)for(int x=0;x<64;x++)panelGrain.SetPixel(x,y,Color.clear);panelGrain.Apply();
 }
 bool Button(float x,float y,float w,float h,string text){
  var rect=new Rect(x,y,w,h);string label=MobileText(L(text));
  bool primary=label==L("BEGIN TRAINING")||label==L("RESUME SHIFT")||label==L("BACK TO SHIFT")||label==L("TRY ANOTHER SHIFT");
  int key=Mathf.RoundToInt(x*31+y*7+w);bool over=rect.Contains(Event.current.mousePosition)&&GUI.enabled;
  hoverAmounts.TryGetValue(key,out float amount);
  if(reducedEffects)amount=over?1:0;
  if(Event.current.type==EventType.Repaint&&!reducedEffects){amount=Mathf.MoveTowards(amount,over?1:0,Time.unscaledDeltaTime/(over?.12f:.23f));hoverAmounts[key]=amount;}
  Color fill=Color.Lerp(primary?cyan:surface,primary?paper:new Color(.24f,.28f,.29f),amount);
  if(over&&UnityEngine.InputSystem.Mouse.current!=null&&UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)fill=Color.Lerp(fill,ink,.22f);
  if(!GUI.enabled)fill=new Color(.055f,.105f,.15f,.8f);
  Box(x+1,y+2,w,h,new Color(.01f,.025f,.04f,.35f));Box(x,y,w,h,fill);
  Box(x,y,w,1,primary?paper:muted*.55f);
  if(primary)Box(x,y,4,h,paper);
  var oldBg=GUI.backgroundColor;GUI.backgroundColor=Color.clear;
  foreach(var state in new[]{button.normal,button.hover,button.active,button.focused})state.textColor=!GUI.enabled?muted:primary?ink:paper;
  GUI.SetNextControlName("LRButton"+key);bool clicked;if(MobileMenuInput){GUI.Label(rect,label,button);clicked=MobileGUIButton(rect);}else clicked=GUI.Button(rect,label,button);GUI.backgroundColor=oldBg;
  if(GUI.enabled&&Event.current.type==EventType.Repaint&&(over||GUI.GetNameOfFocusedControl()=="LRButton"+key))Box(x,y+h-2,w,2,primary?ink:cyan);
  return clicked;
 }
}
}

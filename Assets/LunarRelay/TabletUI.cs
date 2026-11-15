using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 int selectedSlot;
 readonly Dictionary<ActionKind,Texture2D> itemIcons=new Dictionary<ActionKind,Texture2D>();
 void TabletTabs(int active){
  string[] labels={UI("РЮКЗАК","INVENTORY"),UI("БАЗА","BASE STATUS"),UI("СПРАВКА","FIELD GUIDE")};
  for(int i=0;i<3;i++){
   float x=208+i*255;
   if(i==active)Box(x,104,242,45,new Color(.16f,.40f,.58f,.85f));
   if(Button(x,104,242,45,labels[i])){showInventory=i==0;showStatus=i==1;showLog=i==2;paused=false;showSources=false;UpdateCursor();}
   if(i==active)Box(x+16,147,210,2,cyan);
  }
 }
 void ItemImage(StationAction item,Rect rect){
  if(item&&itemIcons.TryGetValue(item.Kind,out var icon))GUI.DrawTexture(rect,icon,ScaleMode.ScaleToFit,true);
 }
 IEnumerator Start(){
  yield return null;
  foreach(var item in actions){
   if(!item||itemIcons.ContainsKey(item.Kind)||!(item.Kind==ActionKind.Tool||item.Kind==ActionKind.Cartridge||item.Kind==ActionKind.Pack||item.Kind==ActionKind.Battery||item.Kind==ActionKind.Brush))continue;
   var root=new GameObject("Inventory model preview");root.layer=31;
   foreach(var source in item.GetComponentsInChildren<MeshFilter>(true)){
    var renderer=source.GetComponent<MeshRenderer>();if(!renderer||!source.sharedMesh)continue;
    var part=new GameObject("Preview mesh");part.layer=31;part.transform.SetParent(root.transform,false);
    part.transform.localPosition=item.transform.InverseTransformPoint(source.transform.position);
    part.transform.localRotation=Quaternion.Inverse(item.transform.rotation)*source.transform.rotation;
    part.transform.localScale=new Vector3(source.transform.lossyScale.x/item.transform.lossyScale.x,source.transform.lossyScale.y/item.transform.lossyScale.y,source.transform.lossyScale.z/item.transform.lossyScale.z);
    part.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;part.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
   }
   var bounds=LocalBounds(root.transform);root.transform.position=new Vector3(0,-1000,0);root.transform.rotation=Quaternion.Euler(12,25,0);
   var center=root.transform.TransformPoint(bounds.center);float radius=Mathf.Max(.05f,bounds.size.magnitude*.5f);
   var camObject=new GameObject("Preview camera");var cam=camObject.AddComponent<Camera>();cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.orthographic=true;cam.orthographicSize=radius*1.15f;cam.nearClipPlane=.01f;cam.farClipPlane=radius*10+10;cam.transform.position=center+Vector3.forward*(radius*3+1);cam.transform.LookAt(center);
   var lightObject=new GameObject("Preview light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<31;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,150,0);
   var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;var previous=RenderTexture.active;cam.Render();RenderTexture.active=rt;
   var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();itemIcons[item.Kind]=texture;
   RenderTexture.active=previous;cam.targetTexture=null;rt.Release();Destroy(rt);Destroy(camObject);Destroy(lightObject);Destroy(root);
   yield return null;
  }
 }
 void TabletInventory(){
  Dim();Box(174,74,1092,752,ink);TabletTabs(0);
  Text(208,185,640,40,UI("ЛИЧНОЕ СНАРЯЖЕНИЕ","PERSONAL EQUIPMENT"),heading,paper);
  Text(208,229,640,28,backpack.Count+" / 6 · "+UI("ЯЧЕЕК ЗАНЯТО","SLOTS OCCUPIED"),micro,cyan);
  for(int i=0;i<6;i++){
   float x=208+(i%3)*194,y=279+(i/3)*194;var item=i<backpack.Count?backpack[i]:null;
   Box(x,y,180,180,new Color(.08f,.19f,.28f,.62f));if(i==selectedSlot)Box(x+12,y+174,156,2,cyan);
   ItemImage(item,new Rect(x+12,y+4,156,130));Text(x+12,y+138,156,38,item?L(item.Label):UI("Свободно","Empty"),micro,item?paper:muted);
   if(MobileMenuInput?MobileGUIButton(new Rect(x,y,180,180)):GUI.Button(new Rect(x,y,180,180),GUIContent.none,GUIStyle.none))selectedSlot=i;
  }
  selectedSlot=Mathf.Clamp(selectedSlot,0,5);var chosen=selectedSlot<backpack.Count?backpack[selectedSlot]:null;
  Text(842,185,370,34,UI("ВЫБРАННЫЙ ПРЕДМЕТ","SELECTED ITEM"),small,cyan);
  ItemImage(chosen,new Rect(880,225,280,210));Text(842,445,370,54,chosen?L(chosen.Label):UI("Выберите предмет в рюкзаке","Select an item in your backpack"),heading);
  GUI.enabled=chosen;if(Button(842,515,370,48,L("TAKE IN HAND")))Equip(chosen);if(Button(842,575,370,44,L("DROP")))DropStored(chosen);GUI.enabled=true;
  Text(208,691,590,40,held?L("IN HAND: ")+L(held.Label):L("HANDS FREE"),body);
  GUI.enabled=held&&backpack.Count<6;if(Button(842,687,370,48,L("STORE IN BACKPACK")))StoreHeld();GUI.enabled=true;
  Text(208,763,590,30,UI("I · закрыть    /    Время приостановлено","I · close    /    Simulation paused"),micro,muted);
  if(Button(1040,755,172,44,UI("Закрыть","Close"))){showInventory=false;UpdateCursor();}
 }
}
}

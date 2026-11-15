using UnityEngine;
using UnityEngine.Rendering;
namespace LunarRelay {
public sealed partial class GameSession {
 Transform toolGripVisual;Vector3 heldGripPoint,heldTipPoint,heldRearPoint;
 Vector3 ToolPoint(StationAction item,string name){var t=FindContains(item.transform,name);return t?item.transform.InverseTransformPoint(t.TransformPoint(LocalBounds(t).center)):Vector3.zero;}
 void PoseHeldItem(StationAction item){
  if(item.Kind!=ActionKind.Tool){item.transform.localRotation=Quaternion.identity;item.transform.localPosition=new Vector3(.27f,-.25f,.55f)-Vector3.Scale(LocalBounds(item.transform).center,item.transform.localScale);return;}
  heldGripPoint=ToolPoint(item,"ThickInsulatedGloveGrip");heldTipPoint=ToolPoint(item,"TrueSquarePyramidTip");heldRearPoint=ToolPoint(item,"RearRubberEndCap");
  var shaft=(heldTipPoint-heldRearPoint).normalized;
  item.transform.localRotation=Quaternion.LookRotation(new Vector3(-.12f,.04f,1),Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(shaft,Vector3.up));
  item.transform.localPosition=new Vector3(.22f,-.22f,.43f)-item.transform.localRotation*Vector3.Scale(heldGripPoint,item.transform.localScale);
  if(!toolGripVisual)BuildToolGlove();SyncToolGlove();
 }
 void GlovePart(PrimitiveType primitive,string name,Vector3 position,Vector3 scale,Material material,Quaternion rotation){
  var part=GameObject.CreatePrimitive(primitive);part.name=name;part.layer=2;part.transform.SetParent(toolGripVisual,false);part.transform.localPosition=position;part.transform.localRotation=rotation;part.transform.localScale=scale;Destroy(part.GetComponent<Collider>());var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
 }
 void BuildToolGlove(){
  toolGripVisual=new GameObject("Right pressure glove").transform;toolGripVisual.SetParent(Player.View.transform,false);
  var cloth=new Material(Shader.Find("Universal Render Pipeline/Lit"));cloth.color=new Color(.64f,.70f,.72f);cloth.SetFloat("_Smoothness",.16f);cloth.SetFloat("_Metallic",0);
  var rubber=new Material(cloth);rubber.color=new Color(.045f,.075f,.10f);
  GlovePart(PrimitiveType.Sphere,"Palm",new Vector3(.033f,-.003f,-.012f),new Vector3(.065f,.09f,.09f),cloth,Quaternion.identity);
  for(int i=0;i<4;i++){
   float y=.025f-i*.018f;
   GlovePart(PrimitiveType.Capsule,"Curled finger",new Vector3(.004f,y,.032f),new Vector3(.021f,.035f,.023f),cloth,Quaternion.Euler(0,0,90));
   GlovePart(PrimitiveType.Sphere,"Finger pad",new Vector3(-.027f,y,.012f),new Vector3(.024f,.019f,.042f),rubber,Quaternion.identity);
  }
  GlovePart(PrimitiveType.Capsule,"Thumb",new Vector3(-.002f,.032f,-.033f),new Vector3(.025f,.036f,.026f),cloth,Quaternion.Euler(35,0,-55));
  GlovePart(PrimitiveType.Cylinder,"Wrist seal",new Vector3(.045f,-.055f,-.055f),new Vector3(.088f,.023f,.088f),rubber,Quaternion.Euler(55,0,-15));
  GlovePart(PrimitiveType.Capsule,"Suit forearm",new Vector3(.088f,-.14f,-.15f),new Vector3(.105f,.11f,.105f),cloth,Quaternion.Euler(48,0,-20));
 }
 void SyncToolGlove(){if(!toolGripVisual)return;bool visible=held&&held.Kind==ActionKind.Tool;toolGripVisual.gameObject.SetActive(visible);if(!visible)return;toolGripVisual.position=held.transform.TransformPoint(heldGripPoint);toolGripVisual.rotation=Player.View.transform.rotation;}
}
}

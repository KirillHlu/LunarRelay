using UnityEngine;
using UnityEngine.InputSystem;
namespace LunarRelay {
public sealed class PlayerRig:MonoBehaviour {
 public Camera View; public CharacterController Body; public GameSession Game;
 float pitch,vertical; public float Speed=2.1f; public float Sensitivity=1f; public bool Crouching {get;private set;}
 void Update(){
  if(Game==null||!Game.Playing)return;
  if(Application.isMobilePlatform){Game.ReadMobileInput(out var stick,out var drag);if(!Game.Playing)return;SetCrouch(Game.MobileCrouch);View.transform.localPosition=Vector3.MoveTowards(View.transform.localPosition,new Vector3(0,Crouching?.90f:1.48f,0),Time.deltaTime*4);transform.Rotate(0,drag.x,0);pitch=Mathf.Clamp(pitch-drag.y,-82,82);View.transform.localEulerAngles=new Vector3(pitch,0,0);if(Body.isGrounded&&vertical<0)vertical=-.4f;if(Game.MobileJump&&Body.isGrounded&&!Crouching)vertical=1.45f;Game.MobileJump=false;vertical-=1.62f*Time.deltaTime;Body.Move(((transform.forward*stick.y+transform.right*stick.x)*Speed*(Crouching?.5f:1)+Vector3.up*vertical)*Time.deltaTime);Game.UpdateFocus();return;}
  var kb=Keyboard.current;var mouse=Mouse.current;if(kb==null||mouse==null)return;
  SetCrouch(kb.leftCtrlKey.isPressed||kb.cKey.isPressed);
  View.transform.localPosition=Vector3.MoveTowards(View.transform.localPosition,new Vector3(0,Crouching?.90f:1.48f,0),Time.deltaTime*4);
  Vector2 look=mouse.delta.ReadValue()*.085f*Sensitivity;transform.Rotate(0,look.x,0);pitch=Mathf.Clamp(pitch-look.y,-82,82);View.transform.localEulerAngles=new Vector3(pitch,0,0);
  Vector3 move=Vector3.zero;if(kb.wKey.isPressed)move+=transform.forward;if(kb.sKey.isPressed)move-=transform.forward;if(kb.dKey.isPressed)move+=transform.right;if(kb.aKey.isPressed)move-=transform.right;
  if(Body.isGrounded&&vertical<0)vertical=-.4f;if(kb.spaceKey.wasPressedThisFrame&&Body.isGrounded&&!Crouching)vertical=1.45f;
  vertical-=1.62f*Time.deltaTime;Body.Move((move.normalized*Speed*(Crouching?.5f:1)+Vector3.up*vertical)*Time.deltaTime);
  Game.UpdateFocus();if(kb.eKey.wasPressedThisFrame&&!Game.DecorPlacementActive)Game.UseFocus();if(kb.rKey.wasPressedThisFrame)Game.StoreHeld();if(kb.qKey.wasPressedThisFrame)Game.Drop();if(kb.fKey.wasPressedThisFrame)Game.ToggleLamp();
 }
 public bool SetCrouch(bool crouch){
  if(!crouch&&Crouching){
   var bottom=transform.position+Vector3.up*1.1f;var top=transform.position+Vector3.up*1.42f;
   foreach(var hit in Physics.OverlapCapsule(bottom,top,.19f,~(1<<2),QueryTriggerInteraction.Ignore))if(hit!=Body&&!hit.transform.IsChildOf(transform))return false;
  }
  Crouching=crouch;Body.height=crouch?1.04f:1.62f;Body.center=Vector3.up*(Body.height*.5f);return true;
 }
 public void Place(Vector3 p,float yaw=0){Body.enabled=false;transform.position=p;transform.rotation=Quaternion.Euler(0,yaw,0);Body.enabled=true;vertical=0;}
}
public enum ActionKind {Greenhouse,WaterRecycler,OxygenGenerator,Brush,CleanPanel,Diagnose,SuitCheck,Info,Tool,Cartridge,Pack,RepairAir,RepairArray,Main,Air,Thermal,Rover,Oxygen,DoorInner,DoorOuter,DoorShelter,Cycle,InstallPack,Battery,ReplaceBattery}
public sealed class StationAction:MonoBehaviour { public ActionKind Kind;public string Label;public bool Used; public Transform Moving;public Vector3 OpenEuler;public Vector3 ClosedEuler;public bool Open=true; }
}

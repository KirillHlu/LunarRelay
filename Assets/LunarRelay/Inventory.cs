using System.Collections.Generic;
using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 const int BackpackCapacity=6;
 readonly List<StationAction> backpack=new List<StationAction>();
 bool showInventory;
 int lastInventoryFrame=-1;
 public void ToggleInventory(){if(!started||Sim.Complete||Sim.Aborted||lastInventoryFrame==Time.frameCount)return;lastInventoryFrame=Time.frameCount;showInventory=!showInventory;showStatus=false;showLog=false;showSources=false;if(showInventory)paused=false;UpdateCursor();}
 public bool StoreHeld(){
  if(!held)return false;
  if(backpack.Count>=BackpackCapacity){Tell(L("Backpack is full. Choose an item to drop."));return false;}
  var item=held;held=null;backpack.Add(item);item.transform.SetParent(transform,true);item.gameObject.SetActive(false);
  Tell(L("Stored in backpack: ")+L(item.Label));return true;
 }
 void Equip(StationAction item){
  if(!item||item.Used)return;
  if(held==item)return;
  bool stored=backpack.Remove(item);
  if(held&&!StoreHeld()){if(stored)backpack.Add(item);return;}
  item.gameObject.SetActive(true);held=item;var rb=item.GetComponent<Rigidbody>();if(rb)rb.isKinematic=true;
  SetHeldColliders(item,false);item.transform.SetParent(Player.View.transform,false);PoseHeldItem(item);
 }
 void DropStored(StationAction item){if(!backpack.Remove(item))return;item.gameObject.SetActive(true);ReleaseItem(item);}
 void ReleaseItem(StationAction item){
  item.transform.SetParent(null,true);item.transform.rotation=Quaternion.Euler(0,Player.transform.eulerAngles.y,0);
  var center=LocalBounds(item.transform).center;
  // A short ray prevents throwing through a nearby wall, including when crouched.
  var eye=Player.View.transform.position;var direction=Player.View.transform.forward;float reach=.65f;
  if(Physics.Raycast(eye,direction,out var hit,reach,~(1<<2),QueryTriggerInteraction.Ignore))reach=Mathf.Max(.10f,hit.distance-.22f);
  item.transform.position=eye+direction*reach-item.transform.TransformVector(center);
  SetHeldColliders(item,true);var rb=item.GetComponent<Rigidbody>();if(rb){rb.isKinematic=false;rb.useGravity=false;rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}
  Tell(L("Item released. Tools and packs fall with lunar gravity."));
 }
 void InventoryPanel(){TabletInventory();}
}
}

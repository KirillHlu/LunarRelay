using UnityEngine;
using UnityEngine.Rendering;
namespace LunarRelay {
public sealed class LeverMotion:MonoBehaviour {
 public Transform Handle;public GameSession Game;public ActionKind Kind;public Transform Grip;
 public bool EnabledState=>Kind==ActionKind.Air?Game.Sim.AirOn:Kind==ActionKind.Thermal?Game.Sim.ThermalOn:Game.Sim.RoverOn;
 public void Set(bool on,bool immediate=false){var target=Quaternion.Euler(on?-55:55,0,0);Handle.localRotation=immediate?target:Quaternion.RotateTowards(Handle.localRotation,target,Time.unscaledDeltaTime*400);}
 void Update(){if(Game&&Handle)Set(EnabledState);}
}
public sealed partial class GameSession {
 void ConfigurePanelLevers(Transform power){
  foreach(var action in actions){if(!action.transform.IsChildOf(power)||!action.name.StartsWith("Switch_")||!action.name.EndsWith("HingePivot"))continue;
   action.transform.localRotation=Quaternion.identity;
   var pivot=new GameObject("Vertical lever hinge").transform;pivot.SetParent(action.transform,false);pivot.gameObject.AddComponent<AnimatedAssembly>();
   foreach(var child in action.GetComponentsInChildren<Transform>())if(child!=action.transform&&child!=pivot&&!child.name.EndsWith("Faceplate")){if(child.parent==action.transform)child.SetParent(pivot,false);}
   var motion=action.gameObject.AddComponent<LeverMotion>();motion.Handle=pivot;motion.Game=this;motion.Kind=action.Kind;motion.Grip=FindContains(pivot,"OrangeGrip");action.Moving=pivot;motion.Set(motion.EnabledState,true);
   foreach(var renderer in action.GetComponentsInChildren<MeshRenderer>()){renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
  }
 }
}
}

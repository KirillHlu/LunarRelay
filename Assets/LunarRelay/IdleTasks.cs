using UnityEngine;
namespace LunarRelay {
public sealed partial class Simulation {
 public double IdleTaskSeconds;
 public bool TaskWaitActive,RoutineAssigned;
 public int RoutineTargetId,RoutineSequence;
}
public sealed partial class GameSession {
 void BeginTaskWait(){Sim.TaskWaitActive=true;Sim.IdleTaskSeconds=0;Sim.RoutineAssigned=false;}
 StationAction RoutineTarget(){if(!Sim.RoutineAssigned||Sim.RoutineTargetId<0||Sim.RoutineTargetId>=actions.Count)return null;return actions[Sim.RoutineTargetId];}
 bool UrgentTask=>Sim.Storm||Sim.CO2KPa>.4||Sim.O2KPa<19.5||Sim.Energy<.5||Sim.BreakerTripped;
 bool WaitingForNextTask=>Sim.Expanded&&Sim.Endless&&Sim.TaskWaitActive&&!UrgentTask;
 void AssignRoutine(){
  var target=Sim.CropDays>=28&&Sim.CropHealth>=.45?ActionFor(ActionKind.Greenhouse):panelService[Sim.RoutineSequence%panelService.Count];
  Sim.RoutineSequence++;Sim.RoutineTargetId=actions.IndexOf(target);Sim.RoutineAssigned=true;
 }
 void UpdateIdleTasks(float seconds){
  if(!Playing||!Sim.Expanded||!Sim.Endless)return;
  if(UrgentTask)return;
  if(!Sim.TaskWaitActive){if(BaseObjective()==null&&!Sim.RoutineAssigned)BeginTaskWait();else return;}
  Sim.IdleTaskSeconds=System.Math.Min(10,Sim.IdleTaskSeconds+seconds);
  if(Sim.IdleTaskSeconds>=10){Sim.TaskWaitActive=false;Sim.IdleTaskSeconds=0;if(BaseObjective()==null)AssignRoutine();}
 }
 string IdleTaskMessage()=>UI("Следующее задание через ","Next task in ")+Mathf.CeilToInt(Mathf.Max(0,10-(float)Sim.IdleTaskSeconds))+UI(" с."," s.");
 public void Use(StationAction action){
  var previous=Objective();bool wasRoutine=previous&&previous==RoutineTarget();double dust=0;int panel=panelService.IndexOf(action),harvests=Sim.Harvests;if(panel>=0)dust=Sim.PanelDust[panel];
  ExecuteUse(action);
  if(!Sim.Expanded||previous!=action||!action)return;
  bool routineDone=action==RoutineTarget()&&((panel>=0&&Sim.SuitEquipped&&Outside&&Carrying(ActionKind.Brush)&&Sim.PanelDust[panel]<=dust)||(action.Kind==ActionKind.Greenhouse&&Sim.Harvests>harvests));
  if(routineDone||(!wasRoutine&&BaseObjective()!=previous)){AwardCompletedTask(action,wasRoutine);if(Sim.Endless)BeginTaskWait();}
 }
}
}

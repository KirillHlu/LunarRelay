using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 float creditNoticeUntil;int creditNoticeAmount;
 void AwardCompletedTask(StationAction action,bool routine){
  if(decorProfile==null)InitDecor();int before=decorProfile.Credits;CheckRewards();if(decorProfile.Credits!=before)return;
  string key=routine?"routine/"+Sim.RoutineSequence:"task/"+actions.IndexOf(action);
  Reward(key,action.Kind==ActionKind.Greenhouse||action.Kind==ActionKind.RepairArray?10:5);
 }
 void CreditHUD(){
  if(decorProfile==null)return;
  if(Application.isMobilePlatform){Text(920,42,180,38,decorProfile.Credits+" LC",heading,Color.white);
   if(Time.unscaledTime<creditNoticeUntil)Text(920,84,180,38,"+"+creditNoticeAmount+" LC",heading,amber);return;}
  var background=new Color(.018f,.038f,.06f,.97f);
  Box(1144,32,264,94,background);Text(1160,43,230,46,decorProfile.Credits+" LC",heading,Color.white);Text(1160,88,230,28,UI("МАГАЗИН · TAB","SHOP · TAB"),micro,cyan);
  if(Time.unscaledTime<creditNoticeUntil){float alpha=Mathf.Clamp01((creditNoticeUntil-Time.unscaledTime)*2);var old=GUI.color;GUI.color=new Color(1,1,1,alpha);Box(1144,138,264,94,background);Text(1160,149,230,46,"+"+creditNoticeAmount+" LC",heading,amber);Text(1160,194,230,28,UI("Задание выполнено","Task completed"),micro,paper);GUI.color=old;}
 }
}
}

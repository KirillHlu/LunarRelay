using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 bool showStatus; string displayedTask="";float taskChanged;
 string UI(string ru,string en)=>useRussian?ru:en;
 void MinimalHUD(){
  string task=NextTask();if(task!=displayedTask){displayedTask=task;taskChanged=Time.unscaledTime;}
  float fade=reducedEffects?1:Mathf.Clamp01((Time.unscaledTime-taskChanged)/.28f);
  float taskHeight=Mathf.Clamp(body.CalcHeight(new GUIContent(task),310)+48,112,176);
  var previous=GUI.color;GUI.color=new Color(1,1,1,fade);
  // One light task surface; the rest of the view is reserved for the world.
  Box(32,32,344,taskHeight,new Color(.065f,.085f,.105f,.74f));
  Text(48,43,310,23,UI("ТЕКУЩАЯ ЗАДАЧА","CURRENT OBJECTIVE"),micro,cyan);
  Text(48,73,310,taskHeight-48,task,body,new Color(.91f,.95f,1,fade));GUI.color=previous;
  ObjectiveMarker();
  if(Application.isMobilePlatform){var aim=GUI.matrix.inverse.MultiplyPoint3x4(new Vector3(Screen.width*.5f,Screen.height*.5f,0));Box(aim.x-2,aim.y-2,4,4,focus?cyan:paper);}else Box(718,448,4,4,focus?cyan:paper);
  if(Application.isMobilePlatform&&focus){Text(470,588,500,36,focus.Label,small,cyan);Text(470,628,500,28,FocusState(),micro,muted);}
  if(!Application.isMobilePlatform&&focus){Box(475,558,490,76,new Color(.065f,.085f,.105f,.8f));Text(490,570,32,32,"E",heading,cyan);Text(536,567,410,40,focus.Label,small);Text(536,608,410,23,FocusState(),micro,muted);}
  if(Time.unscaledTime<toastUntil){Box(455,776,530,76,new Color(.065f,.085f,.105f,.82f));Text(473,787,494,60,toast,small);}
 }
 void StatusReading(float x,float y,string label,string value,float fraction,bool warning=false){
  Text(x,y,280,24,label,micro,muted);Text(x,y+27,280,36,value,heading,warning?amber:paper);
  Box(x,y+72,280,3,surface);Box(x,y+72,280*Mathf.Clamp01(fraction),3,warning?amber:cyan);
 }
 void SystemLine(float x,float y,string label,bool ready){
  Box(x,y+8,5,5,ready?cyan:amber);Text(x+17,y,295,28,label,small,ready?paper:amber);
 }
 void BaseStatusScreen(){
  if(Sim.Expanded){EngineeringScreen();return;}
  Dim();Box(174,74,1092,752,ink);
  TabletTabs(1);
  Text(208,166,900,25,UI("СМЕНА ","SHIFT ")+Sim.Hours.ToString("0.00")+" / 8.00 h · "+lastStage,micro,muted);
  if(Button(1100,100,132,44,"×")){showStatus=false;UpdateCursor();}
  Text(208,207,630,30,UI("РЕСУРСЫ И СРЕДА","RESOURCES & ENVIRONMENT"),small,cyan);
  StatusReading(208,254,L("BATTERY"),Sim.Energy.ToString("0.0")+" / 10 kWh",(float)(Sim.Energy/Sim.Capacity),Sim.Energy<2);
  StatusReading(540,254,L("GENERATION / DEMAND"),Sim.SolarKW.ToString("0.0")+" / "+Sim.LoadKW.ToString("0.0")+" kW",(float)Sim.ServedFraction,Sim.ServedFraction<.95);
  StatusReading(208,367,L("CABIN OXYGEN"),Sim.O2KPa.ToString("0.0")+" kPa",(float)(Sim.O2KPa/21),Sim.O2KPa<19.5);
  StatusReading(540,367,L("CO2 PARTIAL PRESSURE"),Sim.CO2KPa.ToString("0.00")+" kPa",(float)(1-Sim.CO2KPa/.7),Sim.CO2KPa>.45);
  StatusReading(208,480,L("CABIN TEMPERATURE"),Sim.CabinC.ToString("0.0")+" °C",Mathf.InverseLerp(10,35,(float)Sim.CabinC),Sim.CabinC<17||Sim.CabinC>27);
  StatusReading(540,480,UI("ДАВЛЕНИЕ В ШЛЮЗЕ","AIRLOCK PRESSURE"),lockPressure.ToString("0.0")+" kPa",lockPressure/101.3f);
  Text(875,207,340,30,UI("СИСТЕМЫ","SYSTEMS"),small,cyan);
  SystemLine(875,254,UI("Главная шина","Main power bus"),Sim.MainPower);
  SystemLine(875,293,UI("Очистка CO₂","CO2 cleaning"),Sim.MainPower&&Sim.AirOn&&Sim.ScrubberRepaired);
  SystemLine(875,332,UI("Термоконтроль","Thermal control"),Sim.MainPower&&Sim.ThermalOn);
  SystemLine(875,371,UI("Кислородный регулятор","Oxygen regulator"),Sim.O2Valve);
  SystemLine(875,410,UI("Соединение массива","Array connection"),Sim.ArrayRepaired);
  Text(875,459,320,58,UI("Запас скафандра: ","Suit reserve: ")+Sim.SuitHours.ToString("0.0")+" h\n"+UI("Вода в убежище: ","Shelter water: ")+Sim.Packs+" / 3",small);
  Text(875,523,320,68,UI("В убежище: ","Sheltered: ")+Sim.ShelterHours.ToString("0.00")+" / 1.50 h\n"+UI("Учебный индекс риска: ","Training risk index: ")+Sim.Risk.ToString("0"),small);
  Text(208,605,950,27,UI("СВОДКА / ТРЕБУЕТ ВНИМАНИЯ","SUMMARY / ATTENTION"),micro,cyan);
  string warning=!Sim.MainPower?UI("Главная шина отключена. Питание систем отсутствует.","Main bus isolated. Systems are without power."):Sim.Storm?UI("Учебное событие: оставайтесь в подготовленном убежище.","Particle drill: remain in the stocked shelter."):NextTask();
  Text(208,641,990,58,warning,body,amber);
  Text(208,744,650,43,UI("Мониторинг · игровое время приостановлено","Monitoring · simulation paused"),micro,muted);
  if(Button(935,738,297,48,UI("Справка и управление","Guide & controls"))){showStatus=false;showLog=true;UpdateCursor();}
 }
}
}

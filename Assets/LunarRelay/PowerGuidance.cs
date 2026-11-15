using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 StationAction LoadControl(int setting)=>actions.Find(a=>a&&a.GetComponent<HandControl>()&&a.GetComponent<HandControl>().Setting==setting);
 StationAction LowPowerTarget(){
  if(Sim.RoverOn)return Available(ActionKind.Rover);
  if(Sim.SolarKW>Sim.LoadKW+.05&&!Sim.Shadow)return null;
  if(Sim.GreenhouseLevel>0)return LoadControl(5);
  if(System.Math.Abs(Sim.LightingLevel-.5)>.01)return LoadControl(3);
  if(!Sim.Shadow&&Sim.DustEfficiency<.8){if(!Carrying(ActionKind.Brush))return Available(ActionKind.Brush);if(!Sim.SuitEquipped)return Available(ActionKind.SuitCheck);int index=0;for(int i=1;i<Sim.PanelDust.Length;i++)if(Sim.PanelDust[i]>Sim.PanelDust[index])index=i;return panelService[index];}
  return null;
 }
 string LowPowerInstruction(){
  var target=LowPowerTarget();
  if(target){
   if(target.Kind==ActionKind.Rover)return UI("Отключите зарядку ровера у энергомодуля.","Turn off rover charging at the power module.");
   var control=target.GetComponent<HandControl>();if(control)return control.Setting==5?UI("Выключите питание теплицы в жилом модуле. Воздух и отопление оставьте включёнными.","Turn off greenhouse power in the living module. Keep air cleaning and heating on."):UI("Снизьте освещение убежища до 50%.","Reduce shelter lighting to 50%.");
   if(target.Kind==ActionKind.Brush)return UI("Возьмите инструмент очистки из жилого модуля.","Collect the cleaning tool from the living module.");
   if(target.Kind==ActionKind.SuitCheck)return UI("Проверьте скафандр перед выходом на поверхность.","Check the suit before going outside.");
   return UI("Очистите отмеченную солнечную панель от пыли.","Clean dust off the marked solar panel.");
  }
  if(Sim.Shadow)return UI("Расход снижен. Солнце в тени ещё ","Loads reduced. Shadow ends in ")+Mathf.Max(0,(float)(4-Sim.CycleHour)).ToString("0.0")+UI(" игровых ч. Оставайтесь внутри; воздух и отопление не выключайте."," simulated h. Stay inside; keep air cleaning and heating on.");
  if(Sim.SolarKW>Sim.LoadKW)return UI("Батарея заряжается: выработка выше расхода. Дождитесь 2 кВт·ч; затем верните питание теплицы её переключателем.","Battery charging: generation exceeds demand. Wait for 2 kWh, then restore greenhouse power at its switch.");
  return UI("Расход снижен. Оставьте воздух и отопление включёнными и дождитесь восстановления солнечной выработки.","Loads reduced. Keep air cleaning and heating on while solar output recovers.");
 }
}
}

using UnityEngine;
namespace LunarRelay {
public sealed class HandControl:MonoBehaviour {public int Setting;}
public sealed partial class GameSession {
 void AddHandControl(Vector3 p,string en,int setting,bool faceRoom){
  var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=en;g.transform.position=p;g.transform.localScale=new Vector3(.17f,.12f,.07f);
  var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.16f,.34f,.44f);mat.SetFloat("_Smoothness",.2f);g.GetComponent<Renderer>().sharedMaterial=mat;
  var housing=GameObject.CreatePrimitive(PrimitiveType.Cube);housing.name=en+" mounting enclosure";housing.transform.position=p+new Vector3(0,0,faceRoom?.13f:-.13f);housing.transform.localScale=new Vector3(.23f,.18f,.28f);var shell=new Material(mat);shell.color=new Color(.035f,.07f,.10f);housing.GetComponent<Renderer>().sharedMaterial=shell;
  var indicator=GameObject.CreatePrimitive(PrimitiveType.Cube);indicator.name="Switch index";indicator.transform.SetParent(g.transform,false);indicator.transform.localPosition=new Vector3(0,0,faceRoom?-.52f:.52f);indicator.transform.localScale=new Vector3(.45f,.09f,.05f);Destroy(indicator.GetComponent<Collider>());var lightMat=new Material(mat);lightMat.color=new Color(.65f,.78f,.88f);indicator.GetComponent<Renderer>().sharedMaterial=lightMat;
  RoundedForms.Soften(g,.20f);RoundedForms.Soften(housing,.18f);RoundedForms.Soften(indicator,.12f);
  Act(g.transform,ActionKind.Info,en);g.AddComponent<HandControl>().Setting=setting;
  var caption=new GameObject("Switch plate").AddComponent<TextMesh>();caption.transform.SetParent(g.transform,false);caption.transform.localPosition=new Vector3(0,1.1f,faceRoom?-.65f:.65f);caption.transform.localScale=new Vector3(1/.17f,1/.12f,1/.07f);caption.transform.rotation=Quaternion.Euler(0,faceRoom?0:180,0);caption.text=en;caption.font=Resources.Load<Font>("Fonts/PT_Sans-Web-Regular");caption.fontSize=48;caption.characterSize=.002f;caption.anchor=TextAnchor.MiddleCenter;caption.color=new Color(.65f,.78f,.88f);caption.GetComponent<Renderer>().sharedMaterial=caption.font.material;
 }
 void BuildHandControls(){
  AddHandControl(new Vector3(-7.85f,1.6f,1.12f),"Greenhouse power",5,true);
  AddHandControl(new Vector3(-7.2f,1.6f,1.12f),"Meal provision",9,true);
  AddHandControl(new Vector3(-2.9f,1.3f,.72f),"Shelter power",7,true);
  AddHandControl(new Vector3(-2.45f,1.3f,.72f),"Cabin lights",3,true);
  AddHandControl(new Vector3(8,1.35f,5.85f),"Power priority",8,false);
 }
 void CycleLocalLoad(int index){double current=LoadSetting(index);SetLoad(index,current<.01?.5:current<.99?1:0);Tell(UI("Режим: ","Setting: ")+(LoadSetting(index)*100).ToString("0")+UI("%. E — следующий режим.","%. E cycles settings."));}
 void HandsDiagnosis(){
  if(!Sim.Expanded)return;
  for(int i=0;i<4;i++)RunDiagnostic(i);
  if((!Sim.FanOn||Sim.SensorFault)&&(!Sim.AirOn||!Sim.MainPower)&&Carrying(ActionKind.Tool)&&Sim.AirflowChecked&&Sim.SensorChecked){Sim.FanOn=true;Sim.SensorFault=false;Tell(UI("Вентилятор и датчик обслужены. Включите очиститель.","Fan and sensor serviced. Restore air-cleaner power."));return;}
  if(!Sim.ScrubberRepaired&&airCartridgeReady){Use(cartridgeService);return;}
  Tell(!Sim.ScrubberRepaired?UI("Фильтр изношен. Отключите очиститель, установите картридж в разъём и закрепите инструментом.","Worn filter. Switch off the cleaner, fit the cartridge in its socket, then secure it with the driver."):!Sim.FanOn||Sim.SensorFault?UI("Нужен ремонт. Отключите очиститель, возьмите инструмент и нажмите E на этом экране.","Service needed. Isolate the cleaner, bring the driver and press E on this screen."):UI("Воздух проверен. CO₂: ","Air checked. CO2: ")+Sim.CO2KPa.ToString("0.00")+" kPa");
 }
 bool HandsUse(StationAction action){
  var control=action.GetComponent<HandControl>();
  if(control){int index=control.Setting;if(index==8){Sim.PriorityPower=!Sim.PriorityPower;Tell(Sim.PriorityPower?UI("Приоритет: жизнеобеспечение.","Life support has priority."):UI("Питание распределяется поровну.","Power shared proportionally."));}else if(index==9){Sim.MealLevel=Sim.MealLevel>=1?.5:1;Tell(UI("Выдача еды: ","Meal provision: ")+(Sim.MealLevel*100)+"%");}else CycleLocalLoad(index);return true;}
  if(action.Kind==ActionKind.InstallPack&&action.Used&&Sim.Expanded){int slot=actions.FindAll(a=>a.Kind==ActionKind.InstallPack).IndexOf(action);if(Sim.ShieldLitres[slot]>0){Sim.RecoverShield(slot);Tell(UI("Вода возвращена в резерв. E — заполнить блок снова.","Water recovered. E refills this block."));}else Tell(Sim.FillShield(slot)?UI("Защитный блок заполнен.","Shield block refilled."):UI("Нужно 4 л воды.","Requires 4 L of clean water."));return true;}
  if(action.Kind==ActionKind.Main&&Sim.Expanded&&Sim.BreakerTripped&&!Sim.MainPower){if(!Carrying(ActionKind.Tool)){Tell(UI("Нужен шуруповёрт для сброса автомата.","Bring the driver to reset the breaker."));return true;}Sim.BusChecked=true;Sim.BreakerTripped=false;Sim.OverloadHours=0;Tell(UI("Автомат восстановлен. E — включить шину.","Breaker reset. E restores main power."));return true;}
  return false;
 }
 string HandsPrompt(StationAction a){
  var c=a.GetComponent<HandControl>();if(c)return c.Setting<8?UI("E — режим: ","E — setting: ")+(LoadSetting(c.Setting)*100).ToString("0")+"% → "+(LoadSetting(c.Setting)<.01?"50%":LoadSetting(c.Setting)<.99?"100%":"0%"):UI("E — переключить режим","E — switch mode");
  switch(a.Kind){
   case ActionKind.Diagnose:return UI("E — проверить / обслужить инструментом","E — inspect / service with driver");
   case ActionKind.Greenhouse:return Sim.CropDays>=28&&Sim.CropHealth>=.45?UI("E — собрать урожай","E — harvest"):UI("E — проверить растения","E — inspect plants");
   case ActionKind.WaterRecycler:case ActionKind.OxygenGenerator:return UI("E — сменить режим: выкл. / 50% / 100%","E — cycle off / 50% / 100%");
   case ActionKind.InstallPack:if(a.Used)return UI("E — вернуть / заполнить воду","E — recover / refill water");break;
  }return null;
 }
}
}

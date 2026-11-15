using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LunarRelay {
[Serializable] public sealed class SavedAction {public int Id;public bool Used,Open,Active,Kinematic;public Vector3 Position;public Quaternion Rotation;}
[Serializable] public sealed class MissionSave {
 public int Version=1,Held=-1;public Simulation State;public List<SavedAction> Actions=new List<SavedAction>();public List<int> Backpack=new List<int>();
 public Vector3 PlayerPosition;public Quaternion PlayerRotation;public float Pressure,Cycle,Elapsed;public bool CycleToCabin,Cartridge;public string[] History;
}
public sealed partial class GameSession {
 string seedText="1";
 string saveOverride;
 string SavePath=>saveOverride??Path.Combine(Application.persistentDataPath,"lunar-relay-mission-v1.json");
 bool IsPortable(StationAction item)=>item.Kind==ActionKind.Tool||item.Kind==ActionKind.Cartridge||item.Kind==ActionKind.Pack||item.Kind==ActionKind.Battery||item.Kind==ActionKind.Brush;
 void PersistenceShortcuts(){var keyboard=Keyboard.current;if(keyboard==null||!started||!Sim.Expanded)return;if(keyboard.f5Key.wasPressedThisFrame)SaveMission();if(keyboard.f9Key.wasPressedThisFrame)LoadMission();}
 void SaveMission(){
  if(!Sim.Expanded||!started||Sim.Complete||Sim.Aborted){Tell(UI("Сохранение доступно во время активной миссии.","Save is available during an active mission."));return;}
  var save=new MissionSave{State=Sim,Held=held?actions.IndexOf(held):-1,PlayerPosition=Player.transform.position,PlayerRotation=Player.transform.rotation,Pressure=lockPressure,Cycle=cycleRemaining,CycleToCabin=cycleToCabin,Cartridge=airCartridgeReady,Elapsed=wallTime,History=history.ToArray()};
  for(int i=0;i<actions.Count;i++){var a=actions[i];var rb=a.GetComponent<Rigidbody>();save.Actions.Add(new SavedAction{Id=i,Used=a.Used,Open=a.Open,Active=a.gameObject.activeSelf,Kinematic=!rb||rb.isKinematic,Position=a.transform.position,Rotation=a.transform.rotation});}
  foreach(var item in backpack)save.Backpack.Add(actions.IndexOf(item));
  try{Directory.CreateDirectory(Application.persistentDataPath);string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(save,true));if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);Tell(UI("Миссия сохранена.","Mission saved."));}catch(Exception e){Debug.LogWarning(e.Message);Tell(UI("Не удалось сохранить миссию.","Could not save mission."));}
 }
 bool LoadMission(){
  try{
   if(!File.Exists(SavePath)){Tell(UI("Сохранение пока отсутствует.","No saved mission yet."));return false;}
   var save=JsonUtility.FromJson<MissionSave>(File.ReadAllText(SavePath));
   if(save==null||save.Version!=1||save.State==null||!save.State.Expanded||save.Actions.Count>actions.Count||save.Backpack.Count>6||double.IsNaN(save.State.Hours))throw new Exception("Unsupported mission save");
   if(save.Held>=actions.Count||save.Backpack.Exists(i=>i<0||i>=actions.Count))throw new Exception("Invalid saved item");
   held=null;backpack.Clear();Sim=save.State;if(string.IsNullOrEmpty(Sim.RewardRunId))Sim.RewardRunId="legacy-save-"+Sim.ScenarioSeed;PlayerPrefs.SetInt("LunarRelayEndless",Sim.Endless?1:0);
   foreach(var entry in save.Actions){if(entry.Id<0||entry.Id>=actions.Count)continue;var a=actions[entry.Id];a.Used=entry.Used;a.Open=entry.Open;
    if(a.Moving&&a.Kind.ToString().StartsWith("Door"))a.Moving.localRotation=Quaternion.Euler(a.Open?a.OpenEuler:a.ClosedEuler);
    if(IsPortable(a)){a.transform.SetParent(null,true);a.transform.SetPositionAndRotation(entry.Position,entry.Rotation);a.gameObject.SetActive(entry.Active);var rb=a.GetComponent<Rigidbody>();if(rb){rb.isKinematic=entry.Kinematic;if(!entry.Kinematic){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}}SetHeldColliders(a,!entry.Used);}
   }
   foreach(int id in save.Backpack){var item=actions[id];backpack.Add(item);item.transform.SetParent(transform,true);item.gameObject.SetActive(false);}
   if(save.Held>=0)Equip(actions[save.Held]);
   airCartridgeReady=save.Cartridge;foreach(var renderer in cartridgeSocket.GetComponentsInChildren<MeshRenderer>())renderer.enabled=!save.Cartridge;
   var battery=actions.Find(a=>a.Kind==ActionKind.ReplaceBattery);if(battery)foreach(var renderer in battery.GetComponentsInChildren<MeshRenderer>())renderer.enabled=!battery.Used;
   lockPressure=save.Pressure;cycleRemaining=save.Cycle;cycleToCabin=save.CycleToCabin;wallTime=save.Elapsed;
   history.Clear();if(save.History!=null)history.AddRange(save.History);
   RestoreFirstPerson();Player.Place(save.PlayerPosition,save.PlayerRotation.eulerAngles.y);started=true;paused=true;showStatus=showInventory=showLog=showSources=false;UpdateCursor();Physics.SyncTransforms();Tell(UI("Миссия восстановлена на паузе.","Mission restored and paused."));return true;
  }catch(Exception e){Debug.LogWarning("Load mission: "+e.Message);Tell(UI("Не удалось восстановить сохранение этой версии.","Could not restore this version of the saved mission."));return false;}
 }
 void SeedControls(){
  Text(61,537,145,30,UI("Зерно миссии","Mission seed"),small,cyan);
  seedText=GUI.TextField(new Rect(220,535,155,34),seedText,9);
  if(Button(390,535,160,34,UI("Применить","Apply"))&&int.TryParse(seedText,out int selected)){
   selected=Mathf.Clamp(selected,0,100000000);seed=selected;Sim=new Simulation(seed);Sim.EnableExpansion(seed);Sim.Endless=PlayerPrefs.GetInt("LunarRelayEndless",1)==1;PlayerPrefs.SetInt("LunarRelaySeed",selected-1);Tell(UI("Сценарий выбран.","Scenario selected."));
  }
 }
 void ExpandedHelp(){
  Dim();Box(174,74,1092,752,ink);TabletTabs(2);
  string help=UI(
   "УПРАВЛЕНИЕ\nWASD — движение; мышь — обзор; E — предмет/оборудование; R — убрать в рюкзак; I — рюкзак; Q — положить; Ctrl/C — присесть; F — фонарь; Tab — мониторинг; Esc — пауза; F5/F9 — сохранить/восстановить.\n\nИНЖЕНЕРНАЯ СМЕНА\nСначала измеряйте. Сопоставляйте питание, поток воздуха, фильтр и независимый датчик. Подсказки в диагностике необязательны. Отключайте систему перед ремонтом инструментом. Картридж устанавливается по E из рук или рюкзака.\n\nВЫХОД НА ПОВЕРХНОСТЬ\nПроверьте скафандр на приборе шлюза. Закройте внутреннюю дверь, сбросьте давление, затем откройте внешнюю. Следите за O₂ и батареей. Для панелей нужен инструмент очистки из жилого модуля. На обратном пути закройте внешнюю дверь и заполните шлюз.\n\nВОДА, РАСТЕНИЯ И УБЕЖИЩЕ\nДля заполнения каждого защитного блока нужно 4 л воды. Нажмите E на установленном блоке, чтобы вернуть воду; защита уменьшится. Урожай собирается рядом с теплицей. Растения используют отдельные ускоренные часы. В конце смены нужны оба ремонта, 3 установленных блока, не меньше 1 кВт·ч и 0,1 кг еды. Учебное событие ожидается в часы 5–7; проведите в убежище хотя бы 1,5 часа.\n\nНАУКА И УПРОЩЕНИЯ\nОчистка CO₂ не производит O₂. Электролиз расходует воду и электричество, образует кислород и водород. Восстановление воды неполное. Небольшой огород — дополнительный запас еды. Пыль не переносится лунным ветром: в сценарии она связана с деятельностью у поверхности. Радиация невидима; показатель риска безразмерный, не мЗв. Мощности, урожайность и скорости — учебные параметры, а не спецификации NASA.",
   "CONTROLS\nWASD move; mouse look; E interact; R store; I inventory; Q release; Ctrl/C crouch; F lamp; Tab monitoring; Esc pause; F5/F9 save/restore.\n\nENGINEERING SHIFT\nMeasure first. Compare power, airflow, cartridge resistance and the independent sensor. Diagnostic hints are optional. Isolate equipment before service with the driver. E seats the replacement cartridge directly from hand or backpack.\n\nSURFACE OPERATIONS\nCheck and equip the suit at the airlock gauge. Close the inner hatch, vent, then open the outer hatch. Monitor suit O2 and battery. Bring the cleaning tool from the sleeping pod to service panels. On return, close the outer hatch, fill, then open the inner hatch.\n\nWATER, PLANTS AND SHELTER\nEach shielding block takes 4 L of water from the shared reserve. E on the installed block recovers water, reducing shielding mass. Harvest near the greenhouse. Plants use an additional accelerated development clock. Finish with both repairs, 3 placed blocks, at least 1 kWh and 0.1 kg of food. The particle drill runs during hours 5–7: spend at least 1.5 h in the shelter.\n\nSCIENCE AND ASSUMPTIONS\nCO2 removal does not make oxygen. Electrolysis uses water and electricity, producing oxygen and hydrogen. Water recovery is incomplete. A small garden supplements food reserves. Lunar dust is tied to surface activity, not wind. Radiation is invisible; the training risk index is dimensionless, not mSv. Power ratings, crop yields and time scales are game assumptions, not NASA specifications.");
  if(Sim.Endless)help=UI("БЕСКОНЕЧНЫЙ РЕЖИМ\nФинала по таймеру нет. Каждый цикл длится 8 игровых часов: тень в часы 2–4, тревога в 5–7, затем новый цикл. В начале цикла снабжение пополняет воду, еду, O₂ и фильтрующий материал обслуженного очистителя. Энергия, ремонт и настройки сохраняются. Учебный риск обнуляется для новой тревоги. Опасные состояния ресурсов по-прежнему заканчивают игру.\n\n", "ENDLESS MODE\nNo time-limit ending. Each cycle lasts 8 simulated hours: shadow at 2–4, drill at 5–7, then another cycle. At cycle start, scheduled supplies replenish water, food, stored O2 and serviced-filter media. Energy, repairs and settings persist. Training risk resets for the next drill. Unsafe resource states can still end the run.\n\n")+help;
  help=UI("ПОДОЙДИТЕ → НАВЕДИТЕ ПРИЦЕЛ → НАЖМИТЕ E\nЭкран очистителя проверяет воздух. Для ремонта отключите очиститель и принесите инструмент. Генератор O₂ и переработчик воды переключаются по E. На теплице E собирает зрелый урожай. Отдельные переключатели управляют светом, теплицей, убежищем, выдачей еды и приоритетом питания. Установленные водяные блоки можно опустошать и заполнять по E. Tab — только обзор.\n\n", "APPROACH → AIM → PRESS E\nThe air-cleaner screen checks air. Isolate the cleaner and bring the driver for service. E cycles oxygen and water processors. E on the greenhouse harvests mature plants. Physical switches control lights, greenhouse, shelter, meals and power priority. E on installed water blocks recovers or refills water. Tab is an overview only.\n\n")+help;
  help=MobileText(help);float height=body.CalcHeight(new GUIContent(help),960);scroll=TouchScrollView(new Rect(208,185,1004,536),scroll,new Rect(0,0,970,height));GUI.Label(new Rect(0,0,960,height),help,body);EndTouchScrollView();
  if(Button(208,750,490,46,UI("Вернуться к смене","Back to shift"))){showLog=false;UpdateCursor();}if(Button(722,750,490,46,UI("Источники NASA","NASA sources"))){showLog=false;showSources=true;}
 }
 void ScientificDebrief(){
  Dim();Box(174,74,1092,752,ink);
  int score=Mathf.Clamp(100-(int)(Sim.Risk*.6)-Sim.UnsafeAttempts*2-(int)Math.Max(0,2-Sim.Energy)*10-(int)(Sim.WaterShortageHours*10)+Sim.Harvests*3,0,100);
  Text(208,103,990,44,Sim.Complete?UI("СМЕНА ЗАВЕРШЕНА","SHIFT COMPLETE"):UI("РАЗБОР СМЕНЫ","SHIFT REVIEW"),heading,Sim.Complete?cyan:amber);
  Text(208,160,990,65,L(Sim.Outcome),body);
  Text(208,241,990,85,UI("Готовность: ","Readiness: ")+score+" / 100 · "+UI("Время игры: ","Play time: ")+(wallTime/60).ToString("0.0")+" min · SEED "+Sim.ScenarioSeed+"\n"+UI("Энергия: ","Energy: ")+Sim.Energy.ToString("0.1")+" kWh · "+UI("Вода: ","Water: ")+Sim.CleanWater.ToString("0.0")+" L · O₂ "+Sim.O2KPa.ToString("0.0")+" kPa · CO₂ "+Sim.CO2KPa.ToString("0.00")+" kPa\n"+UI("Убежище: ","Shelter time: ")+Sim.ShelterHours.ToString("0.00")+" h · "+UI("События: ","Incidents: ")+Sim.EmergencyCount+" · "+UI("Урожаи: ","Harvests: ")+Sim.Harvests,body);
  string[] graphNames={UI("Батарея · 0–10 кВт·ч","Battery · 0–10 kWh"),UI("Чистая вода · 0–24 л","Clean water · 0–24 L"),UI("CO₂ · 0–0,7 кПа","CO2 · 0–0.7 kPa")};
  for(int i=0;i<3;i++){Text(208+i*338,350,320,25,graphNames[i],micro,cyan);DrawHistory(new Rect(208+i*338,388,320,100),i);}
  string causes=Sim.RoverSavedKWh>0?UI("Отключение ровера уменьшило запрос энергии на ","Pausing rover charging avoided ")+Sim.RoverSavedKWh.ToString("0.0")+UI(" кВт·ч."," kWh of demand."):UI("Зарядка ровера оставалась нагрузкой на энергосистему.","Rover charging remained an electrical load.");
  if(Sim.HeatingOffHours>0)causes+="\n"+UI("Отопление было отключено ","Heating was off for ")+Sim.HeatingOffHours.ToString("0.0")+UI(" ч; посмотрите график ресурсов и итоговую температуру "," h; final cabin temperature is ")+Sim.CabinC.ToString("0.0")+" °C.";
  causes+="\n"+UI("Переработчик вернул ","Water recovery returned ")+Sim.WaterProcessed.ToString("0.0")+UI(" л; электролиз использовал "," L; electrolysis used ")+Sim.ElectrolysisWater.ToString("0.00")+" L.";
  Text(208,521,990,133,causes,body);
  Text(208,662,990,57,UI("NASA: воздух, кислород и вода — разные связанные системы. Использование подсказок и корректных альтернатив не штрафуется. Радиационный риск — учебный индекс.","NASA: air cleaning, oxygen generation and water recovery are distinct linked systems. Hints and valid alternative strategies carry no penalty. Radiation risk is a teaching index."),small,muted);
  if(Button(208,751,490,48,UI("Новая смена","Another shift")))Restart();if(Button(722,751,490,48,UI("Научные источники","Science sources")))showSources=true;
 }
}
}

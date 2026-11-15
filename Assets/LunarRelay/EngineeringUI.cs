using System;
using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 Vector2 incidentScroll;
 void EngineeringScreen(){
  Dim();Box(174,74,1092,752,ink);TabletTabs(1);
  Text(208,166,970,28,UI("СМЕНА ","SHIFT ")+Sim.Hours.ToString("0.00")+(Sim.Endless?UI(" ч · ЦИКЛ "," h · CYCLE ")+Sim.CycleNumber+" ("+Sim.CycleHour.ToString("0.0")+" / 8 h)":" / 8 h")+"  ·  SEED "+Sim.ScenarioSeed+"  ·  "+UI("ПАУЗА","PAUSED"),micro,muted);
  if(Button(1140,103,92,44,"×")){showStatus=false;UpdateCursor();}
  if(Button(1000,210,210,42,shopPage?UI("Параметры базы","Base status"):"Shop"))shopPage=!shopPage;
  if(shopPage){ShopScreen();return;}
  Text(208,213,770,44,UI("ОБЗОР БАЗЫ · Управляйте оборудованием на месте по E","BASE OVERVIEW · Operate equipment in the world with E"),heading,cyan);
  EngineeringOverview();
 }
 void EngineeringOverview(){
  StatusReading(208,277,L("BATTERY"),Sim.Energy.ToString("0.0")+" / "+Sim.Capacity.ToString("0")+" kWh",(float)(Sim.Energy/Sim.Capacity),Sim.Energy<2);
  StatusReading(540,277,L("GENERATION / DEMAND"),Sim.SolarKW.ToString("0.0")+" / "+Sim.LoadKW.ToString("0.0")+" kW",(float)Sim.ServedFraction,Sim.ServedFraction<.95);
  StatusReading(208,383,L("CABIN OXYGEN"),Sim.O2KPa.ToString("0.0")+" kPa",(float)(Sim.O2KPa/21),Sim.O2KPa<19.5);
  StatusReading(540,383,L("CO2 PARTIAL PRESSURE"),Sim.MeasuredCO2.ToString("0.00")+" kPa",(float)(1-Sim.MeasuredCO2/.7),Sim.MeasuredCO2>.4);
  StatusReading(208,489,L("CABIN TEMPERATURE"),Sim.CabinC.ToString("0.0")+" °C",Mathf.InverseLerp(10,35,(float)Sim.CabinC),Sim.CabinC<17||Sim.CabinC>27);
  StatusReading(540,489,UI("ЧИСТАЯ ВОДА","AVAILABLE CLEAN WATER"),Sim.CleanWater.ToString("0.0")+" L",(float)(Sim.CleanWater/24),Sim.CleanWater<4);
  Text(875,277,330,160,UI("Шлюз: ","Airlock: ")+lockPressure.ToString("0")+" kPa\n"+UI("Скафандр: ","Suit: ")+(Sim.SuitEquipped?UI("проверен","checked"):UI("не проверен","not checked"))+"\nO₂: "+Sim.SuitHours.ToString("0.0")+" h / "+(Sim.SuitHours*.035).ToString("0.00")+" kg\n"+UI("Батарея EVA: ","EVA battery: ")+Sim.SuitBattery.ToString("0.00")+" kWh",body);
  Text(875,451,330,130,UI("В убежище: ","Sheltered: ")+Sim.ShelterHours.ToString("0.00")+" / 1.50 h\n"+UI("Учебный риск: ","Training risk: ")+(Sim.ShelterReserve>.001?Sim.Risk.ToString("0"):UI("нет питания прибора","monitor unpowered"))+"\n"+UI("Водяные блоки: ","Water blocks: ")+Sim.Packs+" / 3\n"+UI("Резерв убежища: ","Shelter reserve: ")+Sim.ShelterReserve.ToString("0.00")+" kWh",body);
  Text(208,606,990,61,InvestigationGoal(),body,amber);
  DrawHistory(new Rect(208,689,615,80),0);
  if(Button(875,689,337,48,UI("Дополнительная подсказка","Optional hint"))){Sim.HintLevel=Math.Min(3,Sim.HintLevel+1);engineeringPage=3;}
  Text(875,746,337,38,UI("Подсказки не снижают оценку.","Hints do not reduce your score."),micro,muted);
 }
 double LoadSetting(int index){switch(index){case 0:return Sim.AirOn?1:0;case 1:return Sim.OxygenLevel;case 2:return Sim.ThermalOn?1:0;case 3:return Sim.LightingLevel;case 4:return Sim.WaterLevel;case 5:return Sim.GreenhouseLevel;case 6:return Sim.RoverOn?1:0;default:return Sim.ShelterLevel;}}
 void SetLoad(int index,double level){switch(index){case 0:Sim.AirOn=level>0;AnimateSwitch(ActionFor(ActionKind.Air),Sim.AirOn);break;case 1:Sim.OxygenLevel=level;break;case 2:Sim.ThermalOn=level>0;break;case 3:Sim.LightingLevel=level;break;case 4:Sim.WaterLevel=level;break;case 5:Sim.GreenhouseLevel=level;break;case 6:Sim.RoverOn=level>0;break;case 7:Sim.ShelterLevel=level;break;}string[] names={"Air cleaning","Oxygen generation","Heating","Lighting","Water recovery","Greenhouse","Rover charging","Shelter equipment"};Sim.Incident(names[index]+" set to "+(int)(level*100)+"%. Work follows delivered power; lower settings preserve the battery.");Beep();}
 StationAction ActionFor(ActionKind kind)=>actions.Find(a=>a&&a.Kind==kind);
 void PowerControls(){
  Text(208,271,1000,32,UI("Производство / запрос: ","Generation / requested: ")+Sim.SolarKW.ToString("0.0")+" / "+Sim.LoadKW.ToString("0.0")+" kW · "+UI("доставлено ","served ")+(Sim.ServedFraction*100).ToString("0")+"%",heading);
  string[] loads={UI("Очиститель воздуха","Air cleaner"),UI("Производство O₂","Oxygen generation"),UI("Отопление","Heating"),UI("Освещение","Lighting"),UI("Переработка воды","Water recovery"),UI("Теплица","Greenhouse"),UI("Зарядка ровера","Rover charging"),UI("Оборудование убежища","Shelter equipment")};
  for(int i=0;i<8;i++){
   float y=320+i*43;double setting=LoadSetting(i);Text(208,y+8,295,30,loads[i],small);Text(515,y+8,200,30,(setting*100).ToString("0")+"% / "+Sim.Delivered[i].ToString("0.00")+" kW",small,cyan);
   for(int j=0;j<3;j++){if(j==1&&(i==0||i==2||i==6))continue;double value=j*.5;if(Button(745+j*150,y,138,37,j==0?UI("Выкл.","Off"):j==1?UI("50%","50%"):UI("100%","100%")))SetLoad(i,value);if(Math.Abs(setting-value)<.01)Box(759+j*150,y+34,110,3,cyan);}
  }
  if(Button(208,685,490,44,Sim.PriorityPower?UI("Приоритет: жизнеобеспечение","Priority: life support first"):UI("Приоритет: поровну","Priority: proportional allocation"))){Sim.PriorityPower=!Sim.PriorityPower;Sim.Incident("Power allocation policy changed.");}
  Text(745,682,467,62,UI("Лимит шины 4,4 кВт. Длительная перегрузка отключает автомат. Эти мощности — учебные параметры.","Bus limit: 4.4 kW. Sustained overload trips the breaker. These ratings are game assumptions."),small,muted);
  Text(208,749,1000,42,UI("Пыль: ","Panel efficiency: ")+(Sim.DustEfficiency*100).ToString("0")+"% · "+UI("Затенение: ","Shadow: ")+(Sim.Shadow?UI("да","yes"):UI("нет","no")),small,cyan);
 }
 void BiologyControls(){
  Text(208,273,480,34,UI("ВОДНЫЙ КОНТУР","WATER LOOP"),heading,cyan);
  Text(208,320,480,180,UI("Доступно: ","Available: ")+Sim.CleanWater.ToString("0.0")+" L\n"+UI("Использовано: ","Used-water tank: ")+Sim.UsedWater.ToString("0.0")+" L\n"+UI("Восстановлено: ","Recovered: ")+Sim.WaterProcessed.ToString("0.0")+" L\n"+UI("Электролиз: ","Used in electrolysis: ")+Sim.ElectrolysisWater.ToString("0.00")+" L\n"+UI("Запас O₂: ","Stored O2: ")+Sim.StoredO2.ToString("0.00")+" kg",body);
  for(int i=0;i<3;i++){int slot=i;float y=526+i*51;Text(208,y+8,230,30,UI("Блок ","Block ")+(i+1)+": "+Sim.ShieldLitres[i].ToString("0.0")+" L",small);GUI.enabled=Sim.ShieldLitres[i]>0||(Sim.Packs>i&&Sim.CleanWater>=4);if(Button(460,y,235,42,Sim.ShieldLitres[i]>0?UI("Вернуть в резерв","Recover reserve"):UI("Заполнить · 4 л","Refill · 4 L"))){if(Sim.ShieldLitres[slot]>0)Sim.RecoverShield(slot);else Sim.FillShield(slot);}GUI.enabled=true;}
  Text(745,273,467,34,UI("ТЕПЛИЦА","GREENHOUSE"),heading,cyan);
  Text(745,320,467,155,UI("Рост: ","Crop age: ")+Sim.CropDays.ToString("0.0")+" / 28 days\n"+UI("Состояние: ","Health: ")+(Sim.CropHealth*100).ToString("0")+"%\n"+UI("Влажность корней: ","Root moisture: ")+(Sim.CropMoisture*100).ToString("0")+"%\n"+UI("Еда: ","Food reserve: ")+Sim.FoodKg.ToString("0.00")+" kg",body);
  GUI.enabled=Sim.CropDays>=28&&Sim.CropHealth>=.45&&Vector3.Distance(Player.transform.position,farm.position)<2.8f;
  if(Button(745,508,467,48,UI("Собрать урожай у теплицы","Harvest at the greenhouse"))){if(Sim.Harvest()){Tell(UI("Урожай собран.","Harvest collected."));Beep();}}GUI.enabled=true;
  if(Button(745,570,467,44,Sim.MealLevel>=1?UI("Питание: обычное","Meals: normal provision"):UI("Питание: резервный режим","Meals: reserve provision"))){Sim.MealLevel=Sim.MealLevel>=1?.5:1;Sim.Incident("Meal provisioning changed. This affects the finite food reserve, not oxygen metabolism.");}
  Text(745,630,467,75,UI("Рост растений дополнительно ускорен в 72 раза. Полив и свет зависят от питания. В конце смены сохраните хотя бы 0,1 кг еды.","Plant development uses a separate 72× clock. Light and watering need delivered power. Preserve at least 0.1 kg of food at shift end."),small,muted);
  Text(208,716,990,72,UI("Вода в защитных блоках входит в общий запас, но недоступна для потребления до возврата. Электролиз даёт O₂ и H₂; водород отводится в закрытую внешнюю линию.","Shield-block water is part of total stock, but unavailable for drinking until recovered. Electrolysis produces O2 and H2; hydrogen uses a sealed external vent line."),small,muted);
 }
 void RunDiagnostic(int test){
  if(test==0){Sim.FilterChecked=true;diagnosticResult=Sim.ScrubberRepaired?UI("Сопротивление картриджа нормальное.","Cartridge flow resistance is normal."):UI("Сопротивление картриджа высокое. Проверьте сервисный разъём.","Cartridge flow resistance is high. Inspect the service socket.");}
  if(test==1){Sim.AirflowChecked=true;diagnosticResult=UI("Подача питания: ","Delivered power: ")+Sim.Delivered[0].ToString("0.00")+" kW · "+UI("Поток воздуха: ","Airflow: ")+(Sim.FanOn&&Sim.Delivered[0]>.05?UI("есть","present"):UI("отсутствует","absent"));}
  if(test==2){Sim.SensorChecked=true;diagnosticResult=UI("Панель: ","Panel: ")+Sim.MeasuredCO2.ToString("0.00")+" kPa · "+UI("Независимый прибор: ","Reference: ")+Sim.CO2KPa.ToString("0.00")+" kPa";}
  if(test==3){Sim.BusChecked=true;diagnosticResult=UI("Питание шины: ","Bus: ")+(Sim.MainPower&&!Sim.BreakerTripped?UI("есть","online"):UI("отключено","offline"))+" · "+UI("Запрос ","Demand ")+Sim.LoadKW.ToString("0.0")+" / 4.4 kW · "+UI("соединение массива ","array connection ")+(Sim.ArrayRepaired?"OK":UI("высокое сопротивление","high resistance"));}
  Sim.Incident("Diagnostic test "+test+": "+diagnosticResult);Beep();
 }
 void DiagnosticControls(){
  string[] tests={UI("Проверить фильтр","Test cartridge"),UI("Проверить поток","Test airflow"),UI("Сравнить датчики","Compare sensors"),UI("Проверить шину","Check power bus")};
  for(int i=0;i<4;i++)if(Button(208+i*254,277,240,46,tests[i]))RunDiagnostic(i);
  Text(208,343,990,92,diagnosticResult==""?UI("Начните с измерения. Сопоставьте симптомы с питанием и состоянием оборудования.","Start with a measurement. Compare symptoms with delivered power and equipment state."):diagnosticResult,heading);
  bool near=Vector3.Distance(Player.transform.position,cartridgeService.transform.position)<2.8f;
  bool isolated=!Sim.AirOn||!Sim.MainPower;
  GUI.enabled=near&&Carrying(ActionKind.Tool)&&isolated&&Sim.AirflowChecked;
  if(Button(208,461,490,48,UI("Восстановить вентилятор","Reset circulation fan"))){Sim.FanOn=true;diagnosticResult=UI("Вентилятор обслужен. Включите питание и повторите измерение потока.","Fan serviced. Restore power and repeat the airflow measurement.");Sim.Incident("Circulation restored with cleaner isolated and driver present.");Beep();}GUI.enabled=true;
  GUI.enabled=near&&Carrying(ActionKind.Tool)&&isolated&&Sim.SensorChecked;
  if(Button(722,461,490,48,UI("Калибровать датчик","Calibrate CO2 sensor"))){Sim.SensorFault=false;diagnosticResult=UI("Калибровка выполнена. Повторите сравнение приборов.","Calibration completed. Compare instruments again to verify.");Sim.Incident("Sensor calibrated against independent reference.");Beep();}GUI.enabled=true;
  GUI.enabled=!Sim.MainPower&&Carrying(ActionKind.Tool)&&Sim.BusChecked;
  if(Button(208,525,490,48,UI("Сбросить автомат шины","Reset isolated bus breaker"))){Sim.BreakerTripped=false;Sim.OverloadHours=0;diagnosticResult=UI("Автомат сброшен. Уменьшите запрос перед включением шины.","Breaker reset. Reduce demand before restoring the bus.");Sim.Incident("Breaker reset with main bus isolated.");}GUI.enabled=true;
  if(Button(722,525,490,48,UI("Подсказка ","Hint ")+Sim.HintLevel+" / 3"))Sim.HintLevel=Math.Min(3,Sim.HintLevel+1);
  string hint=Sim.HintLevel==0?UI("Ремонт вентилятора и датчика: подойдите к стойке, отключите очиститель, возьмите инструмент. Фильтр меняется физически по E.","For fan/sensor service, approach the rack, isolate cleaner power and bring the driver. Fit the cartridge physically with E."):Sim.HintLevel==1?UI("Высокий CO₂ может означать фильтр, поток воздуха, питание или ошибку измерения. Сравните независимый прибор.","A high CO2 reading can mean filter, airflow, power or measurement error. Compare the independent reference."):Sim.HintLevel==2?UI("Если оба прибора показывают рост, проверьте поток. Если поток есть — проверьте фильтр. Если показания расходятся — проверьте датчик.","If both instruments rise, check airflow. If airflow is present, inspect the cartridge. If readings disagree, inspect the sensor."):NextTask();
  Text(208,605,990,115,hint,body,muted);
  Text(208,746,990,34,UI("Подсказки не штрафуются. Жёлтая обводка показывает точное место взаимодействия.","Hints carry no penalty. Yellow guidance marks the exact interaction target."),small,cyan);
 }
 void IncidentPanel(){
  string log=string.Join("\n\n",Sim.Incidents);float height=body.CalcHeight(new GUIContent(log),960);
  incidentScroll=TouchScrollView(new Rect(208,277,1004,350),incidentScroll,new Rect(0,0,970,height));GUI.Label(new Rect(0,0,960,height),MobileText(log),body);EndTouchScrollView();
  Text(208,649,1000,43,UI("НАУЧНЫЕ ИСТОЧНИКИ / игровые мощности и скорости не являются спецификацией NASA","SCIENCE SOURCES / game ratings and rates are not NASA hardware specifications"),small,cyan);
  string[] links={"https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/","https://www.nasa.gov/exploration-research-and-technology/growing-plants-in-space/","https://www.nasa.gov/reference/crew-systems/","https://www.nasa.gov/missions/artemis/clps/electrodynamic-dust-shield-heading-to-moon-on-firefly-lander/"};
  string[] titles={"NASA · ECLSS","NASA · Plants","NASA · Shelter","NASA · Lunar dust"};
  for(int i=0;i<4;i++)if(Button(208+i*254,714,240,46,titles[i]))Application.OpenURL(links[i]);
 }
 void DrawHistory(Rect rect,int series){
  Box(rect.x,rect.y,rect.width,rect.height,new Color(.03f,.08f,.11f,.8f));
  var data=Sim.Samples;if(data==null||data.Count<2)return;
  for(int i=1;i<data.Count;i++){
   double a=series==0?data[i-1].Energy/10:series==1?data[i-1].Water/24:data[i-1].CO2/.7;
   double b=series==0?data[i].Energy/10:series==1?data[i].Water/24:data[i].CO2/.7;
   Vector2 start=new Vector2(rect.x+(float)((data[i-1].Hour-data[0].Hour)/Math.Max(.1,data[data.Count-1].Hour-data[0].Hour))*rect.width,rect.y+rect.height*(1-Mathf.Clamp01((float)a)));
   Vector2 end=new Vector2(rect.x+(float)((data[i].Hour-data[0].Hour)/Math.Max(.1,data[data.Count-1].Hour-data[0].Hour))*rect.width,rect.y+rect.height*(1-Mathf.Clamp01((float)b)));
   var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Vector2.SignedAngle(Vector2.right,end-start),start);Box(start.x,start.y,(end-start).magnitude,2,series==2?amber:cyan);GUI.matrix=matrix;
  }
 }
}
}

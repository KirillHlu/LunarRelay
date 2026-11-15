using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace LunarRelay.Editor {
public static class EngineeringValidation {
 public static void Run(){
  var report=new List<string>();Action<bool,string> check=(ok,text)=>{report.Add((ok?"PASS ":"FAIL ")+text);if(!ok)throw new Exception("Engineering simulation: "+text);};
  Func<int,Simulation> create=seed=>{var s=new Simulation(seed);s.EnableExpansion(seed);return s;};
  for(int seed=0;seed<16;seed++)for(int strategy=0;strategy<2;strategy++){
   var s=create(seed);s.ScrubberRepaired=s.ArrayRepaired=true;s.RoverOn=false;s.PriorityPower=strategy==0;s.LightingLevel=.5;s.SuitEquipped=true;
   if(strategy==1){s.OxygenLevel=0;s.WaterLevel=0;s.ShelterLevel=1;s.GreenhouseLevel=0;s.MealLevel=.5;}
   for(int i=0;i<3;i++)check(s.FillShield(i),"seed "+seed+" fills shelter block "+i);
   s.Packs=3;
   for(int t=0;t<8*3600;t+=30){if(!s.FanOn)s.FanOn=true;if(s.CropDays>=28)s.Harvest();s.Step(30,false,true);}
   check(s.Complete&&!s.Aborted,"seed "+seed+" / strategy "+strategy+" completes safely");
   double stock=s.CleanWater+s.UsedWater+s.LostWater+s.ElectrolysisWater;foreach(var value in s.ShieldLitres)stock+=value;
   check(Math.Abs(stock-(seed%4==3?22:28))<1e-7,"water mass conserved seed "+seed+" strategy "+strategy);
   check(Math.Abs(s.GeneratedO2+s.VentedH2-s.ElectrolysisWater)<1e-8,"electrolysis mass balance");
  }
  var off=create(0);off.MainPower=false;off.O2Valve=false;double temp=off.CabinC;off.Step(3600,false,false);
  check(off.CabinC<temp,"unpowered heating cools the cabin");check(off.GeneratedO2==0&&off.WaterProcessed==0,"unpowered processors do not produce resources");
  var water=create(1);water.WaterLevel=1;water.RoverOn=false;water.Step(1800,false,false);check(water.WaterProcessed>0,"water recovery requires delivered power and dirty feed");
  var crop=create(2);crop.RoverOn=false;crop.GreenhouseLevel=1;crop.ScrubberRepaired=crop.ArrayRepaired=true;for(int i=0;i<4*120;i++)crop.Step(30,false,true);
  check(crop.CropDays>=28&&crop.Harvest(),"healthy powered crop can be harvested");check(!crop.Harvest(),"cannot harvest the same crop twice");
  var dark=create(2);dark.GreenhouseLevel=0;dark.RoverOn=false;dark.Step(1800,false,false);check(dark.CropDays==18,"unlit plants do not grow");
  var dust=create(0);dust.RoverOn=false;dust.Step(30,false,false);double before=dust.SolarKW;dust.CleanPanel(0);dust.Step(30,false,false);check(dust.SolarKW>before,"cleaning increases solar output");
  var overload=create(0);overload.OxygenLevel=overload.WaterLevel=overload.GreenhouseLevel=overload.LightingLevel=overload.ShelterLevel=1;overload.Step(300,false,false);check(overload.BreakerTripped,"sustained overload trips breaker");
  var fan=create(1);fan.Step(2*3600,false,false);check(!fan.FanOn,"seeded airflow event occurs");var sensor=create(2);sensor.Step(2*3600,false,false);check(sensor.SensorFault&&sensor.MeasuredCO2>sensor.CO2KPa,"sensor fault changes reading, not true gas mass");
  var a=create(42);var b=create(42);check(a.EventHour==b.EventHour&&a.PanelDust[0]==b.PanelDust[0],"seed reproduces initial equipment and event time");
  var json=JsonUtility.ToJson(sensor);var restored=JsonUtility.FromJson<Simulation>(json);check(restored.ScenarioSeed==2&&restored.SensorFault&&restored.Samples.Count==sensor.Samples.Count,"simulation save round trip preserves scenario and history");
  var endless=create(2);endless.Endless=true;endless.ScrubberRepaired=endless.ArrayRepaired=true;endless.CartridgeCapacity=1.4;endless.RoverOn=false;endless.OxygenLevel=.2;endless.LightingLevel=.5;endless.Packs=3;
  for(int i=0;i<3;i++)endless.FillShield(i);
  for(int t=0;t<256*3600;t+=30){endless.FanOn=true;endless.SensorFault=false;for(int i=0;i<4;i++)endless.PanelDust[i]=0;if(endless.CropDays>=28)endless.Harvest();endless.Step(30,false,true);}
  check(endless.Hours>=255.99&&!endless.Complete&&!endless.Aborted,"endless survives 32 managed cycles without timer ending");
  check(endless.SupplyCycles>=31&&endless.SuppliedWater>0,"scheduled supplies recur without resetting repairs or time");
  double totalWater=endless.CleanWater+endless.UsedWater+endless.LostWater+endless.ElectrolysisWater;foreach(var v in endless.ShieldLitres)totalWater+=v;
  check(Math.Abs(totalWater-28-endless.SuppliedWater)<1e-6,"endless water ledger includes imported supply mass");
  check(endless.Samples.Count<=120&&endless.Incidents.Count<=100,"endless histories remain bounded");
  var resumed=JsonUtility.FromJson<Simulation>(JsonUtility.ToJson(endless));check(resumed.Endless&&resumed.SupplyCycles==endless.SupplyCycles&&resumed.Hours==endless.Hours,"endless clock and supply cycle survive serialization");
  resumed.Hours=10;check(resumed.Shadow&&!resumed.Storm,"shadow repeats in second cycle");resumed.Hours=13;check(resumed.Storm&&!resumed.Shadow,"drill repeats in second cycle");
  var unsafeEndless=create(0);unsafeEndless.Endless=true;unsafeEndless.Step(30,true,false);check(unsafeEndless.Aborted,"endless still enforces EVA safety");
  var legacySave=JsonUtility.FromJson<Simulation>("{\"Hours\":3,\"Expanded\":true}");check(!legacySave.Endless,"old saves retain finite mode");
  Directory.CreateDirectory("Builds");File.WriteAllLines("Builds/engineering-checks.txt",report);Debug.Log("Engineering checks passed: "+report.Count);
 }
}
}

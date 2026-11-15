using System;
using System.Collections.Generic;
namespace LunarRelay {
[Serializable] public sealed class ResourceSample {public double Hour,Energy,Water,CO2,Temperature;}
public sealed partial class Simulation {
 public bool Expanded,FanOn=true,SensorFault,BreakerTripped,EventTriggered,SuitEquipped;
 public bool AirflowChecked,SensorChecked,BusChecked,FilterChecked;
 public int ScenarioSeed,Scenario,EmergencyCount,Harvests,HintLevel;
 public double CleanWater=24,UsedWater=4,LostWater,WaterProcessed,ElectrolysisWater,GeneratedO2,VentedH2;
 public double OverloadHours,HeatingOffHours,LowCropHours,RoverSavedKWh;
 public double MealLevel=1,ShelterReserve=.15;
 public double FoodKg=1.6,CropDays=18,CropHealth=1,CropMoisture=.7,WaterShortageHours;
 public double OxygenLevel=.4,WaterLevel=.5,GreenhouseLevel=.5,LightingLevel=1,ShelterLevel=.5;
 public double SuitBattery=.8,SensorBias,ShelterMassGeometry=1,EventHour=1.5,NextSample;
 public double[] PanelDust={.15,.15,.15,.15},ShieldLitres={0,0,0},ShieldCoverage={1,1,1};
 public double[] Delivered=new double[8];
 public bool PriorityPower=true,DustActivity;
 public List<ResourceSample> Samples=new List<ResourceSample>();
 public List<string> Incidents=new List<string>();
 public double MeasuredCO2=>CO2KPa+(SensorFault?SensorBias:0);
 public double DustEfficiency {get {double sum=0;foreach(var dust in PanelDust)sum+=1-dust;return sum/4;}}
 public void EnableExpansion(int seed){
  Expanded=true;ScenarioSeed=seed;Scenario=Math.Abs(seed%4);var rng=new Random(seed);
  Energy=7.6+rng.NextDouble()*.8;CleanWater=Scenario==3?18:24;FoodKg=Scenario==3?.65:1.6;EventHour=1.2+rng.NextDouble()*.5;
  for(int i=0;i<4;i++)PanelDust[i]=.10+rng.NextDouble()*(Scenario==0?.12:.23);
  Incident("Shift initialized. Seed "+seed+". All equipment ratings are training assumptions.");
 }
 public void Incident(string text){Incidents.Add(Hours.ToString("0.00")+" h · "+text);if(Incidents.Count>100)Incidents.RemoveAt(0);}
 public bool Harvest(){if(CropDays<28||CropHealth<.45)return false;FoodKg+=.6*CropHealth;CropDays=18;Harvests++;Incident("Harvest collected. Mature crop increased the food reserve; seedlings replanted.");return true;}
 public bool FillShield(int slot){if(CleanWater<4||slot<0||slot>2)return false;CleanWater-=4;ShieldLitres[slot]=4;Incident("4 L allocated to a shelter water block. Total water is conserved.");return true;}
 public void RecoverShield(int slot){if(slot<0||slot>2)return;CleanWater+=ShieldLitres[slot];ShieldLitres[slot]=0;Incident("Shelter water returned to drinking reserve. Shielding mass decreased.");}
 public void CleanPanel(int index){PanelDust[index]=Math.Max(0,PanelDust[index]-.22);Incident("Panel "+(index+1)+" serviced: available solar output improved.");}
 void TickExpanded(double h,bool outside,bool sheltered){
  Hours+=h;AdvanceEndlessCycle();
  if(!EventTriggered&&Hours>=EventHour){
   EventTriggered=true;
   if(Scenario==1){FanOn=false;EmergencyCount++;Incident("CO2 is rising. Air circulation changed; check airflow and power.");}
   else if(Scenario==2){SensorFault=true;SensorBias=.16;EmergencyCount++;Incident("CO2 sensor changed abruptly. Compare with the reference instrument.");}
   else if(Scenario==3){PanelDust[0]=Math.Min(.75,PanelDust[0]+.16);EmergencyCount++;Incident("A scheduled nearby surface operation deposited dust on array 1. Output fell.");}
  }
  if(DustActivity)for(int i=0;i<4;i++)PanelDust[i]=Math.Min(.75,PanelDust[i]+h*.012);
  SolarKW=Shadow?0:(ArrayRepaired?3.6:1.1)*DustEfficiency;
  double[] loads={AirOn?.65:0,OxygenLevel, ThermalOn?.55:0,.12*LightingLevel,.35*WaterLevel,.8*GreenhouseLevel,RoverOn?1.8:0,.12*ShelterLevel};
  LoadKW=.15;foreach(double value in loads)LoadKW+=value;
  if(MainPower&&!BreakerTripped&&LoadKW>4.4){OverloadHours+=h;AdvanceEndlessCycle();if(OverloadHours>.05){BreakerTripped=true;EmergencyCount++;Incident("Bus breaker tripped after sustained overload. Reduce loads and isolate the bus before reset.");}}else OverloadHours=0;
  if(!ThermalOn)HeatingOffHours+=h;AdvanceEndlessCycle();if(GreenhouseLevel<.5)LowCropHours+=h;AdvanceEndlessCycle();if(!RoverOn)RoverSavedKWh+=1.8*h;
  double available=MainPower&&!BreakerTripped?Math.Min(4.4,SolarKW+Math.Min(3,Energy*.92/h)):0;
  double baseServed=Math.Min(.15,available);available-=baseServed;double total=baseServed;
  if(PriorityPower){int[] order={0,2,1,7,4,3,5,6};foreach(int i in order){Delivered[i]=Math.Min(loads[i],available);available-=Delivered[i];total+=Delivered[i];}}
  else {double demand=LoadKW-.15,ratio=demand>0?Math.Min(1,available/demand):0;for(int i=0;i<8;i++){Delivered[i]=loads[i]*ratio;total+=Delivered[i];}}
  ServedFraction=LoadKW>0?total/LoadKW:1;
  if(MainPower&&!BreakerTripped){double delta=SolarKW-total;if(delta>=0)Energy=Math.Min(Capacity,Energy+delta*h*.94);else Energy=Math.Max(0,Energy+delta*h/.92);}
  // Water ledger: processing separates recoverable water from waste, never creates water.
  double crewUse=Math.Min(CleanWater,.32*h);CleanWater-=crewUse;UsedWater+=crewUse*.8;LostWater+=crewUse*.2;
  WaterShortageHours=crewUse<.32*h*.99?WaterShortageHours+h:Math.Max(0,WaterShortageHours-h);
  double recycled=Math.Min(UsedWater,Delivered[4]/.35*1.0*h);UsedWater-=recycled;CleanWater+=recycled*.95;LostWater+=recycled*.05;WaterProcessed+=recycled*.95;
  double oxygen=Math.Min(Math.Max(0,4-StoredO2),Math.Min(CleanWater*8/9,Delivered[1]*.16*h));
  double electroWater=oxygen*9/8;CleanWater-=electroWater;ElectrolysisWater+=electroWater;GeneratedO2+=oxygen;VentedH2+=oxygen/8;StoredO2+=oxygen;
  double consumed=.84*2/24*h;O2=Math.Max(0,O2-consumed);ConsumedO2+=consumed;
  if(O2Valve){double target=20.8*1000*Volume/(R*Kelvin)*.032;double added=Math.Min(StoredO2,Math.Max(0,target-O2));StoredO2-=added;O2+=added;}
  CO2+=2.0/24*h;
  if(AirOn&&FanOn&&ScrubberRepaired&&CartridgeCapacity>0){double removed=Math.Min(CO2,Math.Min(CartridgeCapacity,.15*h*Delivered[0]/.65));CO2-=removed;CartridgeCapacity-=removed;RemovedCO2+=removed;}
  double heating=Delivered[2]/.55;
  CabinC+=(heating*.9*(22-CabinC)-(1-heating)*1.1)*h;
  double irrigation=Math.Min(CleanWater,Delivered[5]/.8*.18*h);CleanWater-=irrigation;UsedWater+=irrigation*.8;LostWater+=irrigation*.2;
  CropMoisture=Math.Max(0,Math.Min(1,CropMoisture+irrigation*3-.14*h));
  bool growing=CropMoisture>.15&&CabinC>17&&CabinC<28;
  CropHealth=Math.Max(0,Math.Min(1,CropHealth+(growing?.03:-.14)*h));
  CropDays=Math.Min(28,CropDays+(growing?Delivered[5]/.8*72/24*h:0));
  FoodKg=Math.Max(0,FoodKg-.10*MealLevel*h);
  ShelterReserve=Math.Max(0,Math.Min(.4,ShelterReserve+(Delivered[7]*.8-.02)*h));
  if(outside){SuitHours=Math.Max(0,SuitHours-h);SuitBattery=Math.Max(0,SuitBattery-.12*h);}else if(!Storm){SuitHours=Math.Min(3,SuitHours+h*.5);SuitBattery=Math.Min(.8,SuitBattery+h*.12);}
  if(Storm){double mass=0;for(int i=0;i<3;i++)mass+=ShieldLitres[i]*ShieldCoverage[i];Risk+=h*(sheltered?Math.Max(2,10-mass*.65):outside?30:14);if(sheltered)ShelterHours+=h;AdvanceEndlessCycle();}
  if(Hours>=NextSample){Samples.Add(new ResourceSample{Hour=Hours,Energy=Energy,Water=CleanWater,CO2=CO2KPa,Temperature=CabinC});NextSample=Hours+.1;if(Samples.Count>120)Samples.RemoveAt(0);}
  if(CO2KPa>.7)Abort("CO2 exceeded the training limit. Check cartridge, airflow and delivered power.");
  else if(O2KPa<19)Abort("Cabin oxygen fell below the training range. Check stored oxygen and the regulator.");
  else if(CabinC<16||CabinC>29)Abort("Cabin temperature left the training range. Heating needs delivered power.");
  else if(WaterShortageHours>2)Abort("Drinking water was unavailable for too long. Recycle water or recover shelter water.");
  else if(Risk>55)Abort("The crew remained in insufficient shelter during the particle drill.");
  else if(outside&&(!SuitEquipped||SuitHours<=0||SuitBattery<=0))Abort("EVA reserve or suit check failed. Return through the airlock before reserves run out.");
  else if(Endless&&FoodKg<=0)Abort("Food reserve exhausted. Harvest crops and manage meals between supply cycles.");
  else if(!Endless&&Hours>=8){if(ScrubberRepaired&&ArrayRepaired&&Packs>=3&&Energy>=1&&Risk<30&&ShelterHours>=1.5&&FoodKg>=.1){Complete=true;Outcome="Shift completed. Essential resources and shelter readiness were maintained.";}else Abort("Shift ended before air, power, food reserve, water-block placement and shelter-time checks were complete.");}
 }
}
}

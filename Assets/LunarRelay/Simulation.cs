using System;

namespace LunarRelay {
// SI masses; electrical energy in kWh, power in kW, time in hours.
[Serializable] public sealed partial class Simulation {
 public const double R=8.314462618, Volume=70, Kelvin=295.15;
 public double Hours, Energy=8, Capacity=10, O2=20.8*1000*Volume/(R*Kelvin)*.032, CO2=.12*1000*Volume/(R*Kelvin)*.044, StoredO2=2;
 public double CabinC=22, Risk, ShelterHours, SuitHours=3, SolarKW, LoadKW, ServedFraction=1, RemovedCO2, ConsumedO2;
 public bool MainPower=true, AirOn=true, ThermalOn=true, RoverOn=true, O2Valve=true, ScrubberRepaired, ArrayRepaired;
 public bool Aborted, Complete; public string Outcome="";
 public int Packs, Repairs, UnsafeAttempts; public double CartridgeCapacity=.45;
 public bool Shadow=>CycleHour>=2 && CycleHour<4;
 public bool Storm=>CycleHour>=5 && CycleHour<7;
 public double O2KPa=>O2/.032*R*Kelvin/Volume/1000;
 public double CO2KPa=>CO2/.044*R*Kelvin/Volume/1000;
 public Simulation(int seed=1){RewardRunId=Guid.NewGuid().ToString("N");Energy=7.6+new Random(seed).NextDouble()*.8;}
 public void Step(double seconds,bool outside,bool sheltered){
  if(Aborted||Complete||seconds<=0)return;
  // Bound numerical steps: identical results across frame rates, no long-frame overshoot.
  while(seconds>0){double dt=Math.Min(seconds,30);seconds-=dt;Tick(dt/3600,outside,sheltered);if(Aborted||Complete)break;}
 }
 void Tick(double h,bool outside,bool sheltered){
  if(Expanded){TickExpanded(h,outside,sheltered);return;}
  Hours+=h;SolarKW=Shadow?0:(ArrayRepaired?3.6:1.1);
  LoadKW=.25+(AirOn?.65:0)+(ThermalOn?.55:0)+(RoverOn?1.8:0);
  double supply=MainPower?SolarKW:0, request=LoadKW*h, batteryAvailable=MainPower?Energy*.92:0;
  ServedFraction=request>0?Math.Min(1,(supply*h+batteryAvailable)/request):1;
  double delta=supply-LoadKW;
  if(MainPower){if(delta>=0)Energy=Math.Min(Capacity,Energy+delta*h*.94);else Energy=Math.Max(0,Energy-Math.Min(Energy*.92,-delta*h)/.92);}
  double used=.84*2/24*h;O2=Math.Max(0,O2-used);ConsumedO2+=used;
  if(O2Valve){double target=20.8*1000*Volume/(R*Kelvin)*.032;double added=Math.Min(StoredO2,Math.Max(0,target-O2));O2+=added;StoredO2-=added;}
  CO2+=1.0*2/24*h;
  if(MainPower&&AirOn&&ScrubberRepaired&&CartridgeCapacity>0){double removed=Math.Min(CO2,Math.Min(CartridgeCapacity,.15*h*ServedFraction));CO2-=removed;CartridgeCapacity-=removed;RemovedCO2+=removed;}
  // Teaching thermal buffer: closed habitat drifts slowly without active control.
  CabinC+=(ThermalOn&&MainPower?-(CabinC-22)*.8*ServedFraction+.5*(1-ServedFraction):.5)*h;
  if(outside)SuitHours=Math.Max(0,SuitHours-h);else if(!Storm)SuitHours=Math.Min(3,SuitHours+h*.5);
  // Dimensionless training index, not dose or a medical prediction.
  if(Storm)Risk+=h*(sheltered?Math.Max(2,16-4*Packs):(outside?30:14));
  if(Storm&&sheltered)ShelterHours+=h;
  if(CO2KPa>.7)Abort("Air cleaning stopped for too long. Carbon dioxide rose above this exercise's limit.");
  else if(O2KPa<19)Abort("The oxygen regulator was closed too long. Stored oxygen must reach the cabin.");
  else if(CabinC>29||CabinC<16)Abort("Thermal control was unavailable for too long. Cabin temperature left the exercise range.");
  else if(Risk>55)Abort("The crew needed more time in the stocked shelter during the particle-event drill.");
  else if(outside&&SuitHours<=0)Abort("Your suit reserve reached the training limit. Return through the airlock earlier.");
  else if(!Endless&&Hours>=8){
   if(ScrubberRepaired&&ArrayRepaired&&Packs>=3&&Energy>=1&&Risk<30&&ShelterHours>=1.5){Complete=true;Outcome="Outpost ready! Air cleaning, power recovery and a stocked shelter kept the mission running.";}
   else Abort("Shift ended before readiness checks were complete. Repair both systems, stow three water packs and protect the power reserve.");
  }
 }
 public void Abort(string why){Aborted=true;Outcome=why;}
}
}

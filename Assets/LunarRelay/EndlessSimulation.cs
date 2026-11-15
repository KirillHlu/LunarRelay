using System;
namespace LunarRelay {
public sealed partial class Simulation {
 public bool Endless;
 public int SupplyCycles;
 public double SuppliedWater,SuppliedFood,SuppliedO2;
 public double CycleHour=>Endless?Hours%8:Hours;
 public int CycleNumber=>(int)Math.Floor(Hours/8)+1;
 void AdvanceEndlessCycle(){
  if(!Endless)return;
  int completed=(int)Math.Floor(Hours/8);
  while(SupplyCycles<completed){
   SupplyCycles++;Risk=0;ShelterHours=0;
   double water=Math.Max(0,24-CleanWater),food=Math.Max(0,1.6-FoodKg),oxygen=Math.Max(0,2-StoredO2);
   CleanWater+=water;SuppliedWater+=water;FoodKg+=food;SuppliedFood+=food;StoredO2+=oxygen;SuppliedO2+=oxygen;
   // Scheduled logistics include routine replacement media for an already serviced scrubber.
   if(ScrubberRepaired)CartridgeCapacity=Math.Max(CartridgeCapacity,1.4);
   Scenario=(int)(((long)ScenarioSeed+SupplyCycles)%4);if(Scenario<0)Scenario+=4;
   EventTriggered=false;EventHour=SupplyCycles*8+1.5;
   Incident("New endless cycle. Scheduled logistics replenish water, food, stored oxygen and serviced-filter media. Repairs, equipment settings and energy are retained. Drill risk starts fresh.");
  }
 }
}
}

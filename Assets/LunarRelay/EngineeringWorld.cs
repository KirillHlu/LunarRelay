using UnityEngine;
namespace LunarRelay {
public sealed class AnimatedAssembly:MonoBehaviour{}
public sealed partial class GameSession {
 Material farmLampMaterial;
 Transform farm;Transform[] cropVisuals;readonly System.Collections.Generic.List<StationAction> panelService=new System.Collections.Generic.List<StationAction>();
 int engineeringPage,diagnosticView;string diagnosticResult="";float lastWarning=-100;string previousWarning="";
 void MakeInstrumentScreenMatte(Transform equipment){
  var screen=Find(equipment,"Blank display");var renderer=screen.GetComponent<MeshRenderer>();
  var material=new Material(renderer.sharedMaterial);material.name="Matte instrument screen";
  material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.08f);
  material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
  material.SetFloat("_EnvironmentReflections",0);material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
  material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
  material.SetColor("_BaseColor",new Color(.035f,.075f,.10f,1));renderer.sharedMaterial=material;
 }
 void BuildEngineeringWorld(){
  farm=Model("Greenhouse",new Vector3(-7.5f,.20f,1.55f),Quaternion.identity);
  foreach(var renderer in farm.GetComponentsInChildren<MeshRenderer>())if(renderer.name=="Horticulture LED"){if(!farmLampMaterial)farmLampMaterial=new Material(renderer.sharedMaterial);renderer.sharedMaterial=farmLampMaterial;}
  MakeInstrumentScreenMatte(farm);
  cropVisuals=new[]{Find(farm,"Plants_Seedling"),Find(farm,"Plants_Mature"),Find(farm,"Plants_Wilted")};
  foreach(var phase in cropVisuals){CombineStatic(phase);phase.gameObject.AddComponent<AnimatedAssembly>();phase.gameObject.SetActive(false);}cropVisuals[0].gameObject.SetActive(true);
  Act(Find(farm,"Blank display"),ActionKind.Greenhouse,"Inspect greenhouse / harvest mature plants",true);CombineStatic(farm);
  var water=Model("WaterRecycler",new Vector3(-6.25f,.20f,1.25f),Quaternion.identity);
  MakeInstrumentScreenMatte(water);
  Act(Find(water,"Blank display"),ActionKind.WaterRecycler,"Water recovery controls",true);CombineStatic(water);
  var oxygen=Model("OxygenGenerator",new Vector3(-6.25f,.20f,-1.25f),Quaternion.Euler(0,180,0));
  MakeInstrumentScreenMatte(oxygen);
  Act(Find(oxygen,"Blank display"),ActionKind.OxygenGenerator,"Oxygen generator controls",true);CombineStatic(oxygen);
  var brush=Model("DustTool",new Vector3(-6.8f,.285f,-1.5f),Quaternion.Euler(0,0,90));CombineStatic(brush);Portable(Act(brush,ActionKind.Brush,"Panel cleaning tool"));
  // Relocate the three original portable packs to the clear south side of the pod.
  int pack=0;foreach(var item in actions)if(item.Kind==ActionKind.Pack)item.transform.position=new Vector3(-7.65f+pack++*.65f,.35f,-.55f);
  var status=actions.Find(a=>a.Kind==ActionKind.Info&&a.name=="AirlockGauge_Face");if(status){status.Kind=ActionKind.SuitCheck;status.Label="Check and equip EVA suit";}
  foreach(var screen in actions)if(screen.Kind==ActionKind.RepairAir&&screen.name=="STATUS_BLANK_SCREEN"){screen.Kind=ActionKind.Diagnose;screen.Label="Life-support diagnostic terminal";}
 }
 void AddPanelService(Transform solar,int index){
  var surface=Find(solar,"SolarFace");var action=Act(surface,ActionKind.CleanPanel,"Service solar panel "+(index+1),true);panelService.Add(action);
 }
 bool EngineeringUse(StationAction action){
  switch(action.Kind){
   case ActionKind.Brush:Pickup(action);return true;
   case ActionKind.Greenhouse:Tell(Sim.Harvest()?UI("Урожай собран.","Harvest collected."):UI("Рост растений: ","Crop growth: ")+Sim.CropDays.ToString("0.0")+" / 28 · "+(Sim.CropHealth*100).ToString("0")+"%");return true;
   case ActionKind.WaterRecycler:CycleLocalLoad(4);return true;
   case ActionKind.OxygenGenerator:CycleLocalLoad(1);return true;
   case ActionKind.Diagnose:HandsDiagnosis();return true;
   case ActionKind.SuitCheck:Sim.SuitEquipped=true;Tell(UI("Скафандр проверен. Запасы доступны в Tab.","Suit checked and equipped. EVA reserves are available in Tab."));return true;
   case ActionKind.CleanPanel:
    if(!Sim.Expanded)return true;
    if(!Sim.SuitEquipped||!Outside){Tell(UI("Проверьте скафандр и выйдите через шлюз.","Check your suit and exit through the airlock."));return true;}
    if(!Carrying(ActionKind.Brush)){Tell(UI("Нужен инструмент очистки из жилого модуля.","Bring the panel cleaning tool from the sleeping pod."));return true;}
    int index=panelService.IndexOf(action);Sim.CleanPanel(index);Tell(UI("Панель очищена. Проверьте выработку в Tab.","Panel serviced. Check generation in Tab."));return true;
  }return false;
 }
 void OpenEngineering(){if(!Sim.Expanded){Tell("Expanded engineering systems are disabled in legacy regression mode.");return;}showStatus=true;showInventory=false;showLog=false;paused=false;UpdateCursor();}
 void EngineeringPresentation(){
  if(!Sim.Expanded)return;
  if(farmLampMaterial)farmLampMaterial.SetColor("_EmissionColor",new Color(.8f,.8f,.66f)*(float)(Sim.Delivered[5]/.8));
  foreach(var entry in statusDisplays){if(!entry.Key)continue;var action=entry.Key.GetComponent<StationAction>();if(action&&(action.Kind==ActionKind.Diagnose||action.Kind==ActionKind.RepairAir)){
   bool ready=Sim.ScrubberRepaired&&Sim.FanOn&&Sim.AirOn&&Sim.Delivered[0]>.05;var color=ready?new Color(.12f,.7f,.62f):new Color(.85f,.34f,.08f);entry.Value.SetColor("_BaseColor",color*.16f);entry.Value.SetColor("_EmissionColor",color*(Sim.MainPower?.8f:.12f));entry.Key.SetPropertyBlock(entry.Value);
  }}
  if(cropVisuals!=null){int phase=Sim.CropHealth<.45?2:Sim.CropDays>=28?1:0;for(int i=0;i<3;i++)cropVisuals[i].gameObject.SetActive(i==phase);}
  for(int i=0;i<cabinLights.Count;i++)if(cabinLights[i])cabinLights[i].intensity=cabinBrightness[i]*1.25f*Mathf.Max(.03f,(float)Sim.ServedFraction)*Mathf.Lerp(.08f,1,(float)Sim.LightingLevel);
  for(int i=0;i<panelService.Count;i++){
   var renderer=panelService[i].GetComponent<MeshRenderer>();if(renderer){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetColor("_BaseColor",Color.Lerp(Color.white,new Color(.42f,.40f,.35f),(float)Sim.PanelDust[i]*.65f));renderer.SetPropertyBlock(block);}
  }
  if(Playing){
   Sim.DustActivity=Outside&&Player.Body.velocity.sqrMagnitude>.1f;
   int slot=0;foreach(var action in actions)if(action.Kind==ActionKind.InstallPack){if(slot<3){float distance=Vector3.Distance(Player.transform.position,action.transform.position);Sim.ShieldCoverage[slot]=1/(1+distance*.45);slot++;}}
   string warning=EngineeringWarning();if(warning!=""&&warning!=previousWarning&&Time.unscaledTime-lastWarning>12){Tell(warning);Beep();previousWarning=warning;lastWarning=Time.unscaledTime;}
  }
 }
 string EngineeringWarning(){
  if(Sim.MeasuredCO2>.4)return UI("Показание CO₂ растёт. Проверьте диагностический экран у очистителя.","CO2 reading is rising. Inspect the diagnostic screen at the air cleaner.");
  if(Sim.BreakerTripped)return UI("Главная шина отключилась. Проверьте нагрузку.","Main bus tripped. Check electrical demand.");
  if(Sim.Energy<2)return NextTask();
  if(Sim.FoodKg<.25)return UI("Малый запас еды. Проверьте теплицу и норму выдачи.","Food reserve is low. Check the greenhouse and meal provision level.");
  if(Sim.CleanWater<4)return UI("Мало доступной воды. Проверьте переработчик и запасы в убежище.","Available water is low. Check recovery and shelter water.");
  if(Sim.CycleHour>4.2&&Sim.CycleHour<5)return UI("Приближается учебное событие. Проверьте убежище, воду и резерв энергии.","Particle drill approaching. Check shelter, water and energy reserves.");
  return "";
 }
 string InvestigationGoal(){
  string warning=EngineeringWarning();if(warning!="")return warning;
  return UI("Подойдите к диагностическому экрану очистителя и нажмите E.","Approach the air-cleaner diagnostic screen and press E.");
 }
}
}

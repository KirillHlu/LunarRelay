using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace LunarRelay.Editor {
public static class BuildTools {
 static string windowsFolder="Builds/Windows";
 public static void WindowsTouchRegression(){windowsFolder="Builds/WindowsTouchRegression";Windows();}
 public static void WindowsRelease(){windowsFolder="Builds/WindowsRelease";Windows();}
 public static void WindowsCreditReadability(){windowsFolder="Builds/WindowsReadableCredits";Windows();}
 static void PhoneBranding(){
  const string path="Assets/Branding/LunarRelayIcon.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!icon)throw new Exception("Application icon missing");
  PlayerSettings.productName="Lunar Relay";PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
  var sizes=PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);var icons=new Texture2D[sizes.Length];for(int i=0;i<icons.Length;i++)icons[i]=icon;PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android,icons);
  foreach(var kind in PlayerSettings.GetSupportedIconKinds(UnityEditor.Build.NamedBuildTarget.Android)){var slots=PlayerSettings.GetPlatformIcons(UnityEditor.Build.NamedBuildTarget.Android,kind);foreach(var slot in slots){slot.SetTexture(icon,0);if(slot.maxLayerCount>1){slot.SetTexture(Texture2D.blackTexture,0);slot.SetTexture(icon,1);}}PlayerSettings.SetPlatformIcons(UnityEditor.Build.NamedBuildTarget.Android,kind,slots);}
  AssetDatabase.SaveAssets();
 }
 public static void AndroidPhone(){
  Scene();ValidateModels();PhoneBranding();
  PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.lunarrelay.outpost");PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel35;PlayerSettings.Android.useCustomKeystore=false;PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});PlayerSettings.bundleVersion="1.0.0";PlayerSettings.Android.bundleVersionCode=1;EditorUserBuildSettings.buildAppBundle=false;
  var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=mobile;}
  Directory.CreateDirectory("Builds/Android");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/LunarRelay.unity"},locationPathName="Builds/Android/LunarRelay.apk",target=BuildTarget.Android,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed");
 }
 public static void WindowsTaskCredits(){EngineeringValidation.Run();windowsFolder="Builds/WindowsTaskCredits";Windows();}
 public static void WindowsDecor(){EngineeringValidation.Run();windowsFolder="Builds/WindowsDecor";Windows();}
 public static void WindowsRackSeams(){EngineeringValidation.Run();windowsFolder="Builds/WindowsRackSeams";Windows();}
 public static void WindowsSoftForms(){EngineeringValidation.Run();windowsFolder="Builds/WindowsSoftForms";Windows();}
 public static void WindowsTaskFlow(){EngineeringValidation.Run();windowsFolder="Builds/WindowsTaskFlow";Windows();}
 public static void WindowsShortWait(){EngineeringValidation.Run();windowsFolder="Builds/WindowsShortWait";Windows();}
 public static void WindowsLevers(){EngineeringValidation.Run();windowsFolder="Builds/WindowsLevers";Windows();}
 public static void WindowsClearGoals(){EngineeringValidation.Run();windowsFolder="Builds/WindowsClearGoals";Windows();}
 public static void WindowsHandsOn(){EngineeringValidation.Run();windowsFolder="Builds/WindowsHandsOn";Windows();}
 public static void WindowsPrecision(){EngineeringValidation.Run();windowsFolder="Builds/WindowsPrecision";Windows();}
 public static void WindowsHandPolish(){EngineeringValidation.Run();windowsFolder="Builds/WindowsHandPolish";Windows();}
 public static void WindowsEndless(){EngineeringValidation.Run();windowsFolder="Builds/WindowsEndless";Windows();}
 public static void WindowsBuildingGuidance(){EngineeringValidation.Run();windowsFolder="Builds/WindowsBuildingGuidance";Windows();}
 public static void WindowsReadableLighting(){EngineeringValidation.Run();windowsFolder="Builds/WindowsReadableLighting";Windows();}
 public static void WindowsEngineering(){EngineeringValidation.Run();windowsFolder="Builds/WindowsEngineeringComplete";Windows();}
 public static void WindowsSolarMount(){windowsFolder="Builds/WindowsSolarMount";Windows();}
 public static void WindowsSolarLayout(){windowsFolder="Builds/WindowsSolarLayout";Windows();}
 public static void WindowsStableFrames(){windowsFolder="Builds/WindowsStableFrames";Windows();}
 public static void WindowsRebuilt(){windowsFolder="Builds/WindowsRebuilt";Windows();}
 public static void WindowsCorridor(){windowsFolder="Builds/WindowsCorridor";Windows();}
 public static void WindowsTablet(){windowsFolder="Builds/WindowsTabletFinal";Windows();}
 public static void WindowsMinimal(){windowsFolder="Builds/WindowsMinimal";Windows();}
 public static void WindowsUX(){windowsFolder="Builds/WindowsUXFinal";Windows();}
 public static void WindowsPolished(){windowsFolder="Builds/WindowsPolished";Windows();}
 static void ValidateModels(){
  foreach(var role in new[]{"Habitat","Sleeping","Terrain","Power","Tool","Supplies","Solar","LifeSupport","Rover","Greenhouse","WaterRecycler","OxygenGenerator","DustTool"}){
   var path="Assets/Resources/Models/"+role+".lrmodel";
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   if(!model){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);model=AssetDatabase.LoadAssetAtPath<GameObject>(path);}
   if(!model||model.GetComponentsInChildren<MeshFilter>(true).Length==0)throw new Exception("Required model did not import: "+role);
  }
 }
 static void PresentationAssets(){
  const string path="Assets/Resources/OutpostLook.asset";
  var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
  if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
  if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
  if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(0);bloom.threshold.Override(1.5f);bloom.clamp.Override(2);bloom.scatter.Override(.35f);
  if(!profile.TryGet<ScreenSpaceLensFlare>(out var flare)){flare=profile.Add<ScreenSpaceLensFlare>();AssetDatabase.AddObjectToAsset(flare,profile);}flare.intensity.Override(0);
  if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(.65f);color.contrast.Override(5);color.saturation.Override(-7);
  if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}vignette.intensity.Override(.09f);vignette.smoothness.Override(.45f);
  foreach(var component in profile.components)EditorUtility.SetDirty(component);EditorUtility.SetDirty(profile);
 }
 [MenuItem("Lunar Relay/Create training scene")]
 public static void Scene(){AssetDatabase.Refresh();ValidateModels();PresentationAssets();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Lunar Relay Session").AddComponent<GameSession>();Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/LunarRelay.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/LunarRelay.unity",true)};PlayerSettings.companyName="Lunar Relay Studio";PlayerSettings.productName="Lunar Relay";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;AssetDatabase.SaveAssets();}
 [MenuItem("Lunar Relay/Build Windows")]
 public static void Windows(){Scene();ValidateSimulation();Directory.CreateDirectory(windowsFolder);var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/LunarRelay.unity"},locationPathName=windowsFolder+"/LunarRelay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});File.WriteAllText("Builds/windows-build-result.txt",report.summary.result+"\n"+report.summary.totalSize);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");}
 [MenuItem("Lunar Relay/Build WebGL")]
 public static void WebGL(){if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new Exception("Unity WebGL Build Support is not installed. Install it for 6000.3.25f1 in Unity Hub.");Scene();ValidateSimulation();PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.memorySize=256;PlayerSettings.WebGL.template="APPLICATION:Default";Directory.CreateDirectory("Builds/WebGL");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/LunarRelay.unity"},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed");}
 static void Check(bool condition,string message){if(!condition)throw new Exception("Simulation check failed: "+message);}
 public static void ValidateSimulation(){
  var success=new Simulation(5){ScrubberRepaired=true,ArrayRepaired=true,RoverOn=false,Packs=3};for(int i=0;i<8*3600;i+=10)success.Step(10,false,true);Check(success.Complete&&!success.Aborted,"solvable prepared shift");
  var outside=new Simulation(5){ScrubberRepaired=true,ArrayRepaired=true,RoverOn=false,Packs=3};for(int i=0;i<8*3600;i+=10)outside.Step(10,false,false);Check(!outside.Complete,"shelter cannot be skipped");
  var off=new Simulation{MainPower=false};double initialO2=off.O2;off.O2Valve=false;off.Step(3600,false,false);Check(Math.Abs(initialO2-off.O2-.07)<1e-8,"nominal oxygen mass balance");Check(off.RemovedCO2==0,"unpowered scrubber cannot remove gas");
  var depleted=new Simulation{Energy=.001,MainPower=true};depleted.Hours=2;depleted.Step(3600,false,false);Check(depleted.Energy>=0&&depleted.Energy<.00001,"battery depletion bound");
  var a=new Simulation(12){ScrubberRepaired=true,ArrayRepaired=true,RoverOn=false,Packs=3};var b=new Simulation(12){ScrubberRepaired=true,ArrayRepaired=true,RoverOn=false,Packs=3};for(int i=0;i<600;i++)a.Step(1,false,true);for(int i=0;i<20;i++)b.Step(30,false,true);Check(Math.Abs(a.Energy-b.Energy)<.0001,"frame-rate independent energy");
  var capped=new Simulation{Energy=10,ArrayRepaired=true,RoverOn=false};capped.Step(3600,false,false);Check(capped.Energy<=10,"capacity bound");Directory.CreateDirectory("Builds");File.WriteAllText("Builds/simulation-checks.txt","PASS: prepared shift, shelter required, oxygen balance, unpowered scrubber, battery depletion, timestep energy, capacity bound.\n");Debug.Log("Lunar Relay simulation checks passed.");
 }
}
}

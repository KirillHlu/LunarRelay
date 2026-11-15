using System;
using System.Collections.Generic;
using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 [Serializable]public class Translation {public string en,ru;}
 [Serializable]public class TranslationTable {public Translation[] entries;}
 readonly Dictionary<string,string> russian=new Dictionary<string,string>();
 bool useRussian=false;
 void InitializeLanguage(){
  useRussian=PlayerPrefs.GetInt("LunarRelayLanguageV2",0)==1;
  var file=Resources.Load<TextAsset>("Russian");if(file)foreach(var entry in JsonUtility.FromJson<TranslationTable>(file.text).entries)russian[entry.en]=entry.ru;
 }
 string L(string text){if(text=="PICKUP AND BACKPACK GUIDE"&&!useRussian)return "PICKUP AND BACKPACK\nE picks up items and uses equipment. R stores the held item; I opens the six-slot backpack to equip or drop an item. Picking up another item stores the previous one if there is space. Q drops the held item. Aim at the matching service point and press E: cartridges, batteries and water packs snap into place from your hand or backpack. The yellow outline and distance marker show the current goal. You may carry all three water packs, then fit them individually. Hold Ctrl or C to crouch. Standing up is blocked under low ceilings.";if(!useRussian||string.IsNullOrEmpty(text))return text;return russian.TryGetValue(text,out var translated)?translated:text;}
 void LanguageButton(float x,float y,float w=245){if(Button(x,y,w,38,useRussian?"ENGLISH":"РУССКИЙ")){useRussian=!useRussian;PlayerPrefs.SetInt("LunarRelayLanguageV2",useRussian?1:0);PlayerPrefs.Save();foreach(var label in FindObjectsByType<LocalizedStationLabel>(FindObjectsSortMode.None))label.GetComponent<TextMesh>().text=L(label.English);}}
}
public sealed class LocalizedStationLabel:MonoBehaviour {public string English;}
}

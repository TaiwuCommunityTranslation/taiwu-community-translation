using System.Reflection;
using System.IO;
using TaiwuModdingLib.Core.Plugin;
using TMPro;
using UnityEngine;
using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System;
using Config.Common;
using Config;
using FrameWork;
using FrameWork.AssetBundlePackage;
using FrameWork.ResManager;

namespace TaiwuCommunityTranslation
{
    [PluginConfig("Taiwu Community Translation", "Taiwu Mods Community", "0.1.0")]
    public class Mod : TaiwuRemakeHarmonyPlugin
    {
        public static readonly string prefix = @"Languages\en";
        public static bool enableAutoSizing = true;
        public override void Initialize()
        {
            base.Initialize();

            Application.logMessageReceived += Log;
            Debug.Log("!!!!! Taiwu Community Translation loaded");
            if (!Directory.Exists(prefix)) return;
            TranslatorAssistant.AddToGame();
            
        }

        public override void Dispose()
        {
            // Nothing
            Application.logMessageReceived -= Log;
        }

        public void Log(string logString, string stackTrace, LogType type)
        {
            string filename = "EngModLogs.txt";

            try
            {
                System.IO.File.AppendAllText(filename, logString + "\n");
            }
            catch { }
        }


    }

    public class TranslatorAssistant : MonoBehaviour
    {
        public static void AddToGame()
        {
            GameObject go = new GameObject();
            go.name = "Translator Assistant";
            DontDestroyOnLoad(go);
            go.AddComponent<TranslatorAssistant>();
            Debug.Log("Translator assistant successfully added to game.");
        }

        void Start()
        {
            SetUILangauge();
            StartCoroutine(ReloadLangaugePacks());
            GEvent.Add((Enum)UiEvents.OnUIElementShow, new GEvent.Callback(this.OnUIShow));
        }

        public void OnUIShow(ArgumentBox argBox)
        {
            UIElement uiElement;
            if (!argBox.Get<UIElement>("Element", out uiElement))
                return;
            uiElement?.UiBase.GetComponentsInChildren<TextMeshProUGUI>(true).ToList().ForEach(TMPro =>
            {
                AdjustTMPro(TMPro);
            });
        }

        private IEnumerator ReloadLangaugePacks()
        {
            LocalStringManager.Init("zh-CN");
            while (!LocalStringManager.ConfigLanguageInitReady)
                yield return (object)null;
            Debug.Log("About to apply English");
            ApplyEnglishLangauge();
            Task<ParallelLoopResult> initCfgTask = Task.Run<ParallelLoopResult>((Func<ParallelLoopResult>)(() => Parallel.ForEach<IConfigData>((IEnumerable<IConfigData>)ConfigCollection.Items, (Action<IConfigData>)(item => item.Init()))));
            Debug.Log("Application Done"); 
            while (!initCfgTask.IsCompleted)
                yield return (object)null;
            if (initCfgTask.Exception != null)
                throw initCfgTask.Exception;
            LocalStringManager.Release();

            SetUILangauge();

            yield return (object)new WaitForEndOfFrame();
        }

        private void ApplyEnglishLangauge()
        {
            Debug.Log("LSM Manager");
            Debug.Log("!-----------------!");

            
            var file = @"ui_language.json";
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Mod.prefix, file)));
            Debug.Log($"!!!!! ui_language loaded with {dict.Count} entries");
            foreach (var entry in dict)
            {
                if (entry.Value == "") continue;
                Debug.Log($"'{entry.Key},{entry.Value}'");
                var id = LanguageKey.LanguageKeyToId(entry.Key);
                LocalStringManager._localUILanguageArray[id] = entry.Value;
            }
            Debug.Log("!!!!! patched LocalStringManager._localUILanguageArray");


            List<LocalStringManager.LanguagePackData> mapPack = new List<LocalStringManager.LanguagePackData>();
            List<LocalStringManager.LanguagePackData> arrayPack = new List<LocalStringManager.LanguagePackData>();

            LocalStringManager._configLanguageMap.Values.ToList().ForEach(pack =>
            {
                if (pack.ArrayLanguageData != null) arrayPack.Add(pack);
                if (pack.MapLanguageData != null) mapPack.Add(pack);
            });

            Debug.Log(arrayPack.Count);
            arrayPack.ForEach(pack =>
            {
                ApplyArrayPack(pack.PackName);
            });
            Debug.Log(mapPack.Count);
            mapPack.ForEach(pack =>
            {
                ApplyMapPack(pack.PackName);
            });
        }

        private void ApplyArrayPack(string packname)
        {
            if (!File.Exists(Path.Combine(Mod.prefix, packname+".txt"))) return;
            string[] list = File.ReadAllLines(Path.Combine(Mod.prefix, packname + ".txt"));
            for(int i = 0; i < list.Length; i++)
            {
                LocalStringManager._configLanguageMap[packname].ArrayLanguageData[i] = list[i];
            }
        }
        private void ApplyMapPack(string packname)
        {
            if (!File.Exists(Path.Combine(Mod.prefix, packname))) return;
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Mod.prefix, packname)));
            dict.Keys.ToList().ForEach(key =>
            {
                LocalStringManager._configLanguageMap[packname].MapLanguageData[key] = dict[key];
            });
        }

        void AdjustTMPro(TextMeshProUGUI textMesh)
        {
            if(textMesh == null) return;    
            if (Mod.enableAutoSizing)
            {
                textMesh.fontSizeMin = 14;
                textMesh.fontSizeMax = 26;
                textMesh.enableAutoSizing = true;
                textMesh.ForceMeshUpdate(true);
            }
        }

        public void SetUILangauge()
        {
            SingletonObject.getInstance<YieldHelper>().DelayFrameDo(1u, delegate {
                var rootGo = UIManager.Instance.gameObject;
                var textLanguages = rootGo.GetComponentsInChildren<TextLanguage>(true);
                Debug.Log(textLanguages.Length);
                foreach (var tl in textLanguages)
                {
                    tl.SetLanguage();
                    
                }
                Debug.Log(rootGo.GetComponentsInChildren<TextMeshProUGUI>(true).ToList().Count);
                rootGo.GetComponentsInChildren<TextMeshProUGUI>(true).ToList().ForEach(tmpro =>
                {
                    AdjustTMPro(tmpro);
                });
            });

        }
    }

    [HarmonyPatch(typeof(UI_MainMenu), nameof(UI_MainMenu.OnInit))]
    class MainMenuPatch
    {
        static void Postfix()
        {
            Debug.Log("Main Menu harmony patch");
            GameObject bbl = GameObject.Find("/Camera_UIRoot/Canvas/LayerMain/UI_MainMenu/FrontElements/BottomBtnLayout");
            RectTransform[] bblChildrenTransforms = bbl.GetComponentsInChildren<RectTransform>();

            foreach (var transform in bblChildrenTransforms)
            {
                if (transform.gameObject.name == "LabelRoot")
                {
                    Vector2 sizeDelta = transform.sizeDelta;
                    transform.sizeDelta = new Vector2(90, sizeDelta.y);
                }
            }

        }

      
    }

    //Temp fix
    [HarmonyPatch(typeof(UI_GetItem), "InitTitleAndBack")]
    class AddItemFix
    {
        static bool Prefix(UI_GetItem __instance)
        {
            if (__instance._titleList.Count <= 0 || __instance._title.IsNullOrEmpty())
                return false;
            if (__instance._title.Equals(LocalStringManager.Get((ushort)1124)) || __instance._title.Equals(LocalStringManager.Get((ushort)2280)) || __instance._title.Equals(LocalStringManager.Get((ushort)2281)))
                __instance._backIndex = 4;
            else if (__instance._title.Equals(LocalStringManager.Get((ushort)2282)) || __instance._title.Equals(LocalStringManager.Get((ushort)2283)) || __instance._title.Equals(LocalStringManager.Get((ushort)2284)) || __instance._title.Equals(LocalStringManager.Get((ushort)2285)) || __instance._title.Equals(LocalStringManager.Get((ushort)2286)))
                __instance._backIndex = 3;
            else if (__instance._title.Equals(LocalStringManager.Get((ushort)2287)) || __instance._title.Equals(LocalStringManager.Get((ushort)2288)))
                __instance._backIndex = 2;
            else if (__instance._title.Equals(LocalStringManager.Get((ushort)1125)) || __instance._title.Equals(LocalStringManager.Get((ushort)2289)) || __instance._title.Equals(LocalStringManager.Get((ushort)2290)) || __instance._title.Equals(LocalStringManager.Get((ushort)2128)))
                __instance._backIndex = 1;
            else if (__instance._title.Equals(LocalStringManager.Get((ushort)2291)))
                __instance._backIndex = 0;
            CImage image = __instance.CGet<CImage>("Back");
            CImage component = __instance.CGet<RectTransform>("Title").GetChild(0).GetComponent<CImage>();
            ResLoader.Load<Sprite>(Path.Combine("RemakeResources/Textures/GetItem", __instance._backNameList[__instance._backIndex]), (Action<Sprite>)(sprite =>
            {
                image.sprite = sprite;
                image.enabled = true;
            }));
            if (__instance._title.Length > 4)
                __instance._title = __instance._title.Substring(0, 4);
            //Setting this value makes shit not break
            component.SetSprite(string.Format("acquire_mingcheng_{0}", (object)1));
            __instance.CGet<RectTransform>("TopLine").GetComponent<CImage>().SetSprite(string.Format("acquire_bg_{0}_0", (object)__instance._backIndex));
            __instance.CGet<RectTransform>("BottomLine").GetComponent<CImage>().SetSprite(string.Format("acquire_bg_{0}_0", (object)__instance._backIndex));
            return false;
        }
    }
    /*
    [HarmonyPatch(typeof(EventModel), "AdjustDataForDisplay")]
    class TranslateEvents
    {
        static bool Prefix(EventModel __instance)
        {
            Debug.Log(__instance.DisplayingEventData.EventContent);
            return true;
        }
    }*/
}
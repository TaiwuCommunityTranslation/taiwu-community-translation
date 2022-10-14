using Config;
using Config.Common;
using FrameWork;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaiwuModdingLib.Core.Plugin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaiwuCommunityTranslation
{
    [PluginConfig("Taiwu Community Translation", "Taiwu Mods Community", "0.1.0")]
    public class Mod : TaiwuRemakeHarmonyPlugin
    {
        public static readonly string prefix = @"Languages\en";
        public static bool enableAutoSizing = true;
        public static int minFontSize = 16;
        public static int maxFontSize = 24;

        string filename = "EngModLogs.txt";
        public override void Initialize()
        {
            Application.logMessageReceived += Log;
            base.Initialize();
            File.WriteAllText(filename, "Mod loaded \n");
            

            Debug.Log("!!!!! Taiwu Community Translation loaded");
            if (!Directory.Exists(prefix)) return;
            TranslateEvents();
            TranslatorAssistant.AddToGame();


            this.OnModSettingUpdate();
        }

        public override void OnModSettingUpdate()
        {
            base.OnModSettingUpdate();
            ModManager.GetSetting(ModIdStr, "enableAutoSizing", ref enableAutoSizing);
            ModManager.GetSetting(ModIdStr, "fontMin", ref minFontSize);
            ModManager.GetSetting(ModIdStr, "fontMax", ref maxFontSize);
        }


        public override void Dispose()
        {
            // Nothing
            Application.logMessageReceived -= Log;
        }

        public void Log(string logString, string stackTrace, LogType type)
        {
            try
            {
                File.AppendAllText(filename, logString + "\n");
            }
            catch { }
        }

        public bool generateEvent = false;
        public readonly string EventDir = @"Event/EventLanguages";
        public void TranslateEvents()
        {
            Debug.Log("Translating Events");
            DirectoryInfo d = new DirectoryInfo(EventDir); //Assuming Test is your Folder
            Debug.Log("Loading files");
            Dictionary<string, FileInfo> files = d.GetFiles("*.txt").ToDictionary(file => file.Name); //Getting Text files
            Debug.Log("Generating Templates");
            Dictionary<string, TaiWuTemplate> parsedTemplates = files.ToDictionary(f => f.Key, f => new TaiWuTemplate(f.Value));

            //For dev purposes
            if (generateEvent)
            {
                Directory.CreateDirectory(Path.Combine(EventDir, "EventDump"));
                Dictionary<string, string> flatDict = parsedTemplates.Values
                    .ToList()
                    .SelectMany(template => template.FlattenTemplateToDict())
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                File.WriteAllText(Path.Combine(EventDir, "EventDump") + "/events.json", JsonConvert.SerializeObject(flatDict, Formatting.Indented));
            }
            else
            {
                //Write to new files
                Debug.Log("Loading in translated events");
                var file = @"events.json";
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Mod.prefix, file)));

                Debug.Log("Parsing event strings");
                // Apply values to files in memory
                dict.Keys.ToList().ForEach(key =>
                {

                    string[] parsedKey = key.Split('|');
                    if (parsedKey.Length != 3 && parsedKey.Length != 4) Debug.LogError($"A key has been incorrectly parsed:{parsedKey} ");

                    string val = dict[key];
                    if (val == "") return;


                    string fileName = parsedKey[0];
                    string guid = parsedKey[1];
                    string templateKey = parsedKey[2];
                    int parsedIndex = -1;

                    if (parsedKey.Length == 4) parsedIndex = int.Parse(parsedKey[3]);

                    parsedTemplates[fileName].eventMap[guid].ApplyValue(templateKey, val, parsedIndex);
                });

                Debug.Log("Starting event write to files");
                parsedTemplates.Keys.ToList().ForEach(key => {
                    parsedTemplates[key].WriteBackToFile();
                });
            }

        }
        
    }

    public class TranslatorAssistant : MonoBehaviour
    {
        public static TranslatorAssistant Instance { get; private set; }
        public UIManager rootUi;
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
            Instance = this;
            SingletonObject.getInstance<YieldHelper>().DelayFrameDo(1u, delegate
            {
                rootUi = UIManager.Instance;
            });

            StartCoroutine(ReloadLangaugePacks());
            GEvent.Add((Enum)UiEvents.OnUIElementShow, new GEvent.Callback(this.OnUIShow));
            GEvent.Add((Enum)UiEvents.TopUiChanged, new GEvent.Callback(this.OnTopUiChanged));
            GEvent.Add((Enum)EEvents.OnGameStateChange, new GEvent.Callback(this.OnTopUiChanged));
            
        }

        public void OnTopUiChanged(ArgumentBox argBox)
        {
            rootUi?.GetComponentsInChildren<TextMeshProUGUI>(true).ToList().ForEach(TMPro =>
            {
                AdjustTMPro(TMPro);
            });
        }

        bool first = false;
        public void OnUIShow(ArgumentBox argBox)
        {
            if (first == false)
            {
                rootUi?.GetComponentsInChildren<TextMeshProUGUI>(true).ToList().ForEach(TMPro =>
                {
                    AdjustTMPro(TMPro);
                });
                first = true;
            }

            UIElement uiElement;
            if (!argBox.Get<UIElement>("Element", out uiElement))
                return;

            uiElement?.UiBase.GetComponentsInChildren<TextMeshProUGUI>(true);
        }

        private IEnumerator ReloadLangaugePacks()
        {
            LocalStringManager.Init("zh-CN");
            while (!LocalStringManager.ConfigLanguageInitReady)
                yield return (object)null;
            Debug.Log("About to apply English to non-JSON files");
            ApplyEnglishLangauge();
            Task<ParallelLoopResult> initCfgTask = Task.Run<ParallelLoopResult>((Func<ParallelLoopResult>)(() => Parallel.ForEach<IConfigData>((IEnumerable<IConfigData>)ConfigCollection.Items, (Action<IConfigData>)(item => item.Init()))));
            Debug.Log("Application Done");
            while (!initCfgTask.IsCompleted)
                yield return (object)null;
            if (initCfgTask.Exception != null)
                throw initCfgTask.Exception;
            LocalStringManager.Release();

            yield return (object)new WaitForEndOfFrame();
        }

        private void ApplyEnglishLangauge()
        { 
            var file = @"ui_language.json";
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Mod.prefix, file)));
            Debug.Log($"!!!!! ui_language loaded with {dict.Count} entries");
            try
            {
                foreach (var entry in dict)
                {
                    if (entry.Value == "") continue;
                    var id = LanguageKey.LanguageKeyToId(entry.Key);
                    if(LocalStringManager._localUILanguageArray.Length <= id)
                    {
                        Debug.Log($"{id} is out of bounds and ignore. Key: '{entry.Key}', '{entry.Value}' ");
                        continue;
                    }
                    LocalStringManager._localUILanguageArray[id] = entry.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to apply 'ui_language'");
                Debug.LogError(ex.ToString());
            }
            Debug.Log("!!!!! patched LocalStringManager._localUILanguageArray");


            List<LocalStringManager.LanguagePackData> mapPack = new List<LocalStringManager.LanguagePackData>();
            List<LocalStringManager.LanguagePackData> arrayPack = new List<LocalStringManager.LanguagePackData>();

            LocalStringManager._configLanguageMap.Values.ToList().ForEach(pack =>
            {
                if (pack.ArrayLanguageData != null) arrayPack.Add(pack);
                if (pack.MapLanguageData != null) mapPack.Add(pack);
            });


            arrayPack.ForEach(pack =>
            {
                ApplyArrayPack(pack.PackName);
            });

            mapPack.ForEach(pack =>
            {
                ApplyMapPack(pack.PackName);
            });
        }

        private void ApplyArrayPack(string packname)
        {
            if (!File.Exists(Path.Combine(Mod.prefix, packname + ".txt"))) return;
            string[] list = File.ReadAllLines(Path.Combine(Mod.prefix, packname + ".txt"));
            for (int i = 0; i < list.Length; i++)
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

        public void AdjustTMPro(TextMeshProUGUI textMesh)
        {
            if (textMesh == null) return;
            if (Mod.enableAutoSizing)
            {
                textMesh.fontSizeMin = 16;
                textMesh.fontSizeMax = 28;
                textMesh.enableAutoSizing = true;
                //textMesh.ForceMeshUpdate(true);
            }
        }

        public static void ResizeAndRealignText(TextMeshProUGUI t, Vector2 size, bool includeParent, bool repositionParent = false)
        {
            if (includeParent)
            {
                RectTransform parentTransform = (t.rectTransform.parent as RectTransform);
                if (repositionParent)
                {
                    Vector2 diff = size - parentTransform.sizeDelta;
                    parentTransform.offsetMin -= diff * new Vector2(1, -1);
                    parentTransform.offsetMax -= diff * new Vector2(1, -1);
                }

                parentTransform.sizeDelta = size;

                t.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                t.rectTransform.localPosition = new Vector2(0, 0);
            }
            t.alignment = TextAlignmentOptions.Center;
            t.verticalAlignment = VerticalAlignmentOptions.Middle;
            t.enableWordWrapping = true;
            t.rectTransform.offsetMax = Vector2.zero;
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.sizeDelta = size;
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


    [HarmonyPatch(typeof(UI_NewGame), nameof(UI_NewGame.Start))]
    class NewGameFix
    {
        static void Postfix()
        {
            Debug.Log("!!!Harmony!!! - UI_NewGame Patched");
            UI_NewGame newGameUi = TranslatorAssistant.Instance.rootUi
                .GetComponentInChildren<UI_NewGame>();

            newGameUi.GetComponentsInChildren<TextMeshProUGUI>(true)
                .ToList()
                .ForEach(t => TranslatorAssistant.Instance.AdjustTMPro(t));

            FixWorldMapLabels();

        }

        static void FixWorldMapLabels()
        {
            PartWorldView pwv = TranslatorAssistant.Instance.rootUi.GetComponentInChildren<PartWorldView>(true);

            pwv.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(t => t.gameObject.name == "CityName" || t.gameObject.name == "ReligionName")
                .ToList()
                .ForEach(t =>
                {
                    TranslatorAssistant.ResizeAndRealignText(t, new Vector2(200, 80), true);
                });
        }


    }

    [HarmonyPatch(typeof(GlobalConfig), nameof(GlobalConfig.Init))]
    static class GlobalConfigPatch
    {
        static void Postfix()
        {
            Debug.Log("Overwriting config settings");
            GlobalConfig.Instance.NameLengthConfig_CN = new byte[2] { 6, 6 };
        }
    }
    
    [HarmonyPatch(typeof(MouseTipBase), nameof(MouseTipBase.OnInit))]
    static class MouseTipBasePatch
    {
        static void Postfix(MouseTipBase __instance)
        {
            Debug.Log("Activated");
            __instance.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(t => t.gameObject.name == "EffectTips" )
            .ToList()
            .ForEach(t =>
            {
                TranslatorAssistant.ResizeAndRealignText(t, new Vector2(400, 80), true);
            });
            
        }
    }
    
    [HarmonyPatch(typeof(ItemView), nameof(ItemView.SetData))]
    static class ItemItemViewPatch
    {
        static void Postfix(ItemView __instance)
        {
            __instance.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(x => x.name == "Type")
                .ToList()
                .ForEach((x) =>
                {
                    TranslatorAssistant.ResizeAndRealignText(x, new Vector2(100, 40), true, true);
                });
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

/*TO-DO
 * 
 * WIDEN SELECTION TAB
 * Camera_UIRoot/Canvas/LayerPopUp/UI_CharacterMenuLifeSkill/ElementsRoot/Detail/TopBack/SkillTypeTogGroup/SkillTypeTog0 (15)
 * ClassName: CTOG, CIMAGE
 */
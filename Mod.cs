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
            TranslateEvents();
            TranslatorAssistant.AddToGame();
        }

        public override void OnModSettingUpdate()
        {
            base.OnModSettingUpdate();
            Debug.Log("Hello World");
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

        public bool generateEvent = false;
        public readonly string EventDir = @"Event/EventLanguages";
        public void TranslateEvents()
        {
            Debug.Log("Translating Events");
            DirectoryInfo d = new DirectoryInfo(EventDir); //Assuming Test is your Folder
            Debug.Log(d.FullName);
            Dictionary<string, FileInfo> files = d.GetFiles("*.txt").ToDictionary(file => file.Name); //Getting Text files
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
                var file = @"events.json";
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Mod.prefix, file)));

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

                    Debug.Log(key);
                    if (parsedKey.Length == 4) parsedIndex = int.Parse(parsedKey[3]);

                    parsedTemplates[fileName].eventMap[guid].ApplyValue(templateKey, val, parsedIndex);
                });

                Debug.Log("Starting file write");
                parsedTemplates.Keys.ToList().ForEach(key => {
                    parsedTemplates[key].WriteBackToFile();
                });
            }

        }

        public class TaiWuTemplate {

            string fileName = "";
            public List<EventData> eventData = new List<EventData>();
            public Dictionary<string, EventData> eventMap = new Dictionary<string, EventData>();

            FileInfo file;
            public TaiWuTemplate(FileInfo file)
            {
                EventData activeEventData = null;
                fileName = file.Name;
                this.file = file;
                Debug.Log($"Generating Template For {fileName}");
                StreamReader textStream = file.OpenText();
                while (!textStream.EndOfStream)
                {
                    string line = textStream.ReadLine();
                    if (line.Length == 0) continue;
                    int index = line.IndexOf(':');
                    string key = line.Substring(0, index);
                    string value = line.Substring(index+2); //Skip the ':' and the whitespace

                    switch(new string(key.Where(Char.IsLetter).ToArray()))
                    {
                        case "EventGuid":
                            if (activeEventData != null)
                            {
                                eventData.Add(activeEventData);
                            }
                            activeEventData = new EventData() { guid = value };
                            break;
                        case "EventContent":
                            activeEventData.content = value;
                            break;
                        case "Option":
                            activeEventData.options.Add(value);
                            break;
                    }
                }
                textStream.Close();
                //Adds the last one
                eventData.Add(activeEventData);

                eventMap = eventData.ToDictionary(eventData => eventData.guid);
            }

            public void WriteBackToFile()
            {
                StringBuilder sb = new StringBuilder();
                StreamReader textStream = file.OpenText();
                EventData activeEventData = null;
                while (!textStream.EndOfStream)
                {
                    string line = textStream.ReadLine();
                    string lineToWrite = line;

                    if (line.Length == 0)
                    {
                        sb.AppendLine();
                        continue;
                    }

                    int index = line.IndexOf(':');
                    string key = line.Substring(0, index);
                    string value = line.Substring(index + 2); //Skip the ':' and the whitespace

                    switch (new string(key.Where(Char.IsLetter).ToArray()))
                    {
                        case "EventGuid":
                            activeEventData = eventMap[value];
                            break;
                        case "EventContent":
                            lineToWrite = activeEventData.GenerateContentString();
                            break;
                        case "Option":
                            lineToWrite = activeEventData.GenerateOptString(new string(key.Where(Char.IsDigit).ToArray()));
                            break;
                    }
                    if(lineToWrite != "")
                    {
                        sb.AppendLine(lineToWrite);
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                textStream.Close();
                File.WriteAllText(file.FullName, sb.ToString());
            }


            public Dictionary<string, string> FlattenTemplateToDict()
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                eventData.ForEach(x =>
                {
                    string baseString = $"{fileName}|{x.guid}";
                    if (x.content != null || x.content != "" || x.content != " ") dict.Add($"{baseString}|EventContent", x.content);
                    for (int i = 0; i < x.options.Count; i++)
                    {
                        dict.Add($"{baseString}|Option|{i}", x.options[i]);
                    }
                });
                return dict;
            }
        }

        public class EventData
        {
            public string guid;
            public string content = "";
            public List<string> options = new List<string>();

            public void ApplyValue(string templateKey, string val, int index = -1)
            {
                if (val == "") return;
                if(templateKey.Contains("Option") && index > -1)
                {
                    options[index] = " "+val;
                    return;
                }
                else if(templateKey == "EventContent")
                {
                    content = " "+val;
                    return;
                }
                Debug.LogError($"Ya fucked up son, ${guid} | {templateKey} , index: {index}");
            }

            public string GenerateOptString(string i)
            {
                string val = options[int.Parse(i)-1];
                if (val == "") return "";


                return $"		-- Option_{i} :{val}";
            }

            public string GenerateContentString()
            {
                if (content == "") return "";


                return $"		-- EventContent :{content}";
            }
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
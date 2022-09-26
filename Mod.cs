using System.Reflection;
using System.IO;
using TaiwuModdingLib.Core.Plugin;
using TMPro;
using UnityEngine;
using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TaiwuCommunityTranslation
{
    [PluginConfig("Taiwu Community Translation", "Taiwu Mods Community", "0.1.0")]
    public class Mod : TaiwuRemakeHarmonyPlugin
    {
        public bool enableAutoSizing = true;
        public override void Initialize()
        {
            base.Initialize();

            Application.logMessageReceived += Log;
            Debug.Log("!!!!! Taiwu Community Translation loaded");

            var prefix = @"Languages\en";
            if (!Directory.Exists(prefix)) return;

            var file = @"ui_language.json";
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(prefix, file)));

            Debug.Log($"!!!!! ui_language loaded with {dict.Count} entries");

            var lsm = typeof(LocalStringManager);
            var uiLanguageField = lsm.GetField("_localUILanguageArray", BindingFlags.NonPublic | BindingFlags.Static);
            var lines = uiLanguageField.GetValue(null) as string[];
            foreach (var entry in dict)
            {
                if (entry.Value == "") continue;

                var id = LanguageKey.LanguageKeyToId(entry.Key);
                lines[id] = entry.Value;
            }
            // uiLanguageField.SetValue(null, lines);

            Debug.Log("!!!!! patched LocalStringManager._localUILanguageArray");

            // load translation
            SingletonObject.getInstance<YieldHelper>().DelayFrameDo(1u, delegate {
                var rootGo = UIManager.Instance.gameObject;
                var textLanguages = rootGo.GetComponentsInChildren<TextLanguage>(true);
                foreach (var tl in textLanguages)
                {
                    tl.SetLanguage();
                    AdjustTMPro(tl.gameObject.GetComponent<TextMeshProUGUI>());
                }
            });

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

        void AdjustTMPro(TextMeshProUGUI textMesh)
        {
            if (enableAutoSizing)
            {
                textMesh.fontSizeMin = 12;
                textMesh.fontSizeMax = 24;
                textMesh.enableAutoSizing = true;
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

        void Update()
        {

        }
    }

    [HarmonyPatch(typeof(UI_MainMenu), nameof(UI_MainMenu.OnInit))]
    class MainMenuPatch
    {
        static void Postfix()
        {
            // fix bottom buttons width
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
}
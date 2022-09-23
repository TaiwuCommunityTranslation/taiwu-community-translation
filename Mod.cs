using System.Reflection;
using System.IO;
using TaiwuModdingLib.Core.Plugin;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TaiwuCommunityTranslation
{
    [PluginConfig("Taiwu Community Translation", "Taiwu Mods Community", "0.1.0")]
    public class Mod : TaiwuRemakeHarmonyPlugin
    {
        public override void Initialize()
        {
            base.Initialize();

            Application.logMessageReceived += Log;
            Debug.Log("!!!!! Taiwu Community Translation loaded");

            var prefix = @"Languages\en";
            if (!Directory.Exists(prefix)) return;

            var file = @"ui_language.txt";
            var lines = File.ReadAllText(Path.Combine(prefix, file)).Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = lines[i].Replace("\\n", "\n");

            Debug.Log($"!!!!! ui_language loaded with {lines.Length} lines");

            var lsm = typeof(LocalStringManager);
            var uiLanguageField = lsm.GetField("_localUILanguageArray", BindingFlags.NonPublic | BindingFlags.Static);
            uiLanguageField.SetValue(null, lines);

            Debug.Log("!!!!! cleared LocalStringManager._localUILanguageArray");

            Debug.Log("??????");

            // TODO: start scrolling animation
            SingletonObject.getInstance<YieldHelper>().DelayFrameDo(1u, delegate {
                var rootGo = UIManager.Instance.gameObject;
                var textLanguages = rootGo.GetComponentsInChildren<TextLanguage>(true);
                foreach (var tl in textLanguages)
                {
                    tl.SetLanguage();

                    continue; // DISABLE UI FIX

                    var labelGo = tl.gameObject;
                    var labelTransform = labelGo.transform as RectTransform;

                    // set nowrap
                    var tmp = labelGo.GetComponent<TextMeshProUGUI>();
                    tmp.enableWordWrapping = false;

                    // add container
                    var backGo = labelTransform.parent.gameObject;
                    var backTransform = backGo.transform as RectTransform;
                    if (backGo.GetComponent<Mask>() == null) backGo.AddComponent(typeof(Mask));
                    if (backGo.GetComponent<Image>() == null && backGo.GetComponent<Graphic>() == null)
                    {
                        backGo.AddComponent(typeof(CImage));
                        var cimg = backGo.GetComponent<CImage>();
                        cimg.SetAlpha(0.01f);
                    }

                    // ignore fitted game objects
                    if (tmp.renderedWidth < backTransform.sizeDelta.x) continue;

                    float initX = (tmp.renderedWidth - backTransform.sizeDelta.x) / 2f + 4f;
                    var pos = new Vector2(initX, labelTransform.anchoredPosition.y);
                    labelTransform.anchoredPosition = pos;

                    var sequence = DOTween.Sequence();
                    sequence.Append(labelTransform.DOAnchorPosX(0f - initX, tmp.text.Length * 0.1f).SetEase(Ease.Linear).SetUpdate(isIndependentUpdate: true));
                    sequence.AppendInterval(2f);
                    sequence.AppendCallback(delegate {
                        labelTransform.anchoredPosition = pos;
                    });
                    sequence.AppendInterval(2f);
                    sequence.SetUpdate(isIndependentUpdate: true);
                    // sequence.SetLoops(-1);
                    sequence.SetLoops(3); // FIXME
                    sequence.Play();
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
    }


    public class TranslatorAssistant : MonoBehaviour
    {
        public bool enableAutoSizing = true;
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
            System.Array.ForEach(FindObjectsOfType<TextMeshProUGUI>(), AdjustTMPro);
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
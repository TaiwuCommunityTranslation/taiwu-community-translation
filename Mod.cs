using System.Reflection;
using System.IO;
using TaiwuModdingLib.Core.Plugin;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaiwuCommunityTranslation
{
    [PluginConfig("Taiwu Community Translation", "Taiwu Mods Community", "0.1.0")]
    public class Mod : TaiwuRemakePlugin  
    {
        public override void Initialize()
        {
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

            // fix bottom buttons width
            //var bbl = GameObject.Find("/Camera_UIRoot/Canvas/LayerMain/UI_MainMenu/FrontElements/BottomBtnLayout");
            //var bblChildrenTransforms = bbl.GetComponentsInChildren<RectTransform>();
            //foreach (var transform in bblChildrenTransforms)
            //{
            //    if (transform.gameObject.name == "LabelRoot")
            //    {
            //        var sizeDelta = transform.sizeDelta;
            //        sizeDelta.x = 90;
            //        transform.sizeDelta = sizeDelta;
            //    }
            //}

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
        }

        public override void Dispose()
        {
            // Nothing
        }
    }
}
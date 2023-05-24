using AWK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using UnityEngine.Events;
using UnityEngine;

namespace AwkwardMP
{
    public static class Helper
    {
        public const int PAD = 10;
        public const int MARGIN = 6;
        public const int FONT_SIZE = 12;
        public const int FONT_SIZE_MIN = 10;
        public const int FONT_SIZE_MAX = 75;

        static UnityEngine.Font _font;
        public static UnityEngine.Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");

                }
                return _font;
            }
        }

        const int R = 64;

        static UnityEngine.Texture2D _circle32Texture;
        static UnityEngine.Texture2D Circle32Texture
        {
            get
            {
                if (_circle32Texture == null)
                {
                    var tex = new UnityEngine.Texture2D(R * 2, R * 2);
                    for (int x = 0; x < R; ++x)
                    {
                        for (int y = 0; y < R; ++y)
                        {
                            double h = System.Math.Abs(System.Math.Sqrt(x * x + y * y));
                            float a = h > R ? 0.0f : h < (R - 1) ? 1.0f : (float)(R - h);
                            var c = new UnityEngine.Color(1.0f, 1.0f, 1.0f, a);
                            tex.SetPixel(R + 0 + x, R + 0 + y, c);
                            tex.SetPixel(R - 1 - x, R + 0 + y, c);
                            tex.SetPixel(R + 0 + x, R - 1 - y, c);
                            tex.SetPixel(R - 1 - x, R - 1 - y, c);

                        }
                    }
                    tex.Apply();
                    return _circle32Texture = tex;
                }
                return _circle32Texture;
            }
        }

        static UnityEngine.Sprite _circle32Sprite;
        public static UnityEngine.Sprite CircleSprite
        {
            get
            {
                if (_circle32Sprite == null)
                {
                    _circle32Sprite = UnityEngine.Sprite.Create(Circle32Texture, new UnityEngine.Rect(0, 0, R * 2, R * 2), new UnityEngine.Vector2(R, R), 10f, 0, UnityEngine.SpriteMeshType.Tight, new UnityEngine.Vector4(R - 1, R - 1, R - 1, R - 1));
                }
                return _circle32Sprite;
            }
        }

        public static UnityEngine.Color DARK_GREEN = new UnityEngine.Color(0.0f, 0.5f, 0.0f, 1.0f);
        public static UnityEngine.Color DARK_BLUE = new UnityEngine.Color(0.0f, 0.0f, 0.5f, 1.0f);
        public static UnityEngine.Color DARK_RED = new UnityEngine.Color(0.5f, 0.0f, 0.0f, 1.0f);
        public static UnityEngine.Color WHITE = new UnityEngine.Color(1f, 1f, 1f, 1.0f);


        public static UnityEngine.RectTransform CreateRectTransform(this UnityEngine.Transform parent, string name, bool expand = false)
        {
            var go = new UnityEngine.GameObject(name);
            var rt = go.AddComponent<UnityEngine.RectTransform>();
            rt.SetParent(parent);
            rt.localPosition = default;
            rt.localScale = default;
            rt.localScale = new UnityEngine.Vector3(1, 1, 1);

            if (expand)
            {
                ExpandAnchor(rt);
            }
            return rt;
        }

        public static UnityEngine.UI.Dropdown CreateDropdown(this UnityEngine.RectTransform rt, string name, float padding, UnityEngine.Color fontColor)
        {
            var dropRT = rt.CreateRectTransform(name)
              .ExpandAnchor(-MARGIN);

            var dropimg = dropRT.gameObject.AddComponent<UnityEngine.UI.Image>();
            var dropdown = dropRT.gameObject.AddComponent<UnityEngine.UI.Dropdown>();
            dropimg.color = new UnityEngine.Color(0, 0, 0, 0);
            dropdown.image = dropimg;

            var templateRT = dropRT.CreateRectTransform("Template", true)
                .ExpandTopAnchor()
                .SetOffsets(0, 0, -150, 0);

            var contentRT = templateRT.CreateRectTransform("Content")
                .ExpandTopAnchor()
                .SetOffsets(0, 0, -150, 0);
     
            var itemRT = contentRT.CreateRectTransform("Item", true)
              .SetAnchors(0, 1, 1, 1)
              .SetPivot(0.5f, 1)
              .SetSizeDelta(0, 50);

            var toggle = itemRT.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.colors = new UnityEngine.UI.ColorBlock()
            {
                colorMultiplier = 1,
                normalColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f, 1f),
                highlightedColor = new UnityEngine.Color(.3f, .3f, .3f, 1f),
                pressedColor = new UnityEngine.Color(.4f, .4f, .4f, 4f),
            };
            var itemBackRT = itemRT.CreateRectTransform("Item Background", true);
            var itemBack = itemBackRT.gameObject.AddComponent<UnityEngine.UI.Image>();

            
            var itemLablRT = itemRT.CreateRectTransform("Item Label", true)
              .SetAnchors(0.15f, 0.9f, 0.1f, 0.9f)
              .SetOffsets(0, 0, 0, 0);

            var itemLabl = itemLablRT.AddText("Sample", UnityEngine.TextAnchor.UpperLeft, fontColor);
            itemLabl.alignment = UnityEngine.TextAnchor.MiddleLeft;
            itemLabl.resizeTextMaxSize = 24;

            toggle.targetGraphic = itemBack;
            toggle.isOn = true;

            dropdown.template = templateRT;
            dropdown.itemText = itemLabl;


            templateRT.gameObject.SetActive(false);
            return dropdown;
        }

        public static UnityEngine.UI.Text AddText(this UnityEngine.RectTransform rt, string label, UnityEngine.TextAnchor anchor, UnityEngine.Color FontColor)
        {
            var text = rt.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.text = label;
            text.color = FontColor;
            text.font = Font;
            text.alignment = anchor;
            text.fontSize = FONT_SIZE;
            text.raycastTarget = false;
            //text.alignByGeometry   = true;
            text.resizeTextMinSize = FONT_SIZE_MIN;
            text.resizeTextMaxSize = FONT_SIZE_MAX;
            text.resizeTextForBestFit = true;
            return text;
        }

        public static UnityEngine.RectTransform AddImage(this UnityEngine.RectTransform rt, UnityEngine.Color color)
        {
            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        public static UnityEngine.RectTransform ExpandAnchor(this UnityEngine.RectTransform rt, float? padding = null)
        {
            rt.anchorMax = new UnityEngine.Vector2(1, 1);
            rt.anchorMin = new UnityEngine.Vector2(0, 0);
            rt.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
            if (padding.HasValue)
            {
                rt.offsetMin = new UnityEngine.Vector2(padding.Value, padding.Value);
                rt.offsetMax = new UnityEngine.Vector2(-padding.Value, -padding.Value);
            }
            else
            {
                rt.sizeDelta = default;
                rt.anchoredPosition = default;
            }
            return rt;
        }

        public static UnityEngine.RectTransform ExpandTopAnchor(this UnityEngine.RectTransform rt, float? padding = null)
        {
            rt.anchorMax = new UnityEngine.Vector2(1, 1);
            rt.anchorMin = new UnityEngine.Vector2(0, 1);
            rt.pivot = new UnityEngine.Vector2(0.5f, 1f);
            if (padding.HasValue)
            {
                rt.offsetMin = new UnityEngine.Vector2(padding.Value, padding.Value);
                rt.offsetMax = new UnityEngine.Vector2(-padding.Value, -padding.Value);
            }
            else
            {
                rt.sizeDelta = default;
                rt.anchoredPosition = default;
            }
            return rt;
        }

        public static UnityEngine.RectTransform ExpandMiddleLeft(this UnityEngine.RectTransform rt)
        {
            rt.anchorMax = new UnityEngine.Vector2(0, 0.5f);
            rt.anchorMin = new UnityEngine.Vector2(0, 0.5f);
            rt.pivot = new UnityEngine.Vector2(0.0f, .5f);
            return rt;
        }

        public static UnityEngine.RectTransform SetSizeDelta(this UnityEngine.RectTransform rt, float offsetX, float offsetY)
        {
            rt.sizeDelta = new UnityEngine.Vector2(offsetX, offsetY);
            return rt;
        }


        public static UnityEngine.RectTransform SetOffsets(this UnityEngine.RectTransform rt, float minX, float maxX, float minY, float maxY)
        {
            rt.offsetMin = new UnityEngine.Vector2(minX, minY);
            rt.offsetMax = new UnityEngine.Vector2(maxX, maxY);
            return rt;
        }

        public static UnityEngine.RectTransform SetPivot(this UnityEngine.RectTransform rt, float pivotX, float pivotY)
        {
            rt.pivot = new UnityEngine.Vector2(pivotX, pivotY);
            return rt;
        }

        public static UnityEngine.RectTransform SetAnchors(this UnityEngine.RectTransform rt, float minX, float maxX, float minY, float maxY)
        {
            rt.anchorMin = new UnityEngine.Vector2(minX, minY);
            rt.anchorMax = new UnityEngine.Vector2(maxX, maxY);
            return rt;
        }

        public const float BTTN_LBL_NORM_HGHT = .175f;
        private const int BTTN_FONT_SIZE_MAX = 100;
        private const float BTTN_ALPHA = 0.925f;

        internal static void MakeButton(this RectTransform parent, ref UnityEngine.UI.Button button, string iconText, string labelText, out UnityEngine.UI.Text icon, out UnityEngine.UI.Text text, UnityAction action)
        {
            var rt = parent.CreateRectTransform(labelText);
            button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();

            var iconRt = rt.CreateRectTransform("Icon", true);
            iconRt.anchorMin = new Vector2(0, BTTN_LBL_NORM_HGHT);
            iconRt.anchorMax = new Vector2(1, 1.0f);
            iconRt.offsetMin = new Vector2(0, 0);
            iconRt.offsetMax = new Vector2(0, 0);

            icon = iconRt.gameObject.AddComponent<UnityEngine.UI.Text>();
            button.targetGraphic = icon;
            icon.font = Font;
            icon.text = iconText;
            icon.alignment = TextAnchor.MiddleCenter;
            icon.fontStyle = FontStyle.Bold;
            icon.fontSize = BTTN_FONT_SIZE_MAX;
            icon.resizeTextMinSize = 0;
            icon.resizeTextMaxSize = BTTN_FONT_SIZE_MAX;
            icon.alignByGeometry = true;
            icon.resizeTextForBestFit = true;

            var textRt = rt.CreateRectTransform("Label", true);
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, BTTN_LBL_NORM_HGHT);
            textRt.pivot = new Vector2(.5f, BTTN_LBL_NORM_HGHT * .5f);
            textRt.offsetMin = new Vector2(0, 0);
            textRt.offsetMax = new Vector2(0, 0);

            text = textRt.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.color = WHITE;
            text.font = Font;
            text.text = labelText;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 0;
            text.resizeTextMinSize = 0;
            text.resizeTextMaxSize = BTTN_FONT_SIZE_MAX;
            text.resizeTextForBestFit = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, BTTN_ALPHA);
            colors.pressedColor = new Color(.3f, .3f, .3f, BTTN_ALPHA);
            colors.highlightedColor = new Color(.5f, .5f, .5f, BTTN_ALPHA);
            button.colors = colors;

            button.onClick.AddListener(action);
        }

        public static Dictionary<string, UnityEngine.Sprite> CachedSprites = new();

        public static UnityEngine.Sprite ConnectedSprite;
        public static UnityEngine.Sprite DisconnectedSprite;

        public static UnityEngine.Sprite GetConnectedSprite()
        {
            if (ConnectedSprite) return ConnectedSprite;
            return ConnectedSprite = loadSpriteFromResources("AwkwardMP.Resources.Tick.png", 150f);
        }

        public static UnityEngine.Sprite GetDisconnectedSprite()
        {
            if (DisconnectedSprite) return DisconnectedSprite;
            return DisconnectedSprite = loadSpriteFromResources("AwkwardMP.Resources.Cross.png", 150f);
        }

        public static UnityEngine.Sprite loadSpriteFromResources(string path, float pixelsPerUnit)
        {
            try
            {
                if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
                UnityEngine.Texture2D texture = loadTextureFromResources(path);
                sprite = UnityEngine.Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), new UnityEngine.Vector2(0.5f, 0.5f), pixelsPerUnit);
                sprite.hideFlags |= UnityEngine.HideFlags.HideAndDontSave | UnityEngine.HideFlags.DontSaveInEditor;
                return CachedSprites[path + pixelsPerUnit] = sprite;
            }
            catch
            {
                AwkwardMP.Log.LogError("Error loading sprite from path: " + path);
            }
            return null;
        }

        public static unsafe UnityEngine.Texture2D loadTextureFromResources(string path)
        {
            try
            {
                UnityEngine.Texture2D texture = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.ARGB32, true);
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(path);
                var length = stream.Length;
                byte[] byteTexture = new byte[stream.Length];
                stream.Read(byteTexture, 0, byteTexture.Length);

                texture.LoadImage(byteTexture, false);
                return texture;
            }
            catch
            {
                AwkwardMP.Log.LogError("Error loading texture from resources: " + path);
            }
            return null;
        }

        private static NotificationPopups.PopupId _lastpopUpId;
        public static void ShowPopupYesNo(string text, string btnConfirmText, string btnCancelText, InputManager.InputDelegate onConfirm, InputManager.InputDelegate onCancel, out TMPro.TextMeshProUGUI _popUpText, out AWK.MenuButton btnConfirm, out AWK.MenuButton btnCancel)
        {
            Globals.NotificationPopups.ShowPopup(NotificationPopups.PopupId.SignedOut, onConfirm, onCancel, null, null, null);

            _popUpText = GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler/NotificationPopups(Clone)/SystemPopup/MessageText").GetComponent<TMPro.TextMeshProUGUI>();

            btnConfirm = GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler/NotificationPopups(Clone)/SystemPopup/ButtonRoot/NotificationButton_0").GetComponent<AWK.MenuButton>();
            btnCancel = GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler/NotificationPopups(Clone)/SystemPopup/ButtonRoot/NotificationButton_1").GetComponent<AWK.MenuButton>();

            _popUpText.text = text; 
            btnConfirm.SetTextFromData(btnConfirmText);
            btnCancel.SetTextFromData(btnCancelText);

            _lastpopUpId = NotificationPopups.PopupId.SignedOut;
        }

        public static void ShowPopupOk(string text, string btnConfirmText, InputManager.InputDelegate onConfirm, out TMPro.TextMeshProUGUI _popUpText, out AWK.MenuButton btnConfirm)
        {
            Globals.NotificationPopups.ShowPopup(NotificationPopups.PopupId.FailedToConnect, onConfirm, null, null, null, null);

            _popUpText = GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler/NotificationPopups(Clone)/SystemPopup/MessageText").GetComponent<TMPro.TextMeshProUGUI>();

            btnConfirm = GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler/NotificationPopups(Clone)/SystemPopup/ButtonRoot/NotificationButton_0").GetComponent<AWK.MenuButton>();

            _popUpText.text = text;
            btnConfirm.SetTextFromData(btnConfirmText);

            _lastpopUpId = NotificationPopups.PopupId.FailedToConnect;
        }

        public static void HidePopup()
        {
            Globals.NotificationPopups.HidePopup(_lastpopUpId);
        }










        public static object CurrentQuestion()
        {
            object _params = new
            {
                title = "",
                answerA = "",
                answerB = "",
            };

            try
            {
                QuestionState question = Globals.GameState.GetCurrentQuestion();
                _params = new
                {
                    title = question.QuestionText,
                    answerA = question.Answer1Text,
                    answerB = question.Answer2Text,
                };

                return _params;

            } catch (Exception ex){
                AwkwardMP.Log.LogError(ex);
                return _params; 
            }
        }
    }
}

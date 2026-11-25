using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // Shared state for in-game windows (backpack, tool locker) that need the
    // mouse cursor while the expedition keeps running.
    public static class GameUI
    {
        public interface IWindow { bool IsOpen { get; } void Close(); }
        static readonly List<IWindow> windows = new List<IWindow>();

        public static void Register(IWindow w) { if (!windows.Contains(w)) windows.Add(w); }
        public static void Unregister(IWindow w) { windows.Remove(w); }

        public static bool CapturesInput
        {
            get { foreach (var w in windows) if (w != null && w.IsOpen) return true; return false; }
        }

        // Escape closes the newest open window before it can open the pause menu.
        public static bool CloseTopmost()
        {
            for (int i = windows.Count - 1; i >= 0; i--)
                if (windows[i] != null && windows[i].IsOpen) { windows[i].Close(); RefreshCursor(); return true; }
            return false;
        }

        static int closedFrame = -10;
        public static void MarkClosed() => closedFrame = Time.frameCount;
        // True on the frame a window closed and the next, so its close key is not re-used as INTERACT.
        public static bool JustClosed => Time.frameCount - closedFrame <= 1;

        public static void RefreshCursor()
        {
            var g = Expedition.Instance;
            bool free = CapturesInput || !g || !g.IsPlaying || g.Paused;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }
    }
}

namespace TinyGiantProblems
{
    using TMPro;
    using UnityEngine.UI;

    // Code-built uGUI helpers matching ExpeditionHUD's storybook palette.
    public static class UIKit
    {
        public static readonly Color Ink = new Color(.045f, .105f, .12f, .94f);
        public static readonly Color InkLight = new Color(.09f, .18f, .19f, .96f);
        public static readonly Color Cream = new Color(.96f, .94f, .82f);
        public static readonly Color Gold = new Color(1, .73f, .29f);
        public static readonly Color Muted = new Color(.7f, .81f, .77f);
        public static readonly Color Good = new Color(.53f, .85f, .63f);
        public static readonly Color Bad = new Color(1f, .45f, .4f);
        public static readonly Color ButtonColor = new Color(.22f, .39f, .35f);

        public static TMP_FontAsset Font
        {
            get
            {
                var hud = Object.FindAnyObjectByType<ExpeditionHUD>();
                if (hud && hud.font) return hud.font;
                return TMP_Settings.defaultFontAsset;
            }
        }

        public static RectTransform MakeCanvas(string name, int order, out Canvas canvas)
        {
            var go = new GameObject(name, typeof(RectTransform));
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position; rt.sizeDelta = size;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; Place(rt, anchor, position, size); return rt;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, bool raycast = false)
        {
            var rt = Rect(parent, name, anchor, position, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color; img.raycastTarget = raycast;
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Rect(parent, "Label", anchor, position, dimensions);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.raycastTarget = false;
            t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal; t.richText = true;
            return t;
        }

        public static Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, out TextMeshProUGUI text)
        {
            var img = Panel(parent, label, anchor, position, size, ButtonColor, true);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img; b.onClick.AddListener(() => ProceduralSfx.Play(ProceduralSfx.Click, .5f)); b.onClick.AddListener(action);
            var colors = b.colors; colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f); colors.pressedColor = new Color(.8f, .8f, .8f); b.colors = colors;
            text = Label(img.transform, label, 17, Cream, new Vector2(.5f, .5f), Vector2.zero, size, TextAlignmentOptions.Center);
            return b;
        }

        // Square item tile: colour swatch, glyph, optional count.
        public static void Icon(Transform parent, ItemDef def, float size, out Image swatch, out TextMeshProUGUI glyph)
        {
            swatch = Panel(parent, "Swatch", new Vector2(.5f, .5f), Vector2.zero, Vector2.one * size, def != null ? def.color : Color.clear);
            var shade = Panel(swatch.transform, "Shade", new Vector2(.5f, 0), Vector2.zero, new Vector2(size, size * .38f), new Color(0, 0, 0, .18f));
            shade.rectTransform.pivot = new Vector2(.5f, 0);
            glyph = Label(swatch.transform, def != null ? def.glyph : "", size * .42f, new Color(.08f, .1f, .1f), new Vector2(.5f, .5f), Vector2.zero, Vector2.one * size, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            var picture = Panel(swatch.transform, "Picture", new Vector2(.5f, .5f), Vector2.zero, Vector2.one * size * .96f, Color.white);
            picture.preserveAspect = true; picture.enabled = false;
            SetIcon(swatch, glyph, def);
        }

        // Shows the item's picture (ItemIcons) over a soft tint of its colour, or
        // the letter tile when it has no model. Safe to call every refresh.
        public static void SetIcon(Image swatch, TextMeshProUGUI glyph, ItemDef def)
        {
            var pictureT = swatch ? swatch.transform.Find("Picture") : null;
            var picture = pictureT ? pictureT.GetComponent<Image>() : null;
            var sprite = def != null ? ItemIcons.Get(def) : null;
            if (picture) { picture.sprite = sprite; picture.enabled = sprite; }
            if (def == null) { swatch.color = new Color(0, 0, 0, 0); glyph.text = ""; return; }
            swatch.color = sprite ? new Color(def.color.r, def.color.g, def.color.b, .28f) : def.color;
            glyph.text = sprite ? "" : def.glyph;
        }

        public static string Money(int coins) => $"<color=#FFBC4A>{coins:N0}</color>";
    }
}

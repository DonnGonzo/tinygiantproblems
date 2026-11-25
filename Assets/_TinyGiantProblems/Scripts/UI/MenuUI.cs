using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TinyGiantProblems
{
    // Main menu (shown at launch), Settings window (audio, gameplay, graphics,
    // control rebinding) and the FPS counter. The pause menu and briefing open
    // Settings through MenuUI.Instance.
    public sealed class MenuUI : MonoBehaviour, GameUI.IWindow
    {
        public static MenuUI Instance { get; private set; }
        public static bool MainMenuOpen => Instance && Instance.mainMenu && Instance.mainMenu.activeSelf;
        public static bool Rebinding { get; private set; }
        public static int RebindEndedFrame { get; private set; } = -10;
        static bool shownThisSession;

        RectTransform root;
        GameObject mainMenu, settings;
        readonly List<GameObject> tabs = new List<GameObject>();
        readonly List<Image> tabButtons = new List<Image>();
        TextMeshProUGUI fps;
        float fpsTime; int fpsFrames;

        public bool IsOpen => settings && settings.activeSelf;
        public void Close() { if (Rebinding) return; if (settings) settings.SetActive(false); GameUI.MarkClosed(); GameUI.RefreshCursor(); }

        void Awake()
        {
            Instance = this;
            root = UIKit.MakeCanvas("Menus", 40, out _);
            root.SetParent(transform, false);
            BuildMainMenu();
            BuildSettings();
            fps = UIKit.Label(root, "", 15, UIKit.Muted, new Vector2(.5f, 1), new Vector2(0, -6), new Vector2(200, 22), TextAlignmentOptions.Center);
            GameUI.Register(this);
            mainMenu.SetActive(!shownThisSession);
            shownThisSession = true;
        }

        void OnDestroy() { GameUI.Unregister(this); if (Instance == this) Instance = null; }

        // ------------------------------------------------------------ main menu
        void BuildMainMenu()
        {
            var shade = UIKit.Panel(root, "Main menu", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(.02f, .06f, .07f, .45f), true);
            FullScreen(shade.rectTransform);
            mainMenu = shade.gameObject;
            var card = UIKit.Panel(mainMenu.transform, "Card", new Vector2(0, .5f), new Vector2(90, 0), new Vector2(560, 620), UIKit.Ink, true).rectTransform;
            UIAnim.Add(card.gameObject, .96f);
            UIKit.Label(card, "SMALL WORKS GUILD   /   MOSSBACK SANCTUARY", 14, UIKit.Gold, new Vector2(0, 1), new Vector2(44, -28), new Vector2(480, 24));
            UIKit.Label(card, "TINY GIANT\nPROBLEMS", 64, UIKit.Cream, new Vector2(0, 1), new Vector2(44, -66), new Vector2(480, 170));
            UIKit.Label(card, "A little care. A very big adventure.", 21, UIKit.Muted, new Vector2(0, 1), new Vector2(44, -238), new Vector2(480, 30));
            float y = -300;
            Btn(card, "PLAY", y, () => { mainMenu.SetActive(false); GameUI.RefreshCursor(); }); y -= 62;
            Btn(card, "SETTINGS", y, OpenSettings); y -= 62;
            Btn(card, "QUIT", y, Quit);
            UIKit.Label(card, "Mouse + keyboard or gamepad", 14, UIKit.Muted, new Vector2(0, 0), new Vector2(44, 24), new Vector2(480, 22));
        }

        public void ShowMainMenu()
        {
            var g = Expedition.Instance;
            FindAnyObjectByType<CrewSession>()?.Shutdown();
            if (g) { g.SetPause(false); g.Phase = ExpeditionPhase.Briefing; }
            mainMenu.SetActive(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------- settings
        void BuildSettings()
        {
            var shade = UIKit.Panel(root, "Settings", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, .45f), true);
            FullScreen(shade.rectTransform);
            settings = shade.gameObject;
            var win = UIKit.Panel(settings.transform, "Window", new Vector2(.5f, .5f), Vector2.zero, new Vector2(820, 680), UIKit.Ink, true).rectTransform;
            UIAnim.Add(win.gameObject);
            UIKit.Label(win, "SETTINGS", 34, UIKit.Cream, new Vector2(0, 1), new Vector2(36, -24), new Vector2(400, 44));
            UIKit.MakeButton(win, "CLOSE", new Vector2(1, 1), new Vector2(-30, -28), new Vector2(120, 38), Close, out _);
            string[] names = { "AUDIO", "GAMEPLAY", "GRAPHICS", "CONTROLS" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var b = UIKit.MakeButton(win, names[i], new Vector2(0, 1), new Vector2(36 + i * 188, -86), new Vector2(178, 40), () => ShowTab(index), out _);
                tabButtons.Add((Image)b.targetGraphic);
                var page = UIKit.Rect(win, names[i] + " page", new Vector2(.5f, 1), new Vector2(0, -144), new Vector2(760, 540));
                tabs.Add(page.gameObject);
            }

            // Audio
            var audio = tabs[0].transform; float y = -10;
            SliderRow(audio, "Master volume", ref y, 0, 1, GameSettings.MasterVolume, v => GameSettings.MasterVolume = v, v => Mathf.RoundToInt(v * 100) + "%");
            SliderRow(audio, "Music", ref y, 0, 1, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v, v => Mathf.RoundToInt(v * 100) + "%");
            SliderRow(audio, "Effects", ref y, 0, 1, GameSettings.EffectsVolume, v => GameSettings.EffectsVolume = v, v => Mathf.RoundToInt(v * 100) + "%");

            // Gameplay
            var play = tabs[1].transform; y = -10;
            SliderRow(play, "Look sensitivity", ref y, .2f, 3, GameSettings.LookSensitivity, v => GameSettings.LookSensitivity = v, v => v.ToString("0.00") + "x");
            ToggleRow(play, "Invert look Y", ref y, () => GameSettings.InvertY, v => GameSettings.InvertY = v);
            SliderRow(play, "Screen shake", ref y, 0, 1, GameSettings.ScreenShake, v => GameSettings.ScreenShake = v, v => Mathf.RoundToInt(v * 100) + "%");
            SliderRow(play, "Field of view", ref y, 50, 95, GameSettings.FieldOfView, v => GameSettings.FieldOfView = Mathf.Round(v), v => Mathf.RoundToInt(v) + "°");
            y -= 20;
            TextMeshProUGUI resetText = null; float armed = -10;
            UIKit.MakeButton(play, "RESET GUILD PROGRESS", new Vector2(0, 1), new Vector2(20, y), new Vector2(360, 40), () =>
            {
                if (Time.unscaledTime - armed > 3) { armed = Time.unscaledTime; resetText.text = "PRESS AGAIN TO WIPE COINS, TOOLS + MISSIONS"; return; }
                GuildEconomy.Instance?.ResetProgress(); resetText.text = "PROGRESS RESET"; armed = -10;
            }, out resetText);
            resetText.fontSize = 14;

            // Graphics
            var gfx = tabs[2].transform; y = -10;
            CycleRow(gfx, "Quality", ref y, () => QualitySettings.names[Mathf.Clamp(GameSettings.Quality, 0, QualitySettings.names.Length - 1)], () => GameSettings.Quality = (GameSettings.Quality + 1) % QualitySettings.names.Length);
            ToggleRow(gfx, "Fullscreen", ref y, () => GameSettings.Fullscreen, v => GameSettings.Fullscreen = v);
            ToggleRow(gfx, "V-Sync", ref y, () => GameSettings.VSync, v => GameSettings.VSync = v);
            ToggleRow(gfx, "Show FPS", ref y, () => GameSettings.ShowFps, v => GameSettings.ShowFps = v);

            BuildControls(tabs[3].transform);
            settings.SetActive(false);
        }

        public void OpenSettings()
        {
            settings.SetActive(true); settings.transform.SetAsLastSibling();
            ShowTab(0);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void ShowTab(int index)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].SetActive(i == index);
                tabButtons[i].color = i == index ? new Color(.55f, .42f, .16f) : UIKit.ButtonColor;
            }
            if (index == 3) RefreshBindings();
        }

        // ------------------------------------------------------------- controls
        sealed class BindRow { public InputAction action; public string part; public TextMeshProUGUI kb, pad; public Button kbButton, padButton; }
        readonly List<BindRow> bindRows = new List<BindRow>();
        TextMeshProUGUI rebindHint;

        void BuildControls(Transform page)
        {
            UIKit.Label(page, "ACTION", 14, UIKit.Gold, new Vector2(0, 1), new Vector2(20, -2), new Vector2(240, 20));
            UIKit.Label(page, "KEYBOARD / MOUSE", 14, UIKit.Gold, new Vector2(0, 1), new Vector2(270, -2), new Vector2(220, 20));
            UIKit.Label(page, "GAMEPAD", 14, UIKit.Gold, new Vector2(0, 1), new Vector2(510, -2), new Vector2(220, 20));
            var rows = new (string label, string action, string part)[]
            {
                ("Move forward", "Move", "up"), ("Move back", "Move", "down"), ("Move left", "Move", "left"), ("Move right", "Move", "right"),
                ("Jump / let go", "Jump", null), ("Sprint", "Sprint", null), ("Brace / grip", "Brace", null), ("Use tool", "Use", null),
                ("Interact", "Interact", null), ("Reel tether", "Reel", null), ("Drop item", "Drop", null), ("Pick up", "PickUp", null),
                ("Backpack", "Backpack", null), ("Safety winch", "Recover", null), ("Zoom modifier", "ZoomModifier", null),
            };
            float y = -26;
            foreach (var (label, actionName, part) in rows)
            {
                var row = new BindRow { action = GameInput.Get(actionName), part = part };
                UIKit.Label(page, label, 17, UIKit.Cream, new Vector2(0, 1), new Vector2(20, y - 5), new Vector2(240, 26));
                row.kbButton = UIKit.MakeButton(page, "", new Vector2(0, 1), new Vector2(270, y), new Vector2(220, 29), () => StartRebind(row, false), out row.kb);
                row.padButton = UIKit.MakeButton(page, "", new Vector2(0, 1), new Vector2(510, y), new Vector2(220, 29), () => StartRebind(row, true), out row.pad);
                row.kb.fontSize = row.pad.fontSize = 15;
                bindRows.Add(row);
                y -= 31.5f;
            }
            rebindHint = UIKit.Label(page, "Click a binding, then press the new key or button. Esc cancels.", 14, UIKit.Muted, new Vector2(0, 1), new Vector2(20, y - 6), new Vector2(480, 22));
            UIKit.MakeButton(page, "RESET TO DEFAULTS", new Vector2(1, 1), new Vector2(-30, y - 2), new Vector2(240, 32), () => { if (Rebinding) return; GameInput.ResetOverrides(); RefreshBindings(); }, out var t);
            t.fontSize = 14;
        }

        static int BindingIndex(InputAction action, string part, bool gamepad)
        {
            string group = gamepad ? "Gamepad" : "Keyboard&Mouse";
            var b = action.bindings;
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i].isComposite) continue;
                if (part != null ? !(b[i].isPartOfComposite && b[i].name == part) : b[i].isPartOfComposite) continue;
                if (b[i].groups != null && b[i].groups.Contains(group)) return i;
            }
            return -1;
        }

        void RefreshBindings()
        {
            foreach (var r in bindRows)
            {
                int k = BindingIndex(r.action, r.part, false), p = BindingIndex(r.action, r.part, true);
                r.kb.text = k >= 0 ? r.action.GetBindingDisplayString(k) : "-";
                r.pad.text = p >= 0 ? r.action.GetBindingDisplayString(p) : r.part != null ? "Left stick" : "-";
                r.padButton.interactable = p >= 0;
                r.kbButton.interactable = k >= 0;
            }
        }

        InputActionRebindingExtensions.RebindingOperation op;
        void StartRebind(BindRow row, bool gamepad)
        {
            if (Rebinding) return;
            int index = BindingIndex(row.action, row.part, gamepad);
            if (index < 0) return;
            Rebinding = true;
            (gamepad ? row.pad : row.kb).text = "press...";
            rebindHint.text = gamepad ? "Press a gamepad button (Esc cancels)" : "Press a key or mouse button (Esc cancels)";
            row.action.Disable();
            op = row.action.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta").WithControlsExcluding("<Pointer>/position")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(.08f);
            op = gamepad ? op.WithControlsHavingToMatchPath("<Gamepad>") : op.WithControlsExcluding("<Gamepad>");
            op.OnComplete(o => FinishRebind(row, true)).OnCancel(o => FinishRebind(row, false)).Start();
        }

        void FinishRebind(BindRow row, bool saved)
        {
            op?.Dispose(); op = null;
            row.action.Enable();
            if (saved) { GameInput.SaveOverrides(); ProceduralSfx.Play(ProceduralSfx.Equip, .5f); }
            Rebinding = false; RebindEndedFrame = Time.frameCount;
            rebindHint.text = "Click a binding, then press the new key or button. Esc cancels.";
            RefreshBindings();
        }

        // --------------------------------------------------------- row widgets
        void SliderRow(Transform page, string label, ref float y, float min, float max, float value, System.Action<float> set, System.Func<float, string> show)
        {
            UIKit.Label(page, label, 19, UIKit.Cream, new Vector2(0, 1), new Vector2(20, y - 8), new Vector2(260, 30));
            var valueText = UIKit.Label(page, show(value), 18, UIKit.Gold, new Vector2(0, 1), new Vector2(640, y - 8), new Vector2(100, 30), TextAlignmentOptions.Right);
            var slider = MakeSlider(page, new Vector2(290, y - 12), min, max, value);
            slider.onValueChanged.AddListener(v => { set(v); valueText.text = show(v); });
            y -= 58;
        }

        void ToggleRow(Transform page, string label, ref float y, System.Func<bool> get, System.Action<bool> set)
        {
            UIKit.Label(page, label, 19, UIKit.Cream, new Vector2(0, 1), new Vector2(20, y - 8), new Vector2(260, 30));
            TextMeshProUGUI text = null;
            UIKit.MakeButton(page, get() ? "ON" : "OFF", new Vector2(0, 1), new Vector2(290, y - 4), new Vector2(160, 36), () => { set(!get()); text.text = get() ? "ON" : "OFF"; }, out text);
            y -= 58;
        }

        void CycleRow(Transform page, string label, ref float y, System.Func<string> show, System.Action next)
        {
            UIKit.Label(page, label, 19, UIKit.Cream, new Vector2(0, 1), new Vector2(20, y - 8), new Vector2(260, 30));
            TextMeshProUGUI text = null;
            UIKit.MakeButton(page, show(), new Vector2(0, 1), new Vector2(290, y - 4), new Vector2(240, 36), () => { next(); text.text = show(); }, out text);
            y -= 58;
        }

        static Slider MakeSlider(Transform parent, Vector2 position, float min, float max, float value)
        {
            var rt = UIKit.Rect(parent, "Slider", new Vector2(0, 1), position, new Vector2(330, 22));
            var bg = UIKit.Panel(rt, "Track", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 8), new Color(.12f, .2f, .22f), true);
            var fillArea = UIKit.Rect(rt, "Fill area", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 8));
            var fill = UIKit.Panel(fillArea, "Fill", new Vector2(0, .5f), Vector2.zero, new Vector2(0, 8), UIKit.Gold);
            Stretch(fill.rectTransform);
            var handleArea = UIKit.Rect(rt, "Handle area", new Vector2(.5f, .5f), Vector2.zero, new Vector2(310, 22));
            var handle = UIKit.Panel(handleArea, "Handle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(20, 22), UIKit.Cream, true);
            handle.rectTransform.anchorMin = new Vector2(0, 0); handle.rectTransform.anchorMax = new Vector2(0, 1); handle.rectTransform.sizeDelta = new Vector2(20, 0);
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight; s.minValue = min; s.maxValue = max; s.SetValueWithoutNotify(value);
            return s;
        }

        static void FullScreen(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f); r.offsetMin = r.offsetMax = Vector2.zero; }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, .5f); r.offsetMin = r.offsetMax = Vector2.zero; }

        static void Btn(RectTransform parent, string label, float y, UnityEngine.Events.UnityAction action)
        {
            var b = UIKit.MakeButton(parent, label, new Vector2(.5f, 1), new Vector2(0, y), new Vector2(472, 50), action, out var t);
            t.fontSize = 21;
        }

        void Update()
        {
            // A crew host can start the run for everyone: never leave the menu over gameplay.
            var g = Expedition.Instance;
            if (g && g.IsPlaying && mainMenu.activeSelf) mainMenu.SetActive(false);
            if (!GameSettings.ShowFps) { if (fps.text.Length > 0) fps.text = ""; return; }
            fpsFrames++; fpsTime += Time.unscaledDeltaTime;
            if (fpsTime >= .5f) { fps.text = Mathf.RoundToInt(fpsFrames / fpsTime) + " FPS"; fpsFrames = 0; fpsTime = 0; }
        }
    }
}

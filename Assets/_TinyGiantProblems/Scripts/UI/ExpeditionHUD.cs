using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TinyGiantProblems
{
    public sealed class ExpeditionHUD : MonoBehaviour
    {
        public TMP_FontAsset font;
        Expedition game;
        RectTransform root;
        TextMeshProUGUI status, jobs, caption, prompt, tools, warning, debrief;
        GameObject briefingPanel, pausePanel, debriefPanel;
        GameObject[] gameplayPanels;
        readonly Color ink = new Color(.045f, .105f, .12f, .94f);
        readonly Color cream = new Color(.96f, .94f, .82f);
        readonly Color gold = new Color(1, .73f, .29f);
        public System.Action HostRequested, JoinRequested;
        public TMP_InputField address;
        public string NetworkStatus = "Solo expedition";
        void Start()
        {
            game = Expedition.Instance;
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>(); root = (RectTransform)transform;
            if (!FindAnyObjectByType<EventSystem>()) { var e = new GameObject("EventSystem"); e.AddComponent<EventSystem>(); e.AddComponent<InputSystemUIInputModule>(); }
            var left = Panel(root, "Contract", new Vector2(0, 1), new Vector2(28, -26), new Vector2(340, 245));
            Label(left, "SMALL WORKS GUILD  /  CONTRACT 001", 15, gold, new Vector2(20, -18), new Vector2(300, 25));
            Label(left, "A very bad itch", 30, cream, new Vector2(20, -50), new Vector2(300, 45));
            jobs = Label(left, "", 18, cream, new Vector2(20, -108), new Vector2(305, 124));
            var right = Panel(root, "Vitals", new Vector2(1, 1), new Vector2(-28, -26), new Vector2(290, 150));
            status = Label(right, "", 18, cream, new Vector2(18, -16), new Vector2(255, 125));
            var bottom = Panel(root, "Tools", new Vector2(.5f, 0), new Vector2(0, 22), new Vector2(830, 92));
            tools = Label(bottom, "", 20, cream, new Vector2(20, -15), new Vector2(790, 35));
            Label(bottom, "WASD move  ·  SPACE jump  ·  F grip / brace  ·  E extract  ·  R reel  ·  ESC menu", 15, new Color(.7f,.81f,.77f), new Vector2(20,-55), new Vector2(790,25));
            caption = Label(root, "", 19, cream, new Vector2(0, 135), new Vector2(1080, 52), new Vector2(.5f, 0)); caption.alignment = TextAlignmentOptions.Center;
            prompt = Label(root, "", 21, gold, new Vector2(0, 195), new Vector2(850, 55), new Vector2(.5f, 0)); prompt.alignment = TextAlignmentOptions.Center;
            warning = Label(root, "", 33, gold, new Vector2(0, -35), new Vector2(720, 85), new Vector2(.5f, 1)); warning.alignment = TextAlignmentOptions.Center;
            gameplayPanels=new[] {left.gameObject,right.gameObject,bottom.gameObject,caption.gameObject,prompt.gameObject,warning.gameObject};
            briefingPanel = Modal("Briefing", "TINY GIANT\nPROBLEMS", "A little care. A very big adventure.\n\nMossback has three troublesome itches. Board his back,\nchoose the matching tool, and hold USE at each care site.\nWhen he rumbles, hold BRACE. Then return to the harness.", out var briefing);
            Place(briefing,new Vector2(0,.5f),new Vector2(32,0),new Vector2(640,650));
            Button(briefing, "BOARD MOSSBACK  /  SOLO", -370, () => { FindAnyObjectByType<CrewSession>()?.Shutdown(); NetworkStatus="Solo expedition"; game.Begin(); });
            Button(briefing, "HOST CREW (LAN)", -430, () => { if (HostRequested != null) HostRequested(); else NetworkStatus = "Network is preparing..."; });
            Button(briefing, "JOIN CREW", -490, () => { if (JoinRequested != null) JoinRequested(); });
            var field = new GameObject("Host address", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(TMP_InputField));
            field.transform.SetParent(briefing, false); var fr = (RectTransform)field.transform;
            Place(fr, new Vector2(.5f,1), new Vector2(0,-555), new Vector2(420,38));
            field.GetComponent<UnityEngine.UI.Image>().color = new Color(.12f,.2f,.22f);
            address = field.GetComponent<TMP_InputField>();
            var addressText = Label(fr, "127.0.0.1", 18, cream, new Vector2(12,-4), new Vector2(390,30));
            address.textViewport = fr; address.textComponent = addressText; address.text = "127.0.0.1";
            pausePanel = Modal("Pause", "TAKE A BREATHER", "Mossback appreciates a careful technician.\n\nMouse to orbit · scroll to zoom · gamepad supported\nHold BACKSPACE to recover to the harness.", out var pause);
            Button(pause, "RESUME", -300, () => game.SetPause(false));
            Button(pause, "RECOVER TO HARNESS", -360, () => { game.player.Recover(); game.SetPause(false); });
            Button(pause, "MUSIC ON / OFF", -420, () => game.music.mute = !game.music.mute);
            Button(pause, "FIELD OF VIEW 60 / 80", -480, () => game.player.orbitCamera.fieldOfView = game.player.orbitCamera.fieldOfView < 70 ? 80 : 60);
            Button(pause, "RETURN TO BRIEFING", -540, ReturnToGuild);
            debriefPanel = Modal("Debrief", "SMALL WORK.\nGIANT DIFFERENCE.", "", out var result);
            debrief = Label(result, "", 22, cream, new Vector2(45,-225), new Vector2(550,150)); debrief.alignment = TextAlignmentOptions.Center;
            Button(result, "ANOTHER EXPEDITION", -430, () => game.Begin());
            Button(result, "BACK TO GUILD", -495, ReturnToGuild);
        }
        void ReturnToGuild() { FindAnyObjectByType<CrewSession>()?.Shutdown(); game.SetPause(false); game.Phase=ExpeditionPhase.Briefing; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; NetworkStatus="Solo expedition"; }
        void Update()
        {
            if (!game) return;
            briefingPanel.SetActive(game.Phase == ExpeditionPhase.Briefing);
            pausePanel.SetActive(game.Paused);
            debriefPanel.SetActive(game.Phase == ExpeditionPhase.Debrief);
            foreach(var panel in gameplayPanels) panel.SetActive(game.IsPlaying && !game.Paused);
            status.text = $"MOSSBACK  /  {game.player.ZoneName}\nComfort   {game.Comfort:0}%\nGrip          {game.player.grip:0}%\n{NetworkStatus}";
            jobs.text = "";
            for (int i = 0; i < game.jobs.Length; i++)
            { var j = game.jobs[i]; jobs.text += $"{(j.state.Complete ? "<color=#86D8A0>DONE" : "0" + (i + 1))}  {j.title}{(j.state.Complete ? "</color>" : "")}\n"; }
            tools.text = "";
            for (int i = 0; i < 4; i++) tools.text += (i == (int)game.player.selected ? "<color=#FFBC4A>" : "<color=#B7C9BF>") + $"[{i+1}] {(CareTool)i}   </color>";
            caption.text = game.Caption;
            prompt.text = game.Nearest ? $"{game.Nearest.title}  ·  {game.Nearest.tool}  ·  HOLD USE  {game.Nearest.state.Progress:P0}" : game.Phase == ExpeditionPhase.Extraction ? "All better! Return to the harness and press E / X to extract." : game.player.tethered ? "Tether secured  ·  hold R / LT to reel" : "";
            warning.text = game.WarningRemaining > 0 ? $"{game.Reaction} IN {Mathf.CeilToInt(game.WarningRemaining)}\n<size=21>HOLD F / B TO BRACE</size>" : "";
            debrief.text = $"Mossback comfort  {game.Comfort:0}%\nCare jobs  {game.Completed}/3   ·   Safety rescues  {game.player.recoveries}\nGuild pay  {game.Reward}   ·   Best  {PlayerPrefs.GetInt("GuildBest")}\nExpedition time  {game.Elapsed / 60:0}:{game.Elapsed % 60:00}";
        }
        GameObject Modal(string name, string title, string body, out RectTransform panel)
        {
            panel = Panel(root, name, new Vector2(.5f,.5f), Vector2.zero, new Vector2(640,650));
            Label(panel, "SMALL WORKS GUILD   /   MOSSBACK SANCTUARY", 14, gold, new Vector2(45,-25), new Vector2(550,30));
            Label(panel, title, 47, cream, new Vector2(45,-80), new Vector2(550,120));
            Label(panel, body, 20, cream, new Vector2(45,-220), new Vector2(550,145));
            return panel.gameObject;
        }
        RectTransform Panel(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent,false);
            var rt = (RectTransform)go.transform; Place(rt, anchor, position, size); go.GetComponent<UnityEngine.UI.Image>().color = ink; return rt;
        }
        TextMeshProUGUI Label(RectTransform parent, string value, float size, Color color, Vector2 position, Vector2 dimensions, Vector2? anchor = null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            Place((RectTransform)go.transform, anchor ?? new Vector2(0,1), position, dimensions);
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = size; text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        void Button(RectTransform panel, string label, float y, UnityEngine.Events.UnityAction action)
        {
            var p = Panel(panel, label, new Vector2(.5f,1), new Vector2(0,y), new Vector2(490,46));
            p.GetComponent<UnityEngine.UI.Image>().color = new Color(.22f,.39f,.35f);
            var button = p.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = p.GetComponent<UnityEngine.UI.Image>(); button.onClick.AddListener(action);
            var t = Label(p,label,18,cream,new Vector2(0,-5),new Vector2(490,35)); t.alignment = TextAlignmentOptions.Center;
        }
        static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size) { rt.anchorMin = rt.anchorMax = rt.pivot = anchor; rt.anchoredPosition = position; rt.sizeDelta = size; }
    }
}

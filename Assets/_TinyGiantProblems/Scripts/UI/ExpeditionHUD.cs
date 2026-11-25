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
        TextMeshProUGUI status, jobs, caption, prompt, warning, debrief, pauseControls;
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
            missionTag = Label(left, "SMALL WORKS GUILD  /  MISSION", 15, gold, new Vector2(20, -18), new Vector2(300, 25));
            missionTitle = Label(left, "", 28, cream, new Vector2(20, -48), new Vector2(305, 45));
            jobs = Label(left, "", 18, cream, new Vector2(20, -108), new Vector2(305, 124));
            var right = Panel(root, "Vitals", new Vector2(1, 1), new Vector2(-28, -26), new Vector2(290, 150));
            status = Label(right, "", 18, cream, new Vector2(18, -16), new Vector2(255, 125));
            caption = Label(root, "", 19, cream, new Vector2(0, 165), new Vector2(1080, 52), new Vector2(.5f, 0)); caption.alignment = TextAlignmentOptions.Center;
            prompt = Label(root, "", 21, gold, new Vector2(0, 212), new Vector2(850, 40), new Vector2(.5f, 0)); prompt.alignment = TextAlignmentOptions.Center;
            warning = Label(root, "", 33, gold, new Vector2(0, -35), new Vector2(720, 85), new Vector2(.5f, 1)); warning.alignment = TextAlignmentOptions.Center;
            gameplayPanels=new[] {left.gameObject,right.gameObject,caption.gameObject,prompt.gameObject,warning.gameObject};
            briefingPanel = Modal("Briefing", "TINY GIANT\nPROBLEMS", "A little care. A very big adventure.\n\nPick a mission on the board, then board Mossback.\nSell salvage in the Guild House, buy better tools.\nWhen he rumbles, hold BRACE or go flying!", out var briefing);
            Place(briefing,new Vector2(0,.5f),new Vector2(32,0),new Vector2(640,712));
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
            Button(briefing, "GUILD TOOL LOCKER", -608, () => GuildShopUI.Instance?.Open());
            Button(briefing, "GUILD MEMBER  (CLOTHES & COLOURS)", -666, () => GuildMemberEditor.Instance?.Open());
            BuildMissionBoard();
            UIKit.MakeButton(briefing, "SETTINGS", new Vector2(1, 1), new Vector2(-28, -96), new Vector2(140, 34), () => MenuUI.Instance?.OpenSettings(), out var st); st.fontSize = 14;
            UIKit.MakeButton(briefing, "MAIN MENU", new Vector2(1, 1), new Vector2(-28, -138), new Vector2(140, 34), () => MenuUI.Instance?.ShowMainMenu(), out var mt); mt.fontSize = 14;
            UIAnim.Add(briefingPanel); if (boardPanel) UIAnim.Add(boardPanel.gameObject);
            pausePanel = Modal("Pause", "TAKE A BREATHER", "", out var pause);
            Place(pause, new Vector2(.5f, .5f), Vector2.zero, new Vector2(640, 720));
            pauseControls = Label(pause, "", 16, new Color(.78f, .86f, .82f), new Vector2(45, -205), new Vector2(550, 150));
            Button(pause, "RESUME", -380, () => game.SetPause(false));
            Button(pause, "SETTINGS", -436, () => MenuUI.Instance?.OpenSettings());
            Button(pause, "SAFETY WINCH (BACK TO START)", -492, () => { game.player.Recover(); game.SetPause(false); });
            Button(pause, "RETURN TO MISSION BOARD", -548, ReturnToGuild);
            Button(pause, "MAIN MENU", -604, () => MenuUI.Instance?.ShowMainMenu());
            UIAnim.Add(pausePanel);
            debriefPanel = Modal("Debrief", "SMALL WORK.\nGIANT DIFFERENCE.", "", out var result);
            debrief = Label(result, "", 22, cream, new Vector2(45,-225), new Vector2(550,150)); debrief.alignment = TextAlignmentOptions.Center;
            Place(result, new Vector2(.5f, .5f), Vector2.zero, new Vector2(640, 690));
            debrief.rectTransform.sizeDelta = new Vector2(550, 300); debrief.fontSize = 20;
            Button(result, "NEXT MISSION", -545, () => game.Begin());
            Button(result, "BACK TO GUILD", -605, ReturnToGuild);
            UIAnim.Add(debriefPanel, .9f);
        }
        // Where the technician is: in the Guild House, out in the meadow, or on
        // Mossback (his back, a flank while climbing, or a placed platform).
        string ZoneLabel()
        {
            var p = game.player;
            if (GuildHall.PlayerInside) return "Guild House";
            var frame = game.creature ? game.creature.transform : null;
            if (frame && Physics.Raycast(p.transform.position + Vector3.up * .5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<PlacedPlatform>()) return "Floating Platform";
                if (hit.collider.transform.IsChildOf(frame)) return p.ZoneName == "Guild harness" ? "Back" : p.ZoneName;
            }
            if (p.climbing) return "Flank (climbing)";
            return p.grounded ? "Meadow" : "In the air";
        }

        // Text below is rebuilt only when the values it shows change (no per-frame garbage).
        string lastZone, lastNet; int lastComfort = -1, lastGrip = -1;
        void DrawStatus()
        {
            if (Time.frameCount % 6 != 0 && lastComfort >= 0) return;   // the zone raycast need not run every frame
            string zone = ZoneLabel(); int comfort = Mathf.RoundToInt(game.Comfort), grip = Mathf.RoundToInt(game.player.grip);
            if (zone == lastZone && comfort == lastComfort && grip == lastGrip && (object)NetworkStatus == lastNet) return;
            lastZone = zone; lastComfort = comfort; lastGrip = grip; lastNet = NetworkStatus;
            status.text = $"MOSSBACK  /  {zone}\nComfort   {comfort}%\nGrip          {grip}%\n{NetworkStatus}";
        }

        CareJob lastJob; int lastJobPct = -1, lastWarnSecs = -1; ExpeditionPhase lastPhase; bool lastTether; string lastReaction;
        void DrawPromptAndWarning()
        {
            int pct = game.Nearest ? Mathf.FloorToInt(game.Nearest.state.Progress * 100) : -1;
            if (game.Nearest != lastJob || pct != lastJobPct || game.Phase != lastPhase || game.player.tethered != lastTether)
            {
                lastJob = game.Nearest; lastJobPct = pct; lastPhase = game.Phase; lastTether = game.player.tethered;
                prompt.text = game.Nearest ? $"{game.Nearest.title}  ·  {game.Nearest.tool}  ·  HOLD {GameInput.Display(GameInput.Use).ToUpper()}  {pct}%"
                    : game.Phase == ExpeditionPhase.Extraction ? "All done! Go to the extraction beacon and press " + GameInput.Display(GameInput.Interact) + " to finish."
                    : game.player.tethered ? "Tether secured  ·  hold " + GameInput.Display(GameInput.Reel) + " to reel" : "";
            }
            int secs = game.WarningRemaining > 0 ? Mathf.CeilToInt(game.WarningRemaining) : 0;
            if (secs != lastWarnSecs || (object)game.Reaction != lastReaction)
            {
                lastWarnSecs = secs; lastReaction = game.Reaction;
                warning.text = secs > 0 ? $"{game.Reaction} IN {secs}\n<size=21>HOLD {GameInput.Display(GameInput.Brace).ToUpper()} TO BRACE</size>" : "";
                if (secs > 0) UIAnim.Punch(warning.rectTransform, .12f);
            }
        }

        // Main objectives of the active mission (side tasks and challenges are
        // in the hotbar's panel). Rebuilt only when something changed.
        TextMeshProUGUI missionTag, missionTitle;
        string lastObjectives; int objectiveStamp;
        readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();
        void DrawObjectives()
        {
            var m = MissionManager.Instance;
            if (!m || !m.Active) return;
            // Cheap fingerprint of the objective state: rebuild the text only when it moves.
            int stamp = m.MainComplete ? 1 : 0;
            for (int i = 0; i < m.progress.Length; i++) stamp = stamp * 31 + m.progress[i] * 2 + (m.done[i] ? 1 : 0);
            stamp = stamp * 31 + m.Active.GetInstanceID();
            if (stamp == objectiveStamp && lastObjectives != null) return;
            objectiveStamp = stamp;
            sb.Clear();
            int n = 1;
            for (int i = 0; i < m.Active.objectives.Length; i++)
            {
                var o = m.Active.objectives[i]; if (o.optional) continue;
                bool done = m.done[i];
                string count = o.target > 1 && !o.UsesBest ? $" {m.progress[i]}/{o.target}" : o.UsesBest && !done ? $" {m.progress[i]}/{o.target}" : "";
                if (done) sb.Append("<color=#86D8A0>DONE  ").Append(o.title).Append("</color>\n");
                else sb.Append('0').Append(n).Append("  ").Append(o.title).Append("<color=#B7C9BF>").Append(count).Append("</color>\n");
                n++;
            }
            if (m.MainComplete) sb.Append("<color=#FFBC4A>> Extract at the beacon</color>");
            var text = sb.ToString();
            if (text != lastObjectives) { if (lastObjectives != null) UIAnim.Punch(jobs.rectTransform, .06f); jobs.text = lastObjectives = text; missionTitle.text = m.Active.title; missionTag.text = $"SMALL WORKS GUILD  /  TIER {m.Active.tier}"; }
        }

        static string ControlsSummary()
        {
            string D(UnityEngine.InputSystem.InputAction a) => "<color=#FFBC4A>" + GameInput.Display(a) + "</color>";
            return $"{D(GameInput.Jump)} jump   {D(GameInput.Sprint)} sprint   {D(GameInput.Brace)} brace/grip   {D(GameInput.Use)} use tool\n" +
                   $"{D(GameInput.Interact)} interact   {D(GameInput.Backpack)} bag   {D(GameInput.Drop)} drop   {D(GameInput.Reel)} reel\n" +
                   $"Scroll or 1-9 pick a tool   {D(GameInput.ZoomModifier)} + scroll zoom\nHold {D(GameInput.Recover)} for the safety winch.   Rebind keys in Settings.";
        }

        void ReturnToGuild() { FindAnyObjectByType<CrewSession>()?.Shutdown(); game.SetPause(false); game.Phase=ExpeditionPhase.Briefing; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; NetworkStatus="Solo expedition"; }
        void Update()
        {
            if (!game) return;
            bool menu = MenuUI.MainMenuOpen;
            briefingPanel.SetActive(game.Phase == ExpeditionPhase.Briefing && !menu);
            if (game.Paused && !pausePanel.activeSelf) pauseControls.text = ControlsSummary();
            pausePanel.SetActive(game.Paused);
            debriefPanel.SetActive(game.Phase == ExpeditionPhase.Debrief);
            foreach(var panel in gameplayPanels) panel.SetActive(game.IsPlaying && !game.Paused);
            DrawStatus();
            DrawObjectives();
            caption.text = game.Caption;
            DrawPromptAndWarning();
            if (game.Phase == ExpeditionPhase.Debrief) DrawDebrief(); else lastDebrief = null;
            if (boardPanel) boardPanel.gameObject.SetActive(game.Phase == ExpeditionPhase.Briefing && !menu);
            if (game.Phase == ExpeditionPhase.Briefing) DrawMissionBoard();
        }

        string FallbackDebrief()
        {
            var econ = GuildEconomy.Instance;
            var mess = MessField.Instance;
            string coins = econ ? $"\nCoins earned this run  {econ.RunEarnings}   ·   Wallet  {econ.Coins}" : "";
            string clean = mess ? $"   ·   Coat cleaned  {mess.Cleanliness:P0}" : "";
            return $"Mossback comfort  {game.Comfort:0}%{clean}\nCare jobs  {game.Completed}/3   ·   Safety rescues  {game.player.recoveries}\nGuild pay  {game.Reward}{coins}\nExpedition time  {Mathf.FloorToInt(game.Elapsed / 60)}:{game.Elapsed % 60:00}";
        }

        // ---- Mission board (briefing screen) ----
        RectTransform boardPanel;
        readonly System.Collections.Generic.List<(MissionDef mission, UnityEngine.UI.Image bg, TextMeshProUGUI label)> rows = new System.Collections.Generic.List<(MissionDef, UnityEngine.UI.Image, TextMeshProUGUI)>();
        TextMeshProUGUI boardDetail, boardHeader;
        MissionDef shownMission; int shownDone = -1;
        static readonly Color RowIdle = new Color(.16f, .27f, .27f), RowSelected = new Color(.55f, .42f, .16f), RowLocked = new Color(.1f, .14f, .15f);

        void BuildMissionBoard()
        {
            var catalog = MissionCatalog.Instance; if (!catalog) return;
            boardPanel = Panel(root, "Mission board", new Vector2(1, .5f), new Vector2(-32, 0), new Vector2(600, 700));
            boardHeader = Label(boardPanel, "", 15, gold, new Vector2(28, -22), new Vector2(545, 26));
            Label(boardPanel, "MISSION BOARD", 34, cream, new Vector2(28, -48), new Vector2(545, 46));
            float y = -104;
            foreach (var m in catalog.missions)
            {
                if (!m) continue;
                var row = Panel(boardPanel, m.id, new Vector2(.5f, 1), new Vector2(0, y), new Vector2(545, 36));
                var bg = row.GetComponent<UnityEngine.UI.Image>();
                var button = row.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = bg;
                var mission = m; button.onClick.AddListener(() => { MissionManager.Instance?.Select(mission); ProceduralSfx.Play(ProceduralSfx.Pop, .5f); });
                var label = Label(row, "", 17, cream, new Vector2(14, -6), new Vector2(520, 28));
                rows.Add((m, bg, label));
                y -= 41;
            }
            boardDetail = Label(boardPanel, "", 17, cream, new Vector2(28, y - 8), new Vector2(545, 685 + y));
        }

        void DrawMissionBoard()
        {
            var mm = MissionManager.Instance; if (!mm || boardDetail == null) return;
            if (mm.Selected == shownMission && mm.CompletedCount == shownDone) return;   // only redraw on change
            shownMission = mm.Selected; shownDone = mm.CompletedCount;
            boardHeader.text = $"SMALL WORKS GUILD   /   {shownDone} OF {rows.Count} MISSIONS DONE";
            foreach (var (m, bg, label) in rows)
            {
                bool unlocked = mm.IsUnlocked(m), finished = mm.IsDone(m);
                var r = mm.RecordFor(m);
                string stars = new string('*', m.tier);
                string medals = finished ? $"  <color=#9FD3FF>{r.challenges.Length}/{m.challenges.Length} challenges</color>" : "";
                label.text = !unlocked ? $"<color=#6E8580>LOCKED   {m.title}</color>"
                    : $"{(finished ? "<color=#86D8A0>DONE</color>" : "<color=#FFBC4A>NEW </color>")}   {m.title}  <color=#FFBC4A>{stars}</color>{medals}";
                bg.color = !unlocked ? RowLocked : m == mm.Selected ? RowSelected : RowIdle;
                bg.GetComponent<UnityEngine.UI.Button>().interactable = unlocked;
            }
            var sel = mm.Selected; if (!sel) { boardDetail.text = ""; return; }
            var rec = mm.RecordFor(sel);
            sb.Clear();
            sb.Append("<color=#FFBC4A>").Append(sel.title.ToUpper()).Append("</color>   <size=14>TIER ").Append(sel.tier).Append("</size>\n");
            sb.Append("<size=16>").Append(sel.brief).Append("</size>\n");
            foreach (var o in sel.objectives) if (!o.optional) sb.Append("  - ").Append(o.title).Append(o.target > 1 && !o.UsesBest ? " (" + o.target + ")" : "").Append('\n');
            foreach (var o in sel.objectives) if (o.optional) sb.Append("<color=#B7C9BF>  opt  ").Append(o.title).Append("</color>  <color=#FFBC4A>+").Append(o.reward).Append("</color>\n");
            foreach (var c in sel.challenges)
            {
                bool got = System.Array.IndexOf(rec.challenges, c.title) >= 0;
                sb.Append(got ? "<color=#86D8A0>  DONE " : "<color=#9FD3FF>  *  ").Append(c.title).Append("</color>  <color=#FFBC4A>+").Append(c.reward).Append("</color>\n");
            }
            sb.Append("<color=#FFBC4A>Reward ").Append(sel.rewardCoins).Append(" coins</color>");
            if (!rec.done) { sb.Append("  <size=15>(+50% first clear"); if (sel.rewardItem != ItemId.None) sb.Append(" + ").Append(ItemCatalog.Get(sel.rewardItem).name); sb.Append(")</size>"); }
            else if (rec.bestTime > 0) sb.Append("   <size=15>best ").Append((int)rec.bestTime / 60).Append(':').Append(((int)rec.bestTime % 60).ToString("00")).Append("</size>");
            boardDetail.text = sb.ToString();
        }

        string lastDebrief;
        void DrawDebrief()
        {
            if (lastDebrief != null) return;
            var mm = MissionManager.Instance; var r = mm ? mm.LastResult : null;
            if (r == null || !r.mission) { debrief.text = lastDebrief = FallbackDebrief(); return; }
            sb.Clear();
            sb.Append("<color=#FFBC4A>MISSION COMPLETE  /  ").Append(r.mission.title.ToUpper()).Append("</color>\n");
            sb.Append("Time ").Append((int)r.time / 60).Append(':').Append(((int)r.time % 60).ToString("00"));
            sb.Append("   ·   Comfort ").Append(game.Comfort.ToString("0")).Append("%\n");
            sb.Append("<color=#FFBC4A>+").Append(r.coins).Append(" coins</color>");
            if (r.firstClear) { sb.Append("  <size=16>(first clear bonus"); if (r.mission.rewardItem != ItemId.None) sb.Append(", ").Append(ItemCatalog.Get(r.mission.rewardItem).name).Append(" added to your bag"); sb.Append(")</size>"); }
            sb.Append('\n');
            if (GuildEconomy.Instance) sb.Append("Earned this run ").Append(GuildEconomy.Instance.RunEarnings).Append("   ·   Wallet ").Append(GuildEconomy.Instance.Coins).Append('\n');
            foreach (var c in r.challengesEarned) sb.Append("<color=#86D8A0>DONE  ").Append(c).Append("</color>\n");
            foreach (var c in r.challengesMissed) sb.Append("<color=#FF9A8A>MISSED  ").Append(c).Append("</color>\n");
            if (mm.Selected && mm.Selected != r.mission) sb.Append("<color=#9FD3FF>Next up: ").Append(mm.Selected.title).Append("</color>");
            else if (mm.CompletedCount >= (MissionCatalog.Instance ? MissionCatalog.Instance.missions.Length : 0)) sb.Append("<color=#9FD3FF>Every mission done! Replay for challenges and best times.</color>");
            debrief.text = lastDebrief = sb.ToString();
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
            var button = p.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = p.GetComponent<UnityEngine.UI.Image>(); button.onClick.AddListener(() => ProceduralSfx.Play(ProceduralSfx.Click, .5f)); button.onClick.AddListener(action);
            var t = Label(p,label,18,cream,new Vector2(0,-5),new Vector2(490,35)); t.alignment = TextAlignmentOptions.Center;
        }
        static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size) { rt.anchorMin = rt.anchorMax = rt.pivot = anchor; rt.anchoredPosition = position; rt.sizeDelta = size; }
    }
}

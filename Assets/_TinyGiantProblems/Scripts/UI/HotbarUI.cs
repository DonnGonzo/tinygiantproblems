using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TinyGiantProblems
{
    // Minecraft-style hotbar (scroll / 1-9 / shoulders) plus a TAB backpack where
    // stacks can be dragged, swapped, merged, shift-clicked or dropped.
    // Also draws coins, work orders, toasts and station prompts.
    public sealed class HotbarUI : MonoBehaviour, GameUI.IWindow
    {
        public static HotbarUI Instance { get; private set; }
        const float Slot = 62, Gap = 6;
        Expedition game;
        GuildEconomy economy;
        RectTransform root, hotbarBar, pack, ghost, tooltip, ordersPanel;
        readonly List<SlotView> views = new List<SlotView>();
        TextMeshProUGUI selectedName, coins, coinsDelta, orders, prompt, tooltipText, ghostCount, packHint;
        Image ghostSwatch, selectionFrame;
        TextMeshProUGUI ghostGlyph;
        CanvasGroup selectedGroup;
        GameObject[] playingOnly;
        float nameTimer, coinPulse;
        int lastSelected = -1, shownCoins;
        ItemId lastItem;
        public int DragFrom { get; private set; } = -1;
        readonly List<(RectTransform rt, TextMeshProUGUI text, float born)> toasts = new List<(RectTransform, TextMeshProUGUI, float)>();

        public bool IsOpen => pack && pack.gameObject.activeSelf;
        public void Open() { if (pack && !pack.gameObject.activeSelf) { pack.gameObject.SetActive(true); GameUI.RefreshCursor(); } }
        public void Close() { if (pack && pack.gameObject.activeSelf) { pack.gameObject.SetActive(false); EndDrag(); HideTooltip(); GameUI.MarkClosed(); GameUI.RefreshCursor(); } }

        void Awake() { Instance = this; GameUI.Register(this); }
        void OnDestroy()
        {
            GameUI.Unregister(this);
            if (economy) economy.Toast -= OnToast;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            game = Expedition.Instance;
            economy = GuildEconomy.Instance;
            root = UIKit.MakeCanvas("Hotbar UI", 10, out _);
            root.SetParent(transform, false);

            // Hotbar
            float width = Inventory.HotbarSize * Slot + (Inventory.HotbarSize - 1) * Gap + 20;
            hotbarBar = UIKit.Panel(root, "Hotbar", new Vector2(.5f, 0), new Vector2(0, 16), new Vector2(width, Slot + 20), UIKit.Ink).rectTransform;
            selectionFrame = UIKit.Panel(hotbarBar, "Selection", new Vector2(0, .5f), Vector2.zero, Vector2.one * (Slot + 10), UIKit.Gold);
            for (int i = 0; i < Inventory.HotbarSize; i++)
                views.Add(MakeSlot(hotbarBar, i, new Vector2(10 + i * (Slot + Gap), 0), true));
            selectedName = UIKit.Label(root, "", 20, UIKit.Cream, new Vector2(.5f, 0), new Vector2(0, 132), new Vector2(600, 30), TextAlignmentOptions.Center);
            selectedGroup = selectedName.gameObject.AddComponent<CanvasGroup>();

            // Backpack window
            pack = UIKit.Panel(root, "Backpack", new Vector2(.5f, 0), new Vector2(0, 104), new Vector2(width, 2 * Slot + Gap + 86), UIKit.Ink, true).rectTransform;
            UIKit.Label(pack, "BACKPACK", 16, UIKit.Gold, new Vector2(0, 1), new Vector2(14, -10), new Vector2(200, 24));
            packHint = UIKit.Label(pack, "Drag to arrange · Shift-click to move · Right-click a stack to drop one · Drag outside to drop", 13, UIKit.Muted, new Vector2(1, 1), new Vector2(-14, -13), new Vector2(width - 140, 22), TextAlignmentOptions.TopRight);
            for (int i = 0; i < Inventory.PackSize; i++)
            {
                int row = i / Inventory.HotbarSize, col = i % Inventory.HotbarSize;
                views.Add(MakeSlot(pack, Inventory.HotbarSize + i, new Vector2(10 + col * (Slot + Gap), -40 - row * (Slot + Gap)), false));
            }
            pack.gameObject.SetActive(false);

            tooltip = UIKit.Panel(root, "Tooltip", new Vector2(0, 0), Vector2.zero, new Vector2(300, 96), new Color(.02f, .05f, .06f, .97f)).rectTransform;
            tooltip.pivot = new Vector2(0, 0);
            tooltipText = UIKit.Label(tooltip, "", 15, UIKit.Cream, new Vector2(0, 1), new Vector2(12, -8), new Vector2(276, 82));
            tooltip.gameObject.SetActive(false);

            ghost = UIKit.Rect(root, "Drag ghost", new Vector2(0, 0), Vector2.zero, Vector2.one * Slot);
            var ghostGroup = ghost.gameObject.AddComponent<CanvasGroup>(); ghostGroup.blocksRaycasts = false; ghostGroup.alpha = .9f;
            UIKit.Icon(ghost, null, Slot - 12, out ghostSwatch, out ghostGlyph);
            ghostCount = UIKit.Label(ghost, "", 16, Color.white, new Vector2(1, 0), new Vector2(-4, 2), new Vector2(40, 22), TextAlignmentOptions.BottomRight);
            ghost.gameObject.SetActive(false);

            // Coins badge under the vitals panel
            var coinBadge = UIKit.Panel(root, "Coins", new Vector2(1, 1), new Vector2(-28, -186), new Vector2(290, 56), UIKit.Ink).rectTransform;
            var coin = UIKit.Panel(coinBadge, "Coin", new Vector2(0, .5f), new Vector2(16, 0), new Vector2(30, 30), UIKit.Gold);
            UIKit.Label(coin.transform, "G", 18, new Color(.35f, .2f, .05f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30), TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            coins = UIKit.Label(coinBadge, "", 26, UIKit.Gold, new Vector2(0, .5f), new Vector2(58, 0), new Vector2(150, 36), TextAlignmentOptions.Left);
            coinsDelta = UIKit.Label(coinBadge, "", 18, UIKit.Good, new Vector2(1, .5f), new Vector2(-14, 0), new Vector2(110, 30), TextAlignmentOptions.Right);

            // Work orders under the contract panel
            ordersPanel = UIKit.Panel(root, "Work orders", new Vector2(0, 1), new Vector2(28, -284), new Vector2(340, 236), UIKit.Ink).rectTransform;
            UIKit.Label(ordersPanel, "SIDE TASKS  ·  CHALLENGES", 14, UIKit.Gold, new Vector2(0, 1), new Vector2(18, -12), new Vector2(310, 22));
            orders = UIKit.Label(ordersPanel, "", 15, UIKit.Cream, new Vector2(0, 1), new Vector2(18, -38), new Vector2(310, 190));

            prompt = UIKit.Label(root, "", 21, UIKit.Cream, new Vector2(.5f, 0), new Vector2(0, 252), new Vector2(900, 34), TextAlignmentOptions.Center);
            playingOnly = new[] { hotbarBar.gameObject, selectedName.gameObject, coinBadge.gameObject, ordersPanel.gameObject, prompt.gameObject };

            if (economy) { economy.Toast += OnToast; shownCoins = economy.Coins; }
        }

        SlotView MakeSlot(RectTransform parent, int index, Vector2 pos, bool hotbar)
        {
            var bg = UIKit.Panel(parent, "Slot " + index, hotbar ? new Vector2(0, .5f) : new Vector2(0, 1), pos, Vector2.one * Slot, new Color(.13f, .24f, .25f, 1), true);
            var v = bg.gameObject.AddComponent<SlotView>();
            v.ui = this; v.index = index; v.background = bg;
            UIKit.Icon(bg.transform, null, Slot - 12, out v.swatch, out v.glyph);
            v.count = UIKit.Label(bg.transform, "", 16, Color.white, new Vector2(1, 0), new Vector2(-5, 2), new Vector2(40, 22), TextAlignmentOptions.BottomRight);
            v.count.fontStyle = FontStyles.Bold; v.count.outlineWidth = .25f; v.count.outlineColor = new Color32(0, 0, 0, 255);
            if (hotbar) v.key = UIKit.Label(bg.transform, (index + 1).ToString(), 12, UIKit.Muted, new Vector2(0, 1), new Vector2(5, -3), new Vector2(20, 16));
            return v;
        }

        void OnToast(string text, int coinsDelta)
        {
            var rt = UIKit.Panel(root, "Toast", new Vector2(1, 1), new Vector2(-28, -250), new Vector2(380, 40), UIKit.Ink).rectTransform;
            string amount = coinsDelta > 0 ? $"  <color=#86D8A0>+{coinsDelta}</color>" : coinsDelta < 0 ? $"  <color=#FF7366>{coinsDelta}</color>" : "";
            var label = UIKit.Label(rt, text + amount, 16, UIKit.Cream, new Vector2(0, .5f), new Vector2(14, 0), new Vector2(356, 36), TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
            toasts.Insert(0, (rt, label, Time.unscaledTime));
            if (toasts.Count > 5) { Destroy(toasts[5].rt.gameObject); toasts.RemoveAt(5); }
            if (coinsDelta != 0) { coinPulse = 1; this.coinsDelta.text = coinsDelta > 0 ? $"+{coinsDelta}" : coinsDelta.ToString(); this.coinsDelta.color = coinsDelta > 0 ? UIKit.Good : UIKit.Bad; }
        }

        void Update()
        {
            if (!game || !economy) return;
            var inv = economy.Inventory;
            bool playing = game.IsPlaying && !game.Paused;
            foreach (var go in playingOnly) go.SetActive(playing);
            if (!playing && IsOpen) Close();
            HandleInput(inv, playing);
            SyncSelectedTool(inv);
            DrawSlots(inv);
            DrawCoins();
            DrawOrders();
            DrawPrompt(playing);
            DrawToasts();
            if (DragFrom >= 0)
            {
                if (Mouse.current != null) ghost.position = Mouse.current.position.ReadValue();
            }
        }

        void HandleInput(Inventory inv, bool playing)
        {
            if (!playing) return;
            bool shopOpen = GuildShopUI.Instance && GuildShopUI.Instance.IsOpen;
            if (GameInput.Backpack.WasPressedThisFrame() && !shopOpen)
            {
                if (IsOpen) Close(); else Open();
            }
            if (GameUI.CapturesInput) return;
            bool zoomHeld = GameInput.ZoomModifier.IsPressed();
            if (!zoomHeld)
            {
                float y = GameInput.HotbarScroll.ReadValue<float>();
                if (y > .01f) inv.Scroll(-1); else if (y < -.01f) inv.Scroll(1);
            }
            for (int i = 0; i < 9; i++) if (GameInput.Slot(i).WasPressedThisFrame()) inv.Select(i);
            if (GameInput.HotbarNext.WasPressedThisFrame()) inv.Scroll(1);
            if (GameInput.HotbarPrev.WasPressedThisFrame()) inv.Scroll(-1);
            if (GameInput.Drop.WasPressedThisFrame()) DropFromSlot(inv.selected, zoomHeld ? 999 : 1);
            if (GameInput.Interact.WasPressedThisFrame() && !WinchPost.Hauling && !GameUI.JustClosed)
            {
                var station = Station.Nearest(game.player.transform.position);
                if (station) station.Interact();
            }
        }

        void SyncSelectedTool(Inventory inv)
        {
            var def = inv.SelectedDef;
            var tool = def != null && def.IsTool ? def.care : CareTool.Hands;
            if (tool != CareTool.Tether && game.player.selected == CareTool.Tether && game.player.tethered) game.player.tethered = false;
            game.player.selected = tool;
            var id = def != null ? def.id : ItemId.None;
            if (inv.selected != lastSelected || id != lastItem)
            {
                if (lastSelected >= 0) ProceduralSfx.Play(ProceduralSfx.Equip, .35f);
                lastSelected = inv.selected; lastItem = id;
                selectedName.text = def == null ? "Empty hands" : def.IsDebris ? $"{def.name}  <color=#B7C9BF>·  sells for {def.sellValue}</color>" : def.name;
                nameTimer = 2.2f;
            }
            nameTimer -= Time.unscaledDeltaTime;
            selectedGroup.alpha = Mathf.Clamp01(nameTimer / .5f);
            selectionFrame.rectTransform.anchoredPosition = new Vector2(10 + inv.selected * (Slot + Gap) - 5, 0);
        }

        void DrawSlots(Inventory inv)
        {
            foreach (var v in views)
            {
                var s = inv.slots[v.index];
                bool hidden = v.index == DragFrom;
                var def = s.IsEmpty || hidden ? null : ItemCatalog.Get(s.id);
                UIKit.SetIcon(v.swatch, v.glyph, def);
                v.swatch.transform.GetChild(0).gameObject.SetActive(def != null && !ItemIcons.HasPicture(def));
                v.count.text = def != null && s.count > 1 ? s.count.ToString() : "";
                bool selected = v.index == inv.selected;
                v.background.color = selected ? new Color(.2f, .36f, .34f, 1) : v.hover && IsOpen ? new Color(.18f, .31f, .31f, 1) : new Color(.13f, .24f, .25f, 1);
                v.swatch.rectTransform.localScale = Vector3.one * (selected ? 1.06f : 1f);
            }
        }

        void DrawCoins()
        {
            int target = economy.Coins;
            if (shownCoins != target) shownCoins = (int)Mathf.MoveTowards(shownCoins, target, Mathf.Max(1, Mathf.Abs(target - shownCoins) * 6 * Time.unscaledDeltaTime));
            coins.text = $"{shownCoins:N0}";
            coinPulse = Mathf.MoveTowards(coinPulse, 0, Time.unscaledDeltaTime * .5f);
            coins.transform.localScale = Vector3.one * (1 + coinPulse * coinPulse * .25f);
            var c = coinsDelta.color; c.a = Mathf.Clamp01(coinPulse * 2); coinsDelta.color = c;
        }

        readonly System.Text.StringBuilder ordersSb = new System.Text.StringBuilder();
        string lastOrders; float nextOrdersRedraw;
        void DrawOrders()
        {
            var m = MissionManager.Instance; if (!m || !m.Active) return;
            if (Time.unscaledTime < nextOrdersRedraw) return;   // a few times a second is plenty
            nextOrdersRedraw = Time.unscaledTime + .25f;
            ordersSb.Clear();
            for (int i = 0; i < m.Active.objectives.Length; i++)
            {
                var o = m.Active.objectives[i]; if (!o.optional) continue;
                if (m.done[i]) ordersSb.Append("<color=#86D8A0>DONE  ").Append(o.title).Append("  +").Append(o.reward).Append("</color>\n");
                else
                {
                    ordersSb.Append(o.title);
                    if (o.target > 1) ordersSb.Append("<color=#B7C9BF> ").Append(m.progress[i]).Append('/').Append(o.target).Append("</color>");
                    ordersSb.Append("  <color=#FFBC4A>").Append(o.reward).Append("</color>\n");
                }
            }
            for (int i = 0; i < m.Active.challenges.Length; i++)
            {
                var c = m.Active.challenges[i];
                ordersSb.Append("<color=#9FD3FF>* ").Append(c.title).Append("</color>  <color=#B7C9BF>").Append(m.ChallengeStatus(i)).Append("</color>\n");
            }
            var text = ordersSb.ToString();
            if (text != lastOrders) orders.text = lastOrders = text;
        }

        void DrawPrompt(bool playing)
        {
            if (!playing) return;
            string text = "";
            var p = game.player;
            var station = WinchPost.Hauling ? null : Station.Nearest(p.transform.position);
            var mess = MessField.Instance;
            if (WinchPost.Hauling) text = "Winching up...";
            else if (station) text = station.Prompt;
            else if (!game.Nearest && mess && mess.Aimed)
            {
                var need = ItemCatalog.Get(ItemCatalog.ForCareTool(mess.Aimed.Tool));
                bool right = p.selected == mess.Aimed.Tool;
                text = right ? $"{mess.Aimed.Label}  ·  HOLD USE  {mess.Aimed.progress:P0}" : $"{mess.Aimed.Label}  ·  needs the <color=#FFBC4A>{need.name}</color>";
            }
            else
            {
                var def = economy.Inventory.SelectedDef;
                var ab = p.GetComponent<ToolAbilities>();
                if (def != null && ab)
                {
                    if (def.id == ItemId.Grabber) text = "HOLD USE to pull loose salvage toward you";
                    else if (def.id == ItemId.Scanner) text = ab.ScanReady > 0 ? $"Scanner recharging {ab.ScanReady:0}s" : "USE to ping nearby mess and finds";
                    else if (def.id == ItemId.SnackLauncher) text = ab.SnackReady > 0 ? $"Snack launcher reloading {ab.SnackReady:0}s" : "USE to lob a snack toward Mossback's mouth";
                    else if (def.id == ItemId.Glider) text = ab.Gliding ? "Gliding..." : "Equipped: falls become gentle glides";
                    else if (def.placeable && PlacementSystem.Instance) text = PlacementSystem.Instance.Hint;
                    else if (def.id == ItemId.Claw) { var claw = ClimbingClaw.For(p); text = claw && claw.Exhausted ? "Claw stamina spent: rest on solid ground" : claw && claw.Active ? $"Clawing up · stamina {claw.stamina:0}%" : "Walk into Mossback's flanks to climb · SPACE to let go"; }
                }
            }
            prompt.text = text;
        }

        void DrawToasts()
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var (rt, label, born) = toasts[i];
                float age = Time.unscaledTime - born;
                if (age > 4.5f || !rt) { if (rt) Destroy(rt.gameObject); toasts.RemoveAt(i); continue; }
                var target = new Vector2(-28, -250 - i * 46);
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, target, Time.unscaledDeltaTime * 12);
                float a = Mathf.Clamp01((4.5f - age) / .6f) * Mathf.Clamp01(age / .15f);
                var img = rt.GetComponent<Image>(); var c = UIKit.Ink; c.a *= a; img.color = c;
                label.alpha = a;
            }
        }

        // ---- Drag and drop ----
        public void BeginDrag(int index)
        {
            var inv = economy.Inventory;
            if (!IsOpen || inv.slots[index].IsEmpty) return;
            DragFrom = index;
            var def = ItemCatalog.Get(inv.slots[index].id);
            UIKit.SetIcon(ghostSwatch, ghostGlyph, def);
            ghostCount.text = inv.slots[index].count > 1 ? inv.slots[index].count.ToString() : "";
            ghost.gameObject.SetActive(true);
            ghost.SetAsLastSibling();
        }

        public void DropOn(int index)
        {
            if (DragFrom < 0) return;
            economy.Inventory.MoveOrSwap(DragFrom, index);
            ProceduralSfx.Play(ProceduralSfx.Equip, .3f);
            EndDrag();
        }

        public void EndDrag(bool outside = false)
        {
            if (DragFrom >= 0 && outside) DropFromSlot(DragFrom, 999);
            DragFrom = -1;
            if (ghost) ghost.gameObject.SetActive(false);
        }

        public void QuickMove(int index) { economy.Inventory.QuickMove(index); ProceduralSfx.Play(ProceduralSfx.Equip, .3f); }

        public void DropFromSlot(int index, int amount)
        {
            var inv = economy.Inventory; var s = inv.slots[index];
            if (s.IsEmpty) return;
            var def = ItemCatalog.Get(s.id);
            if (def.IsTool) { game.Caption = $"Tools stay clipped to your harness. Drag the {def.name} into the backpack instead."; ProceduralSfx.Play(ProceduralSfx.Deny, .5f); return; }
            var taken = inv.Take(index, amount);
            var p = game.player;
            var forward = p.model ? p.model.forward : p.transform.forward;
            DebrisPickup.Spawn(taken.id, p.transform.position + Vector3.up * 1.2f + forward * .6f, forward * 3.5f + Vector3.up * 2.5f, taken.count);
            ProceduralSfx.Play(ProceduralSfx.Whoosh, .3f);
        }

        public void ShowTooltip(SlotView v)
        {
            var s = economy.Inventory.slots[v.index];
            if (s.IsEmpty || !IsOpen) { tooltip.gameObject.SetActive(false); return; }
            var def = ItemCatalog.Get(s.id);
            string value = def.IsDebris ? $"\n<color=#FFBC4A>Sells for {def.sellValue} each · {def.sellValue * s.count} total</color>" : "\n<color=#86D8A0>Tool</color>";
            tooltipText.text = $"<b>{def.name}</b>\n<color=#B7C9BF>{def.blurb}</color>{value}";
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();
            tooltip.position = v.background.rectTransform.position + new Vector3(Slot * .6f, Slot * .6f, 0);
        }
        public void HideTooltip() { if (tooltip) tooltip.gameObject.SetActive(false); }
    }

    public sealed class SlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public HotbarUI ui;
        public int index;
        public Image background, swatch;
        public TextMeshProUGUI glyph, count, key;
        public bool hover;

        public void OnBeginDrag(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) ui.BeginDrag(index); }
        public void OnDrag(PointerEventData e) { }
        public void OnEndDrag(PointerEventData e)
        {
            if (ui.DragFrom != index) return;
            var over = e.pointerCurrentRaycast.gameObject;
            bool outside = over == null;
            ui.EndDrag(outside);
        }
        public void OnDrop(PointerEventData e) { ui.DropOn(index); }
        public void OnPointerClick(PointerEventData e)
        {
            if (!ui.IsOpen) return;
            var kb = Keyboard.current;
            if (e.button == PointerEventData.InputButton.Left && kb != null && kb.shiftKey.isPressed) ui.QuickMove(index);
            else if (e.button == PointerEventData.InputButton.Right) ui.DropFromSlot(index, 1);
        }
        public void OnPointerEnter(PointerEventData e) { hover = true; ui.ShowTooltip(this); }
        public void OnPointerExit(PointerEventData e) { hover = false; ui.HideTooltip(); }
    }
}

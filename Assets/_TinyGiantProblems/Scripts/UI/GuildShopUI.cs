using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TinyGiantProblems
{
    // The Guild Tool Locker: spend coins earned from work orders and salvage.
    public sealed class GuildShopUI : MonoBehaviour, GameUI.IWindow
    {
        public static GuildShopUI Instance { get; private set; }
        public bool IsOpen => window && window.activeSelf;
        GameObject window;
        TextMeshProUGUI coins, footer;
        float openedAt;
        sealed class Row { public ItemDef def; public Button buy; public TextMeshProUGUI price, state; public Image card; }
        readonly List<Row> rows = new List<Row>();

        void Awake() { Instance = this; GameUI.Register(this); }
        void OnDestroy() { GameUI.Unregister(this); if (Instance == this) Instance = null; }

        void Start()
        {
            var root = UIKit.MakeCanvas("Guild Tool Locker UI", 20, out _);
            root.SetParent(transform, false);
            var panel = UIKit.Panel(root, "Locker window", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1280, 690), UIKit.Ink, true);
            window = panel.gameObject;
            var t = panel.transform;
            UIKit.Label(t, "SMALL WORKS GUILD  /  TOOL LOCKER", 15, UIKit.Gold, new Vector2(0, 1), new Vector2(34, -24), new Vector2(500, 24));
            UIKit.Label(t, "Gear up for Mossback", 34, UIKit.Cream, new Vector2(0, 1), new Vector2(34, -50), new Vector2(560, 46));
            coins = UIKit.Label(t, "", 26, UIKit.Gold, new Vector2(1, 1), new Vector2(-34, -40), new Vector2(300, 40), TextAlignmentOptions.TopRight);
            var defs = ItemCatalog.ShopOrder;
            for (int i = 0; i < defs.Count; i++)
            {
                // Three columns so tools, upgrades and platform kits all fit at 1600x900.
                int col = i % 3, row = i / 3;
                var card = UIKit.Panel(t, defs[i].name, new Vector2(0, 1), new Vector2(34 + col * 404, -104 - row * 98), new Vector2(390, 90), UIKit.InkLight);
                var iconHolder = UIKit.Rect(card.transform, "Icon", new Vector2(0, .5f), new Vector2(14, 0), new Vector2(64, 64));
                UIKit.Icon(iconHolder, defs[i], 64, out _, out _);
                UIKit.Label(card.transform, defs[i].name, 20, UIKit.Cream, new Vector2(0, 1), new Vector2(92, -10), new Vector2(180, 28));
                UIKit.Label(card.transform, defs[i].blurb, 13, UIKit.Muted, new Vector2(0, 1), new Vector2(92, -36), new Vector2(180, 52));
                var r = new Row { def = defs[i], card = card };
                var def = defs[i];
                r.buy = UIKit.MakeButton(card.transform, "", new Vector2(1, .5f), new Vector2(-12, 0), new Vector2(96, 54), () => Buy(def), out r.price);
                r.price.fontSize = 18;
                r.state = UIKit.Label(card.transform, "", 13, UIKit.Muted, new Vector2(1, 0), new Vector2(-12, 6), new Vector2(110, 18), TextAlignmentOptions.BottomRight);
                rows.Add(r);
            }
            footer = UIKit.Label(t, "", 15, UIKit.Muted, new Vector2(0, 0), new Vector2(34, 30), new Vector2(560, 44));
            UIKit.MakeButton(t, "CLOSE  (E / ESC)", new Vector2(1, 0), new Vector2(-34, 26), new Vector2(220, 46), Close, out _);
            window.SetActive(false);
        }

        public void Open()
        {
            if (!window) return;
            window.SetActive(true); openedAt = Time.unscaledTime;
            Refresh(); GameUI.RefreshCursor();
        }

        public void Close()
        {
            if (!window) return;
            window.SetActive(false); GameUI.MarkClosed(); GameUI.RefreshCursor();
        }

        void Buy(ItemDef def)
        {
            var e = GuildEconomy.Instance; if (!e) return;
            if (e.TryBuy(def) && def.IsTool)
                footer.text = $"<color=#86D8A0>{def.name} clipped to your bag.</color> Press TAB to arrange your hotbar.";
            Refresh();
        }

        void Update()
        {
            if (!IsOpen) return;
            if ((GameInput.Interact.WasPressedThisFrame() || GameInput.Brace.WasPressedThisFrame()) && Time.unscaledTime - openedAt > .2f) { Close(); return; }
            Refresh();
        }

        void Refresh()
        {
            var e = GuildEconomy.Instance; if (!e) return;
            coins.text = $"{e.Coins:N0} coins";
            foreach (var r in rows)
            {
                bool owned = e.Owns(r.def.id);
                bool can = e.CanBuy(r.def, out var reason);
                r.buy.interactable = can;
                r.price.text = owned ? "OWNED" : $"{r.def.price}";
                r.price.color = owned ? UIKit.Good : can ? UIKit.Cream : UIKit.Bad;
                r.state.text = owned ? "" : can ? "click to buy" : reason;
                r.card.color = owned ? new Color(.1f, .22f, .17f, .96f) : UIKit.InkLight;
            }
            if (string.IsNullOrEmpty(footer.text) || !footer.text.StartsWith("<color=#86D8A0>"))
                footer.text = "Earn coins by finishing work orders and selling salvage at the yard below Mossback.";
        }
    }
}

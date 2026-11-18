using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyGiantProblems
{
    // Briefing-screen "Guild Member" editor: a live, slowly turning 3D preview of
    // the technician plus per-part clothing colours (preset swatches and
    // hue / saturation / brightness sliders, randomise, reset). Colours are
    // stored by TechnicianLook and applied to the in-game technician at once.
    public sealed class GuildMemberEditor : MonoBehaviour, GameUI.IWindow
    {
        public static GuildMemberEditor Instance { get; private set; }
        const int PreviewLayer = 30;
        GameObject window, stage;
        Transform model; Camera previewCam; RenderTexture previewRT;
        int part;
        readonly List<Image> partButtons = new List<Image>();
        readonly List<Image> partChips = new List<Image>();
        Slider hue, sat, val;
        Image current;
        bool syncing;
        float yaw = 200, drag;

        public bool IsOpen => window && window.activeSelf;

        void Awake() { Instance = this; GameUI.Register(this); }
        void OnDestroy() { GameUI.Unregister(this); if (Instance == this) Instance = null; if (previewRT) previewRT.Release(); if (stage) Destroy(stage); }

        void Start()
        {
            var root = UIKit.MakeCanvas("Guild Member editor", 25, out _);
            root.SetParent(transform, false);
            var panel = UIKit.Panel(root, "Guild member window", new Vector2(.5f, .5f), new Vector2(150, 0), new Vector2(980, 640), UIKit.Ink, true);
            window = panel.gameObject;
            var t = panel.transform;
            UIKit.Label(t, "SMALL WORKS GUILD  /  GUILD MEMBER", 15, UIKit.Gold, new Vector2(0, 1), new Vector2(30, -22), new Vector2(500, 24));
            UIKit.Label(t, "Dress your technician", 32, UIKit.Cream, new Vector2(0, 1), new Vector2(30, -48), new Vector2(560, 44));

            // Live preview.
            var frame = UIKit.Panel(t, "Preview frame", new Vector2(0, 1), new Vector2(30, -106), new Vector2(400, 470), UIKit.InkLight, true);
            previewRT = new RenderTexture(560, 660, 24) { name = "Guild member preview", antiAliasing = 4 };
            var raw = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            raw.transform.SetParent(frame.transform, false);
            UIKit.Place((RectTransform)raw.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(380, 450));
            raw.texture = previewRT;
            UIKit.Label(frame.transform, "drag to turn", 13, UIKit.Muted, new Vector2(.5f, 0), new Vector2(0, 6), new Vector2(200, 18), TextAlignmentOptions.Bottom);

            // Parts.
            UIKit.Label(t, "PART", 14, UIKit.Gold, new Vector2(0, 1), new Vector2(460, -108), new Vector2(200, 20));
            for (int i = 0; i < TechnicianLook.PartLabels.Length; i++)
            {
                int index = i;
                var b = UIKit.MakeButton(t, "", new Vector2(0, 1), new Vector2(460 + (i % 2) * 245, -132 - (i / 2) * 50), new Vector2(235, 42), () => SelectPart(index), out var label);
                ((RectTransform)b.transform).pivot = new Vector2(0, 1);
                label.text = TechnicianLook.PartLabels[i]; label.alignment = TextAlignmentOptions.Left; label.fontSize = 17; label.margin = new Vector4(44, 0, 0, 0);
                var chip = UIKit.Panel(b.transform, "Chip", new Vector2(0, .5f), new Vector2(10, 0), new Vector2(24, 24), Color.white);
                chip.rectTransform.pivot = new Vector2(0, .5f);
                partButtons.Add(b.GetComponent<Image>()); partChips.Add(chip);
            }

            // Swatches.
            UIKit.Label(t, "COLOUR", 14, UIKit.Gold, new Vector2(0, 1), new Vector2(460, -340), new Vector2(200, 20));
            for (int i = 0; i < TechnicianLook.Swatches.Length; i++)
            {
                var c = TechnicianLook.Swatches[i];
                var b = UIKit.MakeButton(t, "", new Vector2(0, 1), new Vector2(460 + i * 48, -364), new Vector2(40, 40), () => Pick(c), out _);
                ((RectTransform)b.transform).pivot = new Vector2(0, 1);
                b.GetComponent<Image>().color = c;
                var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); b.colors = colors;
            }
            current = UIKit.Panel(t, "Current", new Vector2(0, 1), new Vector2(460, -420), new Vector2(40, 132), Color.white);
            current.rectTransform.pivot = new Vector2(0, 1);
            hue = MakeSlider(t, "Hue", -424); sat = MakeSlider(t, "Saturation", -468); val = MakeSlider(t, "Brightness", -512);

            UIKit.MakeButton(t, "RANDOMISE", new Vector2(0, 0), new Vector2(460, 26), new Vector2(160, 46), () => { TechnicianLook.Randomize(); Sync(); }, out _).GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            UIKit.MakeButton(t, "RESET", new Vector2(0, 0), new Vector2(632, 26), new Vector2(120, 46), () => { TechnicianLook.ResetAll(); Sync(); }, out _).GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            UIKit.MakeButton(t, "DONE", new Vector2(1, 0), new Vector2(-30, 26), new Vector2(170, 46), Close, out _);
            BuildStage();
            window.SetActive(false);
        }

        Slider MakeSlider(Transform parent, string name, float y)
        {
            UIKit.Label(parent, name, 15, UIKit.Muted, new Vector2(0, 1), new Vector2(516, y - 4), new Vector2(110, 22));
            var bg = UIKit.Panel(parent, name + " slider", new Vector2(0, 1), new Vector2(630, y - 10), new Vector2(300, 14), new Color(.2f, .3f, .3f), true);
            bg.rectTransform.pivot = new Vector2(0, 1);
            var fillArea = UIKit.Rect(bg.transform, "Fill area", new Vector2(.5f, .5f), Vector2.zero, new Vector2(300, 14));
            var fill = UIKit.Panel(fillArea, "Fill", new Vector2(0, .5f), Vector2.zero, new Vector2(0, 14), UIKit.Gold);
            fill.rectTransform.anchorMin = new Vector2(0, 0); fill.rectTransform.anchorMax = new Vector2(1, 1); fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = UIKit.Rect(bg.transform, "Handle area", new Vector2(.5f, .5f), Vector2.zero, new Vector2(286, 14));
            var handle = UIKit.Panel(handleArea, "Handle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(16, 26), UIKit.Cream, true);
            handle.rectTransform.anchorMin = new Vector2(0, .5f); handle.rectTransform.anchorMax = new Vector2(0, .5f);
            var s = bg.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = 0; s.maxValue = 1; s.direction = Slider.Direction.LeftToRight;
            s.onValueChanged.AddListener(_ => FromSliders());
            return s;
        }

        void BuildStage()
        {
            stage = new GameObject("Guild member preview stage");
            stage.transform.position = new Vector3(0, -2500, 0);
            var go = GameArt.Model(TechnicianModelInstaller.Key, stage.transform, TechnicianModelInstaller.Height);
            if (go)
            {
                model = go.transform;
                foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = PreviewLayer;
                var rig = go.GetComponentInChildren<TechnicianRig>(); if (rig) rig.enabled = false;
            }
            var key = new GameObject("Preview key").AddComponent<Light>();
            key.transform.SetParent(stage.transform, false); key.type = LightType.Directional; key.cullingMask = 1 << PreviewLayer;
            key.intensity = 1.25f; key.color = new Color(1, .95f, .86f); key.transform.rotation = Quaternion.Euler(35, -30, 0);
            var fill = new GameObject("Preview fill").AddComponent<Light>();
            fill.transform.SetParent(stage.transform, false); fill.type = LightType.Directional; fill.cullingMask = 1 << PreviewLayer;
            fill.intensity = .55f; fill.color = new Color(.7f, .82f, 1); fill.transform.rotation = Quaternion.Euler(15, 150, 0);
            previewCam = new GameObject("Preview camera").AddComponent<Camera>();
            previewCam.transform.SetParent(stage.transform, false);
            previewCam.cullingMask = 1 << PreviewLayer; previewCam.clearFlags = CameraClearFlags.SolidColor; previewCam.backgroundColor = new Color(.09f, .18f, .19f, 1);
            previewCam.fieldOfView = 28; previewCam.nearClipPlane = .05f; previewCam.farClipPlane = 30;
            previewCam.transform.localPosition = new Vector3(0, .95f, 4.6f); previewCam.transform.LookAt(stage.transform.position + Vector3.up * .78f);
            previewCam.targetTexture = previewRT; previewCam.enabled = false;
        }

        public void Open()
        {
            if (!window) return;
            window.SetActive(true);
            if (previewCam) previewCam.enabled = true;
            SelectPart(part);
            GameUI.RefreshCursor();
        }

        public void Close()
        {
            if (!window) return;
            window.SetActive(false);
            if (previewCam) previewCam.enabled = false;
            GameUI.MarkClosed(); GameUI.RefreshCursor();
        }

        void SelectPart(int index) { part = index; Sync(); }
        void Pick(Color c) { TechnicianLook.Set(part, c); Sync(); }

        void FromSliders()
        {
            if (syncing) return;
            TechnicianLook.Set(part, Color.HSVToRGB(hue.value, sat.value, val.value));
            Refresh();
        }

        void Sync()
        {
            syncing = true;
            Color.RGBToHSV(TechnicianLook.Get(part), out var h, out var s, out var v);
            hue.value = h; sat.value = s; val.value = v;
            syncing = false;
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < partButtons.Count; i++)
            {
                partButtons[i].color = i == part ? new Color(.36f, .55f, .48f) : UIKit.ButtonColor;
                partChips[i].color = TechnicianLook.Get(i);
            }
            current.color = TechnicianLook.Get(part);
            if (model) foreach (var r in model.GetComponentsInChildren<Renderer>()) TechnicianLook.Apply(r);
        }

        void Update()
        {
            if (!IsOpen) return;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed && mouse.position.ReadValue().x < Screen.width * .45f) drag = mouse.delta.ReadValue().x * .5f;
            else drag = Mathf.Lerp(drag, 18 * Time.unscaledDeltaTime, .1f);
            yaw += drag;
            if (model) model.localRotation = Quaternion.Euler(0, yaw, 0);
        }
    }
}

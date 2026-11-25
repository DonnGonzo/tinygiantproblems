using System;
using UnityEngine;

namespace TinyGiantProblems
{
    // Clothing colours of the guild technician. The model is split into colour
    // regions (one submesh + material each, see Tools/import_new_models.py), and
    // these colours tint them through MaterialPropertyBlocks. Chosen in the
    // Guild Member editor on the briefing screen; saved in PlayerPrefs.
    public static class TechnicianLook
    {
        public const int Hat = 0, Jacket = 1, Trousers = 2, Gloves = 3, Boots = 4, Belt = 5, Accents = 6, Goggles = 7;
        public static readonly string[] RegionNames = { "Hat", "Jacket", "Trousers", "Gloves", "Boots", "Belt", "Accents", "Goggles" };
        // The parts the editor lets you change (goggle lenses stay dark).
        public static readonly string[] PartLabels = { "Hood & hat", "Jacket", "Trousers", "Gloves", "Boots", "Belt", "Trim & buttons" };
        public static readonly Color[] Defaults =
        {
            new Color(.07f, .33f, .23f), new Color(.09f, .38f, .27f), new Color(.38f, .06f, .11f),
            new Color(.86f, .57f, .07f), new Color(.78f, .5f, .07f), new Color(.7f, .45f, .06f),
            new Color(.74f, .58f, .3f), new Color(.06f, .05f, .05f),
        };
        public static readonly Color[] Swatches =
        {
            new Color(.07f, .33f, .23f), new Color(.12f, .3f, .55f), new Color(.38f, .06f, .11f), new Color(.86f, .57f, .07f),
            new Color(.55f, .3f, .14f), new Color(.85f, .82f, .72f), new Color(.2f, .2f, .22f), new Color(.6f, .2f, .55f),
            new Color(.85f, .35f, .2f), new Color(.45f, .7f, .3f),
        };
        public const string SaveKey = "TGP.Look.v1";
        public static event Action Changed;
        static Color[] current;
        static MaterialPropertyBlock block;

        [Serializable] sealed class Data { public Color[] colors; }

        public static Color[] Current { get { if (current == null) Load(); return current; } }
        public static Color Get(int region) => Current[region];
        public static void Set(int region, Color c) { Current[region] = c; Save(); Changed?.Invoke(); }
        public static void ResetAll() { current = (Color[])Defaults.Clone(); Save(); Changed?.Invoke(); }
        public static void Randomize()
        {
            var c = Current;
            for (int i = 0; i < PartLabels.Length; i++)
            {
                Color.RGBToHSV(Swatches[UnityEngine.Random.Range(0, Swatches.Length)], out var h, out var s, out var v);
                c[i] = Color.HSVToRGB(Mathf.Repeat(h + UnityEngine.Random.Range(-.04f, .04f), 1), Mathf.Clamp01(s * UnityEngine.Random.Range(.85f, 1.1f)), Mathf.Clamp01(v * UnityEngine.Random.Range(.85f, 1.1f)));
            }
            // Jacket usually matches the hood; keep that unless the dice say otherwise.
            if (UnityEngine.Random.value < .6f) c[Jacket] = c[Hat] * 1.12f;
            Save(); Changed?.Invoke();
        }

        public static string ToJson(Color[] colors) => JsonUtility.ToJson(new Data { colors = colors });
        public static Color[] FromJson(string json)
        {
            var result = (Color[])Defaults.Clone();
            if (string.IsNullOrEmpty(json)) return result;
            try
            {
                var d = JsonUtility.FromJson<Data>(json);
                if (d?.colors != null) for (int i = 0; i < Mathf.Min(d.colors.Length, result.Length); i++) result[i] = d.colors[i];
            }
            catch { }
            return result;
        }
        static void Load() => current = FromJson(PlayerPrefs.GetString(SaveKey, ""));
        static void Save() { PlayerPrefs.SetString(SaveKey, ToJson(current)); PlayerPrefs.Save(); }

        public static void Apply(Renderer r)
        {
            if (!r) return;
            block ??= new MaterialPropertyBlock();
            int n = Mathf.Min(r.sharedMaterials.Length, Current.Length);
            for (int i = 0; i < n; i++)
            {
                r.GetPropertyBlock(block, i);
                block.SetColor("_BaseColor", Current[i]);
                r.SetPropertyBlock(block, i);
            }
        }
    }
}

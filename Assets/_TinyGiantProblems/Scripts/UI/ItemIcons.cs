using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // Picture icons for items: a 3/4 view of each item's real model on a
    // transparent background. Baked PNGs live in Resources/ItemIcons/<ItemId>.png
    // (menu: Tiny Giant Problems/Bake item icons); anything missing is rendered
    // once at runtime and cached. Items with no model keep their letter tile.
    public static class ItemIcons
    {
        const int Layer = 31;
        static readonly Dictionary<ItemId, Sprite> cache = new Dictionary<ItemId, Sprite>();

        public static bool HasPicture(ItemDef def) => def != null && (!string.IsNullOrEmpty(def.model) || def.id == ItemId.GuildBadge);

        public static Sprite Get(ItemDef def)
        {
            if (!HasPicture(def)) return null;
            if (cache.TryGetValue(def.id, out var s) && s) return s;
            var baked = Resources.Load<Texture2D>("ItemIcons/" + def.id);
            var tex = baked ? baked : Render(def, 128);
            s = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100) : null;
            if (s) s.name = def.name + " icon";
            cache[def.id] = s;
            return s;
        }

        // Renders the item model, framed to its bounds, on a transparent texture.
        public static Texture2D Render(ItemDef def, int size)
        {
            var stage = new GameObject("Icon stage");
            stage.transform.position = new Vector3(0, -4000, 0);
            GameObject model = null; Camera cam = null; Light key = null, fill = null; RenderTexture rt = null;
            try
            {
                model = GameArt.ItemVisual(def, stage.transform, 1);
                if (!model) return null;
                model.transform.localRotation = Quaternion.Euler(0, IconYaw(def.id), 0);
                foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return null;
                var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);

                key = new GameObject("Icon key").AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = 1.3f; key.cullingMask = 1 << Layer; key.color = new Color(1, .96f, .88f);
                key.transform.rotation = Quaternion.Euler(40, -35, 0);
                fill = new GameObject("Icon fill").AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = .55f; fill.cullingMask = 1 << Layer; fill.color = new Color(.75f, .85f, 1);
                fill.transform.rotation = Quaternion.Euler(10, 150, 0);

                cam = new GameObject("Icon camera").AddComponent<Camera>();
                cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
                cam.orthographic = true; cam.nearClipPlane = .01f; cam.farClipPlane = 100; cam.allowHDR = false; cam.allowMSAA = true;
                var view = Quaternion.Euler(24, 0, 0);
                cam.transform.rotation = view;
                cam.transform.position = b.center - view * Vector3.forward * (b.extents.magnitude * 4 + 1);
                // Fit the bounds' projected extent in camera space.
                float half = 0;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) * 2 - 1, (i & 2) - 1, (i & 4) / 2 - 1));
                    var local = cam.transform.InverseTransformPoint(corner);
                    half = Mathf.Max(half, Mathf.Abs(local.x), Mathf.Abs(local.y));
                }
                cam.orthographicSize = half * 1.08f;
                rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 4;
                cam.targetTexture = rt;
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = def.id + " icon" };
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                return tex;
            }
            finally
            {
                if (cam) { cam.targetTexture = null; Kill(cam.gameObject); }
                if (rt) RenderTexture.ReleaseTemporary(rt);
                if (key) Kill(key.gameObject); if (fill) Kill(fill.gameObject);
                Kill(stage);
            }
        }

        // Turn tools that lie along their X axis so their working end shows.
        static float IconYaw(ItemId id)
        {
            switch (id)
            {
                case ItemId.Vacuum: case ItemId.Sprayer: case ItemId.RopeLauncher: case ItemId.Grabber: case ItemId.SnackLauncher: return -130;
                case ItemId.Platform: return 210;
                default: return 200;
            }
        }

        static void Kill(Object o) { if (!o) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }
    }
}

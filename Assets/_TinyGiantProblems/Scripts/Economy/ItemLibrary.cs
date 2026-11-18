using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // Resources/ItemLibrary maps short keys ("pest-vacuum/pest-vacuum") to the
    // imported GiantTiny prefabs so runtime systems can spawn real models.
    [CreateAssetMenu(menuName = "Tiny Giant Problems/Item Library")]
    public sealed class ItemLibrary : ScriptableObject
    {
        [Serializable] public struct Entry { public string key; public GameObject prefab; }
        public Entry[] entries = new Entry[0];
        public Material baseMaterial;
        public Material particleMaterial;

        static ItemLibrary cached;
        static bool searched;
        Dictionary<string, GameObject> lookup;

        public static ItemLibrary Instance
        {
            get
            {
                if (!searched) { cached = Resources.Load<ItemLibrary>("ItemLibrary"); searched = true; }
                return cached;
            }
        }

        public GameObject Prefab(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (lookup == null)
            {
                lookup = new Dictionary<string, GameObject>();
                foreach (var e in entries) if (e.prefab && !string.IsNullOrEmpty(e.key)) lookup[e.key] = e.prefab;
            }
            return lookup.TryGetValue(key, out var p) ? p : null;
        }
    }

    // Small helpers shared by every runtime-built prop.
    public static class GameArt
    {
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        static Material fallbackBase;

        public static Material Colored(Color color, float smoothness = .25f, float emission = 0)
        {
            var key = new Color(color.r, color.g, color.b, smoothness + emission * 10);
            if (materials.TryGetValue(key, out var m) && m) return m;
            var lib = ItemLibrary.Instance;
            var source = lib && lib.baseMaterial ? lib.baseMaterial : FallbackBase();
            m = new Material(source) { name = "Runtime " + ColorUtility.ToHtmlStringRGB(color) };
            m.color = color;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (emission > 0 && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            materials[key] = m;
            return m;
        }

        static Material FallbackBase()
        {
            if (fallbackBase) return fallbackBase;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            fallbackBase = new Material(shader);
            return fallbackBase;
        }

        public static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Color color, float smoothness = .25f, float emission = 0)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go.GetComponent<Collider>()); else UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Colored(color, smoothness, emission);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // Instantiates a library model (unit-sized, ground pivot) or returns null.
        public static GameObject Model(string key, Transform parent, float worldSize)
        {
            var lib = ItemLibrary.Instance;
            var prefab = lib ? lib.Prefab(key) : null;
            if (!prefab) return null;
            var go = UnityEngine.Object.Instantiate(prefab, parent, false);
            go.name = key;
            foreach (var c in go.GetComponentsInChildren<Collider>()) { if (Application.isPlaying) UnityEngine.Object.Destroy(c); else UnityEngine.Object.DestroyImmediate(c); }
            float parentScale = parent ? Mathf.Max(.0001f, parent.lossyScale.x) : 1;
            go.transform.localScale = Vector3.one * worldSize / parentScale;
            return go;
        }

        // Visual for an item: imported model when available, else a readable stand-in.
        public static GameObject ItemVisual(ItemDef def, Transform parent, float worldSize)
        {
            var go = Model(def.model, parent, worldSize);
            if (go) return go;
            var root = new GameObject(def.name);
            root.transform.SetParent(parent, false);
            float s = worldSize / (parent ? Mathf.Max(.0001f, parent.lossyScale.x) : 1);
            switch (def.id)
            {
                case ItemId.GuildBadge:
                    Shape("Badge", PrimitiveType.Cylinder, root.transform, Vector3.up * s * .1f, new Vector3(s, s * .08f, s), def.color, .8f, .4f).transform.localRotation = Quaternion.Euler(70, 0, 0);
                    break;
                case ItemId.Burr:
                    Shape("Burr core", PrimitiveType.Sphere, root.transform, Vector3.up * s * .4f, Vector3.one * s * .7f, def.color);
                    for (int i = 0; i < 8; i++)
                    {
                        var spike = Shape("Spike", PrimitiveType.Capsule, root.transform, Vector3.up * s * .4f, new Vector3(s * .12f, s * .45f, s * .12f), def.color * .8f);
                        spike.transform.localRotation = Quaternion.Euler(i * 45, i * 70, 0);
                    }
                    break;
                default:
                    Shape(def.name, PrimitiveType.Sphere, root.transform, Vector3.up * s * .4f, new Vector3(s, s * .75f, s), def.color);
                    break;
            }
            return root;
        }

        public static ParticleSystem Particles(string name, Transform parent, Color color, float size, float speed, float life, bool world = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startColor = color;
            main.startSize = new ParticleSystem.MinMaxCurve(size * .6f, size * 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .5f, speed);
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * .6f, life);
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = 400;
            main.gravityModifier = 0;
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .5f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, .6f, 1, 1.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            var lib = ItemLibrary.Instance;
            if (lib && lib.particleMaterial) r.sharedMaterial = lib.particleMaterial;
            else r.sharedMaterial = Colored(color);
            return ps;
        }

        public static void Burst(ParticleSystem ps, Vector3 position, int count, Vector3 velocity = default, float spread = 1)
        {
            if (!ps) return;
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            for (int i = 0; i < count; i++)
            {
                p.position = position;
                p.velocity = velocity + UnityEngine.Random.insideUnitSphere * spread;
                ps.Emit(p, 1);
            }
        }
    }
}

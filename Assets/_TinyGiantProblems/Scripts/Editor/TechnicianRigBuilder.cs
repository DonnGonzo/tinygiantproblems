using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGiantProblems.Editor
{
    // Builds technician/technician-rigged.prefab: the imported (unrigged)
    // technician mesh skinned to a 17-bone code skeleton with region-based
    // weights, turned to face +Z, plus right/left hand anchors for tools.
    // Coordinates below are in the normalised prefab space (height 1, ground 0).
    // To swap in a Mixamo FBX later, replace this prefab's mesh and bones and keep
    // the anchors; ClimbingClaw, FallHurt and the tool visuals do not change.
    public static class TechnicianRigBuilder
    {
        public const string Key = "technician/technician-rigged";
        const string Folder = GiantTinyAssets.Root + "/technician";

        static float S(float a, float b, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, x));

        static readonly (string name, int parent, Vector3 pos)[] Layout =
        {
            ("Hips", -1, new Vector3(0, .30f, 0)),
            ("Spine", 0, new Vector3(0, .40f, 0)),
            ("Chest", 1, new Vector3(0, .50f, 0)),
            ("Head", 2, new Vector3(0, .60f, 0)),
            ("Hat tip", 3, new Vector3(.06f, .80f, -.02f)),
            ("Upper arm L", 2, new Vector3(-.20f, .56f, 0)),
            ("Forearm L", 5, new Vector3(-.235f, .47f, .01f)),
            ("Hand L", 6, new Vector3(-.255f, .405f, .02f)),
            ("Upper arm R", 2, new Vector3(.20f, .56f, 0)),
            ("Forearm R", 8, new Vector3(.235f, .47f, .01f)),
            ("Hand R", 9, new Vector3(.255f, .405f, .02f)),
            ("Thigh L", 0, new Vector3(-.10f, .29f, 0)),
            ("Shin L", 11, new Vector3(-.125f, .16f, .01f)),
            ("Foot L", 12, new Vector3(-.14f, .06f, .03f)),
            ("Thigh R", 0, new Vector3(.10f, .29f, 0)),
            ("Shin R", 14, new Vector3(.125f, .16f, .01f)),
            ("Foot R", 15, new Vector3(.14f, .06f, .03f)),
        };

        static BoneWeight Weight(Vector3 p, bool trousers)
        {
            var w = new Dictionary<int, float>();
            void Add(int bone, float value) { if (value > 1e-4f) w[bone] = (w.TryGetValue(bone, out var o) ? o : 0) + value; }
            float ax = Mathf.Abs(p.x), y = p.y;
            bool left = p.x < 0;
            // Gloves hang beside the baggy trousers: only the outer loops above the
            // trouser hem (y > .32) belong to the arms.
            float arm = S(.203f, .222f, ax) * S(.315f, .335f, y) * (1 - S(.57f, .62f, y));
            arm = Mathf.Max(arm, S(.19f, .21f, ax) * S(.47f, .5f, y) * (1 - S(.57f, .62f, y)));
            // Maroon trouser cloth pressed against a glove never follows the arm.
            if (trousers && y < .47f) arm = 0;
            float leg = (1 - S(.19f, .29f, y)) * (1 - arm);
            float torso = 1 - arm - leg;
            if (arm > 0)
            {
                int b = left ? 5 : 8;
                float hand = 1 - S(.40f, .45f, y), fore = (1 - hand) * (1 - S(.48f, .53f, y)), upper = 1 - hand - fore;
                Add(b, arm * upper); Add(b + 1, arm * fore); Add(b + 2, arm * hand);
            }
            if (leg > 0)
            {
                float right = S(-.06f, .06f, p.x);
                float shin = 1 - S(.15f, .21f, y), thigh = 1 - shin;
                float foot = shin * (1 - S(.05f, .09f, y)) * S(-.02f, .08f, p.z) * .8f;
                shin -= foot;
                for (int side = 0; side < 2; side++)
                {
                    float k = leg * (side == 0 ? 1 - right : right);
                    int b = side == 0 ? 11 : 14;
                    Add(b, k * thigh); Add(b + 1, k * shin); Add(b + 2, k * foot);
                }
            }
            if (torso > 0)
            {
                float hips = 1 - S(.33f, .42f, y);
                float spine = S(.33f, .42f, y) * (1 - S(.45f, .53f, y));
                float chest = S(.45f, .53f, y) * (1 - S(.58f, .64f, y));
                float head = S(.58f, .64f, y);
                float hat = S(.72f, .84f, y) * S(-.02f, .10f, p.x);
                Add(0, torso * hips); Add(1, torso * spine); Add(2, torso * chest);
                Add(3, torso * head * (1 - hat)); Add(4, torso * head * hat);
            }
            var top = w.OrderByDescending(kv => kv.Value).Take(4).ToArray();
            float sum = top.Sum(kv => kv.Value);
            var bw = new BoneWeight();
            for (int i = 0; i < top.Length; i++)
            {
                float v = top[i].Value / sum;
                switch (i)
                {
                    case 0: bw.boneIndex0 = top[i].Key; bw.weight0 = v; break;
                    case 1: bw.boneIndex1 = top[i].Key; bw.weight1 = v; break;
                    case 2: bw.boneIndex2 = top[i].Key; bw.weight2 = v; break;
                    case 3: bw.boneIndex3 = top[i].Key; bw.weight3 = v; break;
                }
            }
            return bw;
        }

        public static void Apply()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/technician.prefab");
            if (!source) throw new System.InvalidOperationException("Prepare the technician model first.");
            var temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
            var root = new GameObject("technician-rigged");
            try
            {
                var filter = temp.GetComponentInChildren<MeshFilter>();
                var turn = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
                var matrix = turn * temp.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;

                var skeleton = new GameObject("Technician skeleton").transform;
                skeleton.SetParent(root.transform, false);
                var rig = skeleton.gameObject.AddComponent<TechnicianRig>();
                var bones = new Transform[Layout.Length];
                var rest = new Vector3[Layout.Length];
                for (int i = 0; i < Layout.Length; i++)
                {
                    var t = new GameObject(Layout[i].name).transform;
                    t.SetParent(Layout[i].parent < 0 ? skeleton : bones[Layout[i].parent], false);
                    t.position = skeleton.TransformPoint(Layout[i].pos);
                    bones[i] = t; rest[i] = t.localPosition;
                }
                rig.bones = bones; rig.restPositions = rest;
                var anchor = new GameObject("Tool hand anchor").transform;
                anchor.SetParent(bones[10], false); anchor.position = skeleton.TransformPoint(new Vector3(.27f, .37f, .05f));
                rig.handAnchor = anchor;
                var leftAnchor = new GameObject("Off hand anchor").transform;
                leftAnchor.SetParent(bones[7], false); leftAnchor.position = skeleton.TransformPoint(new Vector3(-.27f, .37f, .05f));
                rig.leftHandAnchor = leftAnchor;

                var mesh = Object.Instantiate(filter.sharedMesh);
                mesh.name = "Technician skin";
                var points = mesh.vertices; var normals = mesh.normals;
                var normalMatrix = matrix.inverse.transpose;
                var weights = new BoneWeight[points.Length];
                var uvs = mesh.uv;
                var albedoTex = new Texture2D(2, 2);
                albedoTex.LoadImage(File.ReadAllBytes(Folder + "/BaseColor.png"));
                // Cloth class per triangle (corners are unshared), so a face never
                // splits between arm and trousers and stretches into a sliver.
                var tris = mesh.triangles; var trouser = new bool[points.Length];
                for (int t = 0; t < tris.Length; t += 3)
                {
                    var c = Color.clear;
                    for (int k = 0; k < 3; k++) { int v = tris[t + k]; c += v < uvs.Length ? albedoTex.GetPixelBilinear(uvs[v].x, uvs[v].y) : Color.gray; }
                    c /= 3;
                    bool cloth = c.r > c.g * 1.9f && c.b > c.g * .8f && c.r < .8f;
                    for (int k = 0; k < 3; k++) trouser[tris[t + k]] = cloth;
                }
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = matrix.MultiplyPoint3x4(points[i]);
                    if (i < normals.Length) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
                    weights[i] = Weight(points[i], trouser[i]);
                }
                Object.DestroyImmediate(albedoTex);
                mesh.vertices = points; if (normals.Length > 0) mesh.normals = normals;
                mesh.boneWeights = weights;
                mesh.bindposes = bones.Select(b => b.worldToLocalMatrix * skeleton.localToWorldMatrix).ToArray();
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                string meshPath = Folder + "/Technician skin.asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (saved) { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); mesh = saved; }
                else AssetDatabase.CreateAsset(mesh, meshPath);

                var body = new GameObject("Technician body").AddComponent<SkinnedMeshRenderer>();
                body.transform.SetParent(skeleton, false);
                body.sharedMesh = mesh; body.sharedMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials;
                body.bones = bones; body.rootBone = bones[0]; body.quality = SkinQuality.Bone4;
                body.localBounds = new Bounds(new Vector3(0, .2f, 0), new Vector3(1.4f, 1.6f, 1.4f));
                body.updateWhenOffscreen = true;
                rig.body = body;
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/technician-rigged.prefab");
            }
            finally { Object.DestroyImmediate(temp); Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }

        // Checks weights and that every bone moves skin when posed.
        public static string Validate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/technician-rigged.prefab");
            var rig = prefab.GetComponentInChildren<TechnicianRig>();
            var mesh = rig.body.sharedMesh;
            var counts = new int[rig.bones.Length];
            foreach (var w in mesh.boneWeights)
            {
                if (Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) > .001f) return "FAIL: weights not normalised";
                if (w.weight0 > .2f) counts[w.boneIndex0]++;
            }
            var empty = Enumerable.Range(0, counts.Length).Where(i => counts[i] == 0).Select(i => rig.bones[i].name).ToArray();
            return empty.Length > 0 ? "FAIL: bones without skin: " + string.Join(", ", empty)
                : "PASS: " + mesh.vertexCount + " vertices on " + counts.Length + " bones (" + string.Join(", ", Enumerable.Range(0, counts.Length).Select(i => rig.bones[i].name + " " + counts[i])) + ")";
        }

        // Renders the rig in a set of poses for review: Screenshots/Rig-<pose>.png
        public static string PosePreview(params string[] poses)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/technician-rigged.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(6000, 0, 0);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            var rig = go.GetComponentInChildren<TechnicianRig>();
            var light = new GameObject("Preview light").AddComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(40, 150, 0); light.cullingMask = 1 << 31; light.intensity = 1.4f;
            var cam = new GameObject("Preview camera").AddComponent<Camera>();
            cam.cullingMask = 1 << 31; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.62f, .7f, .74f);
            cam.orthographic = true; cam.orthographicSize = .62f; cam.nearClipPlane = .01f; cam.farClipPlane = 50;
            Directory.CreateDirectory("Screenshots");
            var rt = new RenderTexture(600, 600, 24); var tex = new Texture2D(600, 600, TextureFormat.RGB24, false);
            var center = go.transform.position + Vector3.up * .5f;
            foreach (var pose in poses)
            {
                var parts = pose.Split(':'); float yaw = parts.Length > 1 ? float.Parse(parts[1]) : 30;
                for (int i = 0; i < rig.bones.Length; i++) { rig.bones[i].localPosition = rig.restPositions[i]; rig.bones[i].localRotation = Quaternion.identity; }
                TechnicianPoses.Apply(rig, parts[0]);
                // Edit-mode renders do not re-skin: bake the pose into a mesh.
                var baked = new Mesh(); rig.body.BakeMesh(baked, true);
                var shot = new GameObject("Baked pose", typeof(MeshFilter), typeof(MeshRenderer));
                shot.layer = 31; shot.transform.SetPositionAndRotation(rig.body.transform.position, rig.body.transform.rotation);
                shot.GetComponent<MeshFilter>().sharedMesh = baked; shot.GetComponent<MeshRenderer>().sharedMaterials = rig.body.sharedMaterials;
                rig.body.enabled = false;
                cam.transform.position = center + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 5; cam.transform.LookAt(center);
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 600, 600), 0, 0); tex.Apply();
                File.WriteAllBytes($"Screenshots/Rig-{parts[0]}-{yaw:0}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(shot); Object.DestroyImmediate(baked); rig.body.enabled = true;
            }
            RenderTexture.active = null; cam.targetTexture = null;
            Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(light.gameObject); Object.DestroyImmediate(go);
            return "ok";
        }
    }

    // Static key poses (editor review only), mirroring TechnicianRig's layers.
    public static class TechnicianPoses
    {
        public static void Apply(TechnicianRig rig, string pose)
        {
            void R(TechnicianRig.Bone b, float x, float y = 0, float z = 0) => rig.bones[(int)b].localRotation = Quaternion.Euler(x, y, z);
            var hips = rig.bones[0];
            switch (pose)
            {
                case "walk": R(TechnicianRig.Bone.ThighL, -30); R(TechnicianRig.Bone.ThighR, 30); R(TechnicianRig.Bone.ShinR, 42); R(TechnicianRig.Bone.UpperArmL, 26); R(TechnicianRig.Bone.UpperArmR, -26); break;
                case "run": R(TechnicianRig.Bone.ThighL, -48); R(TechnicianRig.Bone.ThighR, 40); R(TechnicianRig.Bone.ShinL, 20); R(TechnicianRig.Bone.ShinR, 90); R(TechnicianRig.Bone.UpperArmL, 45); R(TechnicianRig.Bone.UpperArmR, -45); R(TechnicianRig.Bone.ForeArmL, -70); R(TechnicianRig.Bone.ForeArmR, -70); R(TechnicianRig.Bone.Spine, 12); break;
                case "climb": R(TechnicianRig.Bone.UpperArmL, -175, 0, -12); R(TechnicianRig.Bone.UpperArmR, -125, 0, 12); R(TechnicianRig.Bone.ForeArmL, -25); R(TechnicianRig.Bone.ForeArmR, -60); R(TechnicianRig.Bone.ThighL, -80); R(TechnicianRig.Bone.ShinL, 95); R(TechnicianRig.Bone.ThighR, -45); R(TechnicianRig.Bone.ShinR, 70); R(TechnicianRig.Bone.Spine, 10); break;
                case "jump": R(TechnicianRig.Bone.ThighL, -70); R(TechnicianRig.Bone.ThighR, -40); R(TechnicianRig.Bone.ShinL, 100); R(TechnicianRig.Bone.ShinR, 75); R(TechnicianRig.Bone.UpperArmL, -110, 0, -35); R(TechnicianRig.Bone.UpperArmR, -110, 0, 35); break;
                case "fall": R(TechnicianRig.Bone.UpperArmL, -40, 0, -100); R(TechnicianRig.Bone.UpperArmR, -40, 0, 100); R(TechnicianRig.Bone.ThighL, 10); R(TechnicianRig.Bone.ThighR, -30); R(TechnicianRig.Bone.ShinR, 40); R(TechnicianRig.Bone.Head, -15); break;
                case "brace": hips.localPosition += Vector3.down * .09f; R(TechnicianRig.Bone.ThighL, -62); R(TechnicianRig.Bone.ThighR, -62); R(TechnicianRig.Bone.ShinL, 95); R(TechnicianRig.Bone.ShinR, 95); R(TechnicianRig.Bone.Spine, 22); R(TechnicianRig.Bone.UpperArmL, -55); R(TechnicianRig.Bone.UpperArmR, -55); break;
                case "hurt": hips.localPosition += new Vector3(0, -.2f, -.05f); R(TechnicianRig.Bone.ThighL, -85, 0, -10); R(TechnicianRig.Bone.ThighR, -78, 0, 12); R(TechnicianRig.Bone.ShinL, 25); R(TechnicianRig.Bone.ShinR, 35); R(TechnicianRig.Bone.Spine, -18); R(TechnicianRig.Bone.Head, 25, 0, 12); R(TechnicianRig.Bone.UpperArmL, 20, 0, -40); R(TechnicianRig.Bone.UpperArmR, 20, 0, 40); break;
                case "hold": R(TechnicianRig.Bone.UpperArmR, -28, 0, 6); R(TechnicianRig.Bone.ForeArmR, -55); break;
            }
        }
    }
}

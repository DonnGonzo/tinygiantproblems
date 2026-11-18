using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // The guild house in the meadow, front door facing Mossback. You walk
    // straight in: the painted door is cut out of the exterior mesh, an open
    // door leaf and dark frame dress the doorway, and the interior room model
    // stands inside the house with its open side right behind the doorway.
    // Inside: Tool Locker (left), sell counter (right), Wardrobe (back). The
    // free Winch Post stands outside next to the door.
    //
    // Both models are axis-aligned at import (Tools/import_new_models.py ALIGN):
    // the exterior's front door gable faces its +Z, the interior's open side
    // faces its -X (turned 90 degrees here so it faces the door).
    public static class GuildHouse
    {
        public const string ExteriorKey = "guild-house/guild-house", InteriorKey = "guild-house-interior/guild-house-interior";
        public const float Size = 21;           // exterior's longest side, metres (1.5x the first pass)
        public const float InteriorSize = 13f;    // fits inside the exterior walls (14.5 poked through)
        public const float DoorWidth = 3.2f, DoorHeight = 4.4f;
        public static Transform Exterior { get; private set; }
        public static Transform Interior { get; private set; }
        public static Transform DoorOutside { get; private set; }
        public static bool PlayerInside => volume && volume.Inside;
        static HouseVolume volume;

        public static bool Build(Transform parent, Vector3 ground, Vector3 towardCreature)
        {
            var lib = ItemLibrary.Instance;
            var exPrefab = lib ? lib.Prefab(ExteriorKey) : null;
            var inPrefab = lib ? lib.Prefab(InteriorKey) : null;
            if (!exPrefab || !inPrefab) return false;
            towardCreature.y = 0; towardCreature.Normalize();

            var root = new GameObject("Guild House").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(ground, Quaternion.LookRotation(towardCreature, Vector3.up));

            // Exterior shell, measured in the root's local metres.
            var shell = Object.Instantiate(exPrefab, root).transform;
            shell.name = "Guild House exterior";
            shell.localRotation = Quaternion.identity;
            shell.localScale = Vector3.one * Size;
            shell.localPosition = Vector3.down * .2f;
            Exterior = root;
            var ext = LocalBounds(root, shell.GetComponentsInChildren<Renderer>());
            // Level the meadow under the house so it sits on the ground, then
            // set the house on that new ground with its footing slightly sunk in.
            float baseY = SettleGround(root, ext, 3f, 14f);
            root.position = new Vector3(root.position.x, baseY, root.position.z);
            // The model's lowest course is an inset dark brick band under the stone
            // footing; sink it fully so the stones meet the grass (no visible gap).
            shell.localPosition = Vector3.down * .9f;
            ext = LocalBounds(root, shell.GetComponentsInChildren<Renderer>());
            float front = ext.max.z, back = ext.min.z, left = ext.min.x, right = ext.max.x;
            float doorX = ext.center.x;
            Physics.SyncTransforms();
            // The bounds include the steps and footing; the door sits in the wall
            // itself, so find the front wall plane with a ray at door height.
            var wallRay = root.TransformPoint(new Vector3(doorX, 3f, front + 6));
            foreach (var h in Physics.RaycastAll(wallRay, -root.forward, 20, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider.transform.IsChildOf(shell)) front = Mathf.Min(front, Mathf.Max(root.InverseTransformPoint(h.point).z, ext.max.z - 4));
            {
                float best = float.MinValue;
                foreach (var h in Physics.RaycastAll(wallRay, -root.forward, 20, ~0, QueryTriggerInteraction.Ignore))
                    if (h.collider.transform.IsChildOf(shell)) best = Mathf.Max(best, root.InverseTransformPoint(h.point).z);
                if (best > float.MinValue) front = best;
            }
            // Threshold: top of the stone footing just inside the door.
            float threshold = .6f;
            var probe = root.TransformPoint(new Vector3(doorX, 20, front - 1.4f));
            if (Physics.Raycast(probe, Vector3.down, out var th, 40, ~0, QueryTriggerInteraction.Ignore) && th.collider.transform.IsChildOf(shell))
                threshold = root.InverseTransformPoint(th.point).y;
            threshold = Mathf.Clamp(threshold, .15f, 1.4f);

            // Replace the closed exterior collision with walls that leave a doorway.
            foreach (var c in shell.GetComponentsInChildren<Collider>()) c.enabled = false;
            float wallH = 7, t = .5f;
            float doorL = doorX - DoorWidth / 2, doorR = doorX + DoorWidth / 2;
            Box(root, "Back wall", new Vector3((left + right) / 2, wallH / 2, back + t / 2), new Vector3(right - left, wallH, t));
            Box(root, "Left wall", new Vector3(left + t / 2, wallH / 2, (front + back) / 2), new Vector3(t, wallH, front - back));
            Box(root, "Right wall", new Vector3(right - t / 2, wallH / 2, (front + back) / 2), new Vector3(t, wallH, front - back));
            Box(root, "Front wall L", new Vector3((left + doorL) / 2, wallH / 2, front - t / 2), new Vector3(doorL - left, wallH, t));
            Box(root, "Front wall R", new Vector3((doorR + right) / 2, wallH / 2, front - t / 2), new Vector3(right - doorR, wallH, t));
            Box(root, "Over door", new Vector3(doorX, (threshold + DoorHeight + wallH) / 2, front - t / 2), new Vector3(DoorWidth, wallH - threshold - DoorHeight, t));
            Box(root, "Roof", new Vector3((left + right) / 2, wallH + .25f, (front + back) / 2), new Vector3(right - left, .5f, front - back));
            // Visible plank floor over the whole footprint, just above the shell's own
            // inner base (which otherwise z-fights dark through the room floor).
            float floorTop = threshold + .06f;
            var floorBox = Box(root, "Floor", new Vector3((left + right) / 2, floorTop / 2, (front + back) / 2), new Vector3(right - left - .2f, floorTop, front - back - .2f));

            // Ramp up the footing to the threshold.
            // From the meadow (measured, it slopes) up to the threshold at about 20 degrees.
            float groundY = -.5f;
            var groundProbe = root.TransformPoint(new Vector3(doorX, 10, front + 4));
            foreach (var h in Physics.RaycastAll(groundProbe, Vector3.down, 30, ~0, QueryTriggerInteraction.Ignore))
                if (!h.collider.transform.IsChildOf(root)) groundY = Mathf.Max(groundY == -.5f ? -99 : groundY, root.InverseTransformPoint(h.point).y);
            float rise = threshold - groundY, run = Mathf.Max(2.5f, rise / Mathf.Tan(20 * Mathf.Deg2Rad));
            float rampLength = Mathf.Sqrt(rise * rise + run * run) + .4f;
            var ramp = Box(root, "Door ramp", Vector3.zero, new Vector3(DoorWidth + .6f, .3f, rampLength));
            ramp.transform.localRotation = Quaternion.Euler(Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0, 0);
            ramp.transform.localPosition = new Vector3(doorX, (threshold + groundY) / 2 - .15f, front + run / 2);
            // The house model has its own stone steps here now that the ground is level;
            // the ramp stays as invisible collision only.

            // Cut the painted door out of the exterior mesh and dress the doorway.
            CutDoorway(root, shell, new Vector3(doorX, 0, front), threshold);
            var wood = new Color(.42f, .25f, .12f); var darkWood = new Color(.2f, .12f, .06f);
            Piece(root, "Door post L", new Vector3(doorL - .2f, threshold + DoorHeight / 2, front - .1f), new Vector3(.4f, DoorHeight, .6f), darkWood);
            Piece(root, "Door post R", new Vector3(doorR + .2f, threshold + DoorHeight / 2, front - .1f), new Vector3(.4f, DoorHeight, .6f), darkWood);
            Piece(root, "Door lintel", new Vector3(doorX, threshold + DoorHeight + .2f, front - .1f), new Vector3(DoorWidth + .8f, .4f, .6f), darkWood);
            Piece(root, "Threshold", new Vector3(doorX, threshold - .05f, front - .3f), new Vector3(DoorWidth, .1f, 1.2f), darkWood);
            var leafPivot = new GameObject("Open door").transform;
            leafPivot.SetParent(root, false);
            leafPivot.localPosition = new Vector3(doorL + .05f, threshold, front - .35f);
            leafPivot.localRotation = Quaternion.Euler(0, 100, 0); // swung open, inward against the wall
            var leaf = Piece(leafPivot, "Door leaf", new Vector3(DoorWidth / 2, DoorHeight / 2, 0), new Vector3(DoorWidth - .1f, DoorHeight - .05f, .16f), wood);
            Piece(leaf.transform, "Iron band", new Vector3(0, .3f, .55f), new Vector3(1.02f, .05f, .2f), new Color(.25f, .24f, .22f));
            Piece(leaf.transform, "Iron band", new Vector3(0, -.3f, .55f), new Vector3(1.02f, .05f, .2f), new Color(.25f, .24f, .22f));

            DoorOutside = new GameObject("Guild House door (outside)").transform;
            DoorOutside.SetParent(root, false);
            DoorOutside.localPosition = new Vector3(doorX, 0, front + 3.2f);
            DoorOutside.localRotation = Quaternion.identity;
            var sign = new GameObject("Guild House sign").transform; sign.SetParent(root, false);
            sign.localPosition = new Vector3(doorX, 0, front + .6f);
            WorldStations.MakeSign(sign, "GUILD HOUSE", new Vector3(0, threshold + DoorHeight + 1.2f, 0), UIKit.Gold, .7f);

            // Interior room inside the shell, open side behind the doorway.
            Interior = Object.Instantiate(inPrefab, root).transform;
            Interior.name = "Guild House interior";
            Interior.localRotation = Quaternion.Euler(0, 90, 0);
            Interior.localScale = Vector3.one * InteriorSize;
            Interior.localPosition = Vector3.zero;
            Physics.SyncTransforms();
            var room = LocalBounds(root, Interior.GetComponentsInChildren<Renderer>());
            float roomFloor = .63f / 20 * InteriorSize;
            Interior.localPosition = new Vector3(doorX - room.center.x, threshold + .08f - roomFloor, front - 1.0f - room.max.z);
            Physics.SyncTransforms();
            room = LocalBounds(root, Interior.GetComponentsInChildren<Renderer>());
            float floorY = threshold + .08f;
            // The room model's floor came out a flat dark slate in the colour pass;
            // drop its floor triangles so the plank floor shows through.
            RemoveTriangles(root, Interior, c => c.y < floorY + .6f, n => n.y > .75f);
            // The exterior shell has its own dark inner base too; trim it inside the walls.
            RemoveTriangles(root, shell, c => c.y > .3f && c.y < floorY + .6f && c.x > left + .6f && c.x < right - .6f && c.z > back + .6f && c.z < front - .6f, n => n.y > .75f);
            // Plank floor laid just above both models' own floor surfaces.
            float plankTop = floorY + .06f;
            if (Physics.Raycast(root.TransformPoint(new Vector3(doorX + 1.5f, 6, (room.min.z + front) / 2)), Vector3.down, out var fl, 10, ~0, QueryTriggerInteraction.Ignore))
                plankTop = Mathf.Max(plankTop, root.InverseTransformPoint(fl.point).y + .05f);
            // Sized to the interior room (not the house bounds, which include the
            // outside steps and made the floor poke out through the walls).
            float fl0 = room.min.x + .3f, fl1 = room.max.x - .3f, fb = room.min.z + .3f, ff = front - .5f;
            // ...and never past the house's own walls (measured from its wall band).
            {
                var smf = shell.GetComponentInChildren<MeshFilter>(); var sm = smf ? smf.sharedMesh : null;
                if (sm && sm.isReadable)
                {
                    var M = root.worldToLocalMatrix * smf.transform.localToWorldMatrix;
                    float wx0 = float.MaxValue, wx1 = float.MinValue, wz0 = float.MaxValue;
                    foreach (var v0 in sm.vertices) { var q = M.MultiplyPoint3x4(v0); if (q.y < floorY + 1.5f || q.y > floorY + 3f) continue; wx0 = Mathf.Min(wx0, q.x); wx1 = Mathf.Max(wx1, q.x); wz0 = Mathf.Min(wz0, q.z); }
                    if (wx0 < wx1) { fl0 = Mathf.Max(fl0, wx0 + .6f); fl1 = Mathf.Min(fl1, wx1 - .6f); fb = Mathf.Max(fb, wz0 + .6f); }
                }
            }
            var planks = Piece(root, "Plank floor", new Vector3((fl0 + fl1) / 2, plankTop - .05f, (fb + ff) / 2), new Vector3(fl1 - fl0, .1f, ff - fb), new Color(.5f, .31f, .15f));
            planks.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (float z = fb + .8f; z < ff; z += 1.1f)
                Piece(root, "Plank seam", new Vector3((fl0 + fl1) / 2, plankTop + .002f, z), new Vector3(fl1 - fl0, .01f, .05f), new Color(.33f, .2f, .1f));
            float midZ = (room.min.z + front) / 2;

            Lamp(root, new Vector3(doorX - 3, floorY + 5f, midZ), 1.6f, 13);
            Lamp(root, new Vector3(doorX + 3, floorY + 5f, midZ), 1.6f, 13);
            Lamp(root, new Vector3(doorX, floorY + 4.5f, room.min.z + 2.5f), 1.2f, 11);

            var locker = Station<ToolLocker>(root, "Guild Tool Locker", new Vector3(room.min.x + 1.6f, floorY, midZ + 1.5f), Vector3.right, 2.6f);
            Model("tool-case/tool-case", locker.transform, 1.4f, Vector3.zero);
            Model("guild-workshop-furniture-kit/shelf", locker.transform, 2.6f, new Vector3(-1.8f, 0, -.2f));
            WorldStations.MakeSign(locker.transform, "TOOL LOCKER", new Vector3(0, 2.3f, 0), UIKit.Gold, .4f);

            var sell = Station<SellStation>(root, "Sell Station", new Vector3(room.max.x - 1.6f, floorY, midZ + 1.5f), Vector3.left, 2.8f);
            if (!Model("guild-workshop-furniture-kit/workbench", sell.transform, 2.4f, Vector3.zero))
                GameArt.Shape("Counter", PrimitiveType.Cube, sell.transform, Vector3.up * .5f, new Vector3(2.4f, 1, 1), new Color(.36f, .19f, .11f));
            var bin = Model("chest/open-chest", sell.transform, 1.3f, new Vector3(1.6f, 0, .2f));
            if (bin) sell.SetLid(bin.transform);
            WorldStations.MakeSign(sell.transform, "SELL SALVAGE", new Vector3(0, 2.3f, 0), UIKit.Gold, .4f);

            var wardrobe = Station<Wardrobe>(root, "Guild Wardrobe", new Vector3(doorX, floorY, room.min.z + 1.6f), Vector3.forward, 2.6f);
            Model("guild-workshop-furniture-kit/shelf", wardrobe.transform, 2.6f, Vector3.zero);
            Model("technician-cosmetic-kit/hat", wardrobe.transform, .7f, new Vector3(-.5f, 1.3f, .2f));
            Model("technician-cosmetic-kit/scarf", wardrobe.transform, .7f, new Vector3(.5f, 1.3f, .2f));
            Model("guild-workshop-furniture-kit/stool", wardrobe.transform, .8f, new Vector3(-1.6f, 0, .6f));
            WorldStations.MakeSign(wardrobe.transform, "WARDROBE", new Vector3(0, 2.8f, 0), UIKit.Cream, .4f);

            volume = root.gameObject.AddComponent<HouseVolume>();
            volume.min = new Vector3(left + .3f, -1, back + .3f); volume.max = new Vector3(right - .3f, wallH, front - .2f);
            return true;
        }

        // Flattens the meadow Terrain under the house footprint (plus `margin`)
        // to one level and blends back to the original slope over `falloff`
        // metres. Works on a runtime copy of the TerrainData so the authored
        // asset is never modified. Returns the pad height (world y).
        static TerrainData originalTerrain;
        public static float SettleGround(Transform root, Bounds footprint, float margin, float falloff)
        {
            var terrain = Terrain.activeTerrain;
            if (!terrain) return root.position.y;
            if (!originalTerrain)
            {
                originalTerrain = terrain.terrainData;
                var copy = Object.Instantiate(originalTerrain); copy.name = originalTerrain.name + " (runtime)";
                terrain.terrainData = copy;
                var col = terrain.GetComponent<TerrainCollider>(); if (col) col.terrainData = copy;
            }
            var data = terrain.terrainData; var origin = terrain.transform.position;
            // Pad height: the average ground under the footprint.
            float sum = 0; int n = 0;
            for (float x = footprint.min.x; x <= footprint.max.x; x += 2)
                for (float z = footprint.min.z; z <= footprint.max.z; z += 2)
                { sum += terrain.SampleHeight(root.TransformPoint(new Vector3(x, 0, z))) + origin.y; n++; }
            float pad = n > 0 ? sum / n : root.position.y;
            int res = data.heightmapResolution;
            var size = data.size;
            float reach = Mathf.Max(footprint.extents.x, footprint.extents.z) + margin + falloff + 2;
            var center = root.TransformPoint(footprint.center);
            int x0 = Mathf.Clamp(Mathf.FloorToInt((center.x - reach - origin.x) / size.x * (res - 1)), 0, res - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((center.x + reach - origin.x) / size.x * (res - 1)), 0, res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((center.z - reach - origin.z) / size.z * (res - 1)), 0, res - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((center.z + reach - origin.z) / size.z * (res - 1)), 0, res - 1);
            int w = x1 - x0 + 1, h = z1 - z0 + 1;
            var heights = data.GetHeights(x0, z0, w, h);
            float padNorm = (pad - origin.y) / size.y;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    var world = new Vector3(origin.x + (x0 + i) / (float)(res - 1) * size.x, 0, origin.z + (z0 + j) / (float)(res - 1) * size.z);
                    var local = root.InverseTransformPoint(world);
                    float dx = Mathf.Max(0, Mathf.Abs(local.x - footprint.center.x) - footprint.extents.x - margin);
                    float dz = Mathf.Max(0, Mathf.Abs(local.z - footprint.center.z) - footprint.extents.z - margin);
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    float k = d <= 0 ? 1 : 1 - Mathf.SmoothStep(0, 1, d / falloff);
                    heights[j, i] = Mathf.Lerp(heights[j, i], padNorm, k);
                }
            data.SetHeights(x0, z0, heights);
            Physics.SyncTransforms();
            return pad;
        }

        // Put the authored terrain back (leaving Play mode / reloading).
        public static void RestoreGround()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain && originalTerrain) { terrain.terrainData = originalTerrain; var col = terrain.GetComponent<TerrainCollider>(); if (col) col.terrainData = originalTerrain; }
            originalTerrain = null;
        }

        // Bounds of renderers expressed in `space`'s local frame (axis-aligned there).
        static Bounds LocalBounds(Transform space, Renderer[] renderers)
        {
            var b = new Bounds(); bool any = false;
            foreach (var r in renderers)
            {
                var mf = r.GetComponent<MeshFilter>(); var mesh = mf ? mf.sharedMesh : null;
                if (!mesh) continue;
                var mb = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) * 2 - 1, (i & 2) - 1, (i & 4) / 2 - 1));
                    var p = space.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        // Removes the exterior triangles inside the doorway box (in root space).
        static void CutDoorway(Transform root, Transform shell, Vector3 doorBase, float threshold)
        {
            foreach (var mf in shell.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh; if (!src) continue;
                // Never edit a mesh whose vertices can't be read: that would erase it.
                if (!src.isReadable) { Debug.LogWarning("Guild House: mesh " + src.name + " is not readable; leaving it uncut."); continue; }
                var mesh = Object.Instantiate(src); mesh.name = src.name + " (doorway)";
                var verts = mesh.vertices;
                var toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int sm = 0; sm < mesh.subMeshCount; sm++)
                {
                    var tris = mesh.GetTriangles(sm); var keep = new List<int>(tris.Length);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        var c = (toRoot.MultiplyPoint3x4(verts[tris[i]]) + toRoot.MultiplyPoint3x4(verts[tris[i + 1]]) + toRoot.MultiplyPoint3x4(verts[tris[i + 2]])) / 3;
                        bool inDoor = Mathf.Abs(c.x - doorBase.x) < DoorWidth / 2 && c.y < threshold + DoorHeight && c.y > -1 && c.z > doorBase.z - 2.2f;
                        if (!inDoor) { keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]); }
                    }
                    mesh.SetTriangles(keep, sm);
                }
                mf.sharedMesh = mesh;
            }
        }

        static void RemoveTriangles(Transform root, Transform model, System.Func<Vector3, bool> where, System.Func<Vector3, bool> facing)
        {
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh; if (!src) continue;
                // Never edit a mesh whose vertices can't be read: that would erase it.
                if (!src.isReadable) { Debug.LogWarning("Guild House: mesh " + src.name + " is not readable; leaving it uncut."); continue; }
                var mesh = Object.Instantiate(src); mesh.name = src.name + " (trimmed)";
                var verts = mesh.vertices;
                var toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int sm = 0; sm < mesh.subMeshCount; sm++)
                {
                    var tris = mesh.GetTriangles(sm); var keep = new List<int>(tris.Length);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        Vector3 a = toRoot.MultiplyPoint3x4(verts[tris[i]]), b = toRoot.MultiplyPoint3x4(verts[tris[i + 1]]), c = toRoot.MultiplyPoint3x4(verts[tris[i + 2]]);
                        var n = Vector3.Cross(b - a, c - a).normalized;
                        bool drop = where((a + b + c) / 3) && (facing(n) || facing(-n));
                        if (!drop) { keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]); }
                    }
                    mesh.SetTriangles(keep, sm);
                }
                mf.sharedMesh = mesh;
            }
        }

        static BoxCollider Box(Transform root, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.localPosition = center;
            var b = go.AddComponent<BoxCollider>(); b.size = size; return b;
        }

        static GameObject Piece(Transform parent, string name, Vector3 local, Vector3 size, Color color)
        {
            var go = GameArt.Shape(name, PrimitiveType.Cube, parent, local, size, color);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        static T Station<T>(Transform root, string name, Vector3 local, Vector3 facing, float radius) where T : Station
        {
            var s = new GameObject(name).AddComponent<T>();
            s.transform.SetParent(root, false);
            s.transform.localPosition = local;
            s.transform.localRotation = Quaternion.LookRotation(facing);
            s.radius = radius;
            return s;
        }

        static void Lamp(Transform root, Vector3 local, float intensity, float range)
        {
            var l = new GameObject("Lantern light").AddComponent<Light>();
            l.transform.SetParent(root, false); l.transform.localPosition = local;
            l.type = LightType.Point; l.color = new Color(1, .78f, .5f); l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
        }

        static GameObject Model(string key, Transform parent, float size, Vector3 local)
        {
            var go = GameArt.Model(key, parent, size);
            if (go) go.transform.localPosition = local;
            return go;
        }
    }

    // Tracks whether the technician is inside the house footprint and pulls the
    // orbit camera in while they are (no teleport, no button).
    public sealed class HouseVolume : MonoBehaviour
    {
        void OnDestroy() => GuildHouse.RestoreGround();
        public Vector3 min, max;
        public bool Inside { get; private set; }
        bool was;
        void Update()
        {
            var g = Expedition.Instance; if (!g || !g.player) return;
            var p = transform.InverseTransformPoint(g.player.transform.position);
            Inside = p.x > min.x && p.x < max.x && p.z > min.z && p.z < max.z && p.y > min.y && p.y < max.y;
            if (Inside != was)
            {
                was = Inside;
                g.player.distance = Inside ? 4.2f : 8;
                if (Inside) g.Caption = "The Guild House: Tool Locker on the left, sell counter on the right, wardrobe at the back.";
            }
        }
    }

    // Opens the Guild Member editor in the guild house.
    public sealed class Wardrobe : Station
    {
        public override string Prompt => "<color=#FFBC4A>E</color>  Wardrobe: change your technician's colours";
        public override void Interact() { var ed = GuildMemberEditor.Instance; if (ed) ed.Open(); }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // The Small Works Guild House, built at runtime from modular box pieces
    // (see Docs/GuildHouse-Layout.md for the plan). Local frame: +Z out of the
    // front door towards Mossback, +X to the right as you walk in, y = 0 is the
    // top of the indoor floor. Ground floor: Tool Locker (left), sell counter
    // (right), Wardrobe (back left). Upper levels: gallery (4 m), loft and high
    // beam (8 m), gable balcony for gliding. Routes: stairs, crate climb, shelf
    // tower, claw climbing wall. Three hidden trinkets respawn every mission.
    public static class GuildHall
    {
        public static Transform Root { get; private set; }
        public static Transform DoorOutside { get; private set; }
        public static bool PlayerInside => volume && volume.Inside;
        static HallVolume volume;

        // One shared palette so every piece reads as part of the same building.
        public static class Palette
        {
            public static readonly Color Stone = new Color(.56f, .53f, .49f), StoneDark = new Color(.4f, .38f, .36f);
            public static readonly Color Plaster = new Color(.93f, .87f, .73f), Timber = new Color(.45f, .28f, .15f), TimberDark = new Color(.24f, .14f, .08f);
            public static readonly Color Plank = new Color(.66f, .47f, .28f), PlankSeam = new Color(.47f, .31f, .17f);
            public static readonly Color Roof = new Color(.2f, .44f, .42f), RoofDark = new Color(.14f, .33f, .32f);
            public static readonly Color Trim = new Color(1f, .74f, .3f), Rug = new Color(.63f, .2f, .16f);
            public static readonly Color Climb = new Color(.36f, .5f, .56f), Glass = new Color(.62f, .8f, .86f), Iron = new Color(.24f, .24f, .23f);
        }

        // Half extents of the footprint (outside faces of the walls).
        public const float HalfX = 11, HalfZ = 8, Wall = .5f, Eave = 9, Ridge = 13, Gallery = 4, Loft = 8;
        const float DoorHalf = 1.7f, DoorHeight = 4.2f;
        static Transform structure;

        public static bool Build(Transform parent, Vector3 center, Vector3 towardCreature)
        {
            towardCreature.y = 0; towardCreature.Normalize();
            Root = new GameObject("Guild House").transform;
            Root.SetParent(parent, false);
            Root.rotation = Quaternion.LookRotation(towardCreature, Vector3.up);
            Root.position = center;

            // Pick the flattest nearby spot so as little foundation shows as
            // possible, then sit the floor just above the highest ground under
            // the footprint and run the stone foundation below the lowest.
            var side = Vector3.Cross(Vector3.up, towardCreature);
            float best = float.MaxValue; var site = center;
            for (float s = -12; s <= 12; s += 3)
                for (float t = -9; t <= 3; t += 3)
                {
                    var c = center + side * s + towardCreature * t;
                    Root.position = c; Spread(out float h0, out float l0);
                    float score = (h0 - l0) + .04f * (Mathf.Abs(s) + Mathf.Abs(t));
                    if (score < best) { best = score; site = c; }
                }
            Root.position = site;
            Spread(out float hi, out float lo);
            Root.position = new Vector3(site.x, hi + .3f, site.z);
            float lowLocal = lo - Root.position.y - 1.2f;

            structure = new GameObject("Structure").transform; structure.SetParent(Root, false);
            BuildShell(lowLocal);
            BuildRoof();
            BuildFacade(lowLocal);
            BuildFrontSteps();
            BuildGroundFloor();
            BuildUpperLevels();
            BuildLights();
            var stations = BuildStations();

            DoorOutside = new GameObject("Guild House door (outside)").transform;
            DoorOutside.SetParent(Root, false);
            DoorOutside.localPosition = new Vector3(0, 0, HalfZ + 3.4f);
            DoorOutside.localPosition = new Vector3(0, GroundY(DoorOutside.position) - Root.position.y, HalfZ + 3.4f);

            volume = Root.gameObject.AddComponent<HallVolume>();
            volume.min = new Vector3(-HalfX + Wall, -1, -HalfZ + Wall); volume.max = new Vector3(HalfX - Wall, Ridge, HalfZ - Wall);
            var finds = Root.gameObject.AddComponent<HallFinds>();
            finds.spots = new[]
            {
                (ItemId.ShinyPebble, new Vector3(6.7f, .25f, 6.7f)),     // under the stairs
                (ItemId.ShinyPebble, new Vector3(-5f, .25f, -6.85f)),    // behind the wardrobe
                (ItemId.GuildBadge, new Vector3(12.7f, 8.25f, 0)),        // end of the gable balcony
            };
            ClearSite();
            StaticBatchingUtility.Combine(structure.gameObject);
            return stations;
        }

        static void Spread(out float hi, out float lo)
        {
            hi = float.MinValue; lo = float.MaxValue;
            for (float x = -HalfX - .4f; x <= HalfX + .41f; x += 1)
                for (float z = -HalfZ - .4f; z <= HalfZ + 4.5f; z += 1)   // includes the front steps
                { float g = GroundY(Root.TransformPoint(new Vector3(x, 0, z))); if (z <= HalfZ + .41f) hi = Mathf.Max(hi, g); lo = Mathf.Min(lo, g); }
        }

        // ---------------------------------------------------------------- shell
        static void BuildShell(float lowLocal)
        {
            // Foundation and floor.
            Block(structure, "Foundation", new Vector3(-HalfX - .4f, lowLocal, -HalfZ - .4f), new Vector3(HalfX + .4f, 0, HalfZ + .4f), Palette.Stone);
            // Masonry courses so the exposed foundation reads as laid stone.
            for (float y = -.55f; y > lowLocal + .2f; y -= .55f)
                Block(structure, "Stone course", new Vector3(-HalfX - .415f, y - .025f, -HalfZ - .415f), new Vector3(HalfX + .415f, y + .025f, HalfZ + .415f), Palette.StoneDark, false);
            // Capstone lip round the outside only (it must not cover the floor).
            Block(structure, "Foundation lip", new Vector3(-HalfX - .48f, -.12f, HalfZ), new Vector3(HalfX + .48f, .04f, HalfZ + .48f), Palette.StoneDark, false);
            Block(structure, "Foundation lip", new Vector3(-HalfX - .48f, -.12f, -HalfZ - .48f), new Vector3(HalfX + .48f, .04f, -HalfZ), Palette.StoneDark, false);
            Block(structure, "Foundation lip", new Vector3(-HalfX - .48f, -.12f, -HalfZ), new Vector3(-HalfX, .04f, HalfZ), Palette.StoneDark, false);
            Block(structure, "Foundation lip", new Vector3(HalfX, -.12f, -HalfZ), new Vector3(HalfX + .48f, .04f, HalfZ), Palette.StoneDark, false);
            Block(structure, "Floor planks", new Vector3(-HalfX + Wall, -.05f, -HalfZ + Wall), new Vector3(HalfX - Wall, .02f, HalfZ - Wall), Palette.Plank, false);
            for (float x = -HalfX + Wall + 1.2f; x < HalfX - Wall; x += 1.2f)
                Block(structure, "Plank seam", new Vector3(x - .03f, .02f, -HalfZ + Wall), new Vector3(x + .03f, .025f, HalfZ - Wall), Palette.PlankSeam, false);

            float i = HalfX - Wall, k = HalfZ - Wall;
            // Front wall with the doorway.
            Block(structure, "Front wall L", new Vector3(-HalfX, 0, k), new Vector3(-DoorHalf, Eave, HalfZ), Palette.Plaster);
            Block(structure, "Front wall R", new Vector3(DoorHalf, 0, k), new Vector3(HalfX, Eave, HalfZ), Palette.Plaster);
            Block(structure, "Over door", new Vector3(-DoorHalf, DoorHeight, k), new Vector3(DoorHalf, Eave, HalfZ), Palette.Plaster);
            Block(structure, "Back wall", new Vector3(-HalfX, 0, -HalfZ), new Vector3(HalfX, Eave, -k), Palette.Plaster);
            Block(structure, "Left wall", new Vector3(-HalfX, 0, -k), new Vector3(-i, Eave, k), Palette.Plaster);
            // Right wall: the gable door at loft height opens onto the balcony.
            Block(structure, "Right wall back", new Vector3(i, 0, -k), new Vector3(HalfX, Eave, -.9f), Palette.Plaster);
            Block(structure, "Right wall front", new Vector3(i, 0, .9f), new Vector3(HalfX, Eave, k), Palette.Plaster);
            Block(structure, "Right wall under door", new Vector3(i, 0, -.9f), new Vector3(HalfX, Loft, .9f), Palette.Plaster);

            // Gables (z, y outline), the right one with the door hole.
            float rise = Ridge - Eave;
            Gable("Left gable", -HalfX, -i, new[] { new Vector2(-HalfZ, Eave), new Vector2(HalfZ, Eave), new Vector2(0, Ridge) });
            float side = Eave + (HalfZ - .9f) * rise / HalfZ;
            Gable("Right gable", i, HalfX, new[]
            {
                new Vector2(-HalfZ, Eave), new Vector2(-.9f, Eave), new Vector2(-.9f, side),
                new Vector2(.9f, Eave), new Vector2(HalfZ, Eave), new Vector2(.9f, side),
                new Vector2(-.9f, 10.4f), new Vector2(.9f, 10.4f), new Vector2(.9f, side),
                new Vector2(-.9f, 10.4f), new Vector2(.9f, side), new Vector2(-.9f, side),
                new Vector2(-.9f, side), new Vector2(.9f, side), new Vector2(0, Ridge),
            });
            // Invisible stepped collision for both gables (keeps you inside from the loft).
            for (float y = Eave; y < Ridge; y += .5f)
            {
                float half = HalfZ * (1 - (y + .5f - Eave) / rise) + .2f;
                Block(structure, "Gable collision", new Vector3(-HalfX, y, -half), new Vector3(-i, y + .5f, half), Color.clear, true, false);
                if (y + .5f <= 10.4f) { Block(structure, "Gable collision", new Vector3(i, y, -half), new Vector3(HalfX, y + .5f, -.9f), Color.clear, true, false); Block(structure, "Gable collision", new Vector3(i, y, .9f), new Vector3(HalfX, y + .5f, half), Color.clear, true, false); }
                else Block(structure, "Gable collision", new Vector3(i, y, -half), new Vector3(HalfX, y + .5f, half), Color.clear, true, false);
            }
        }

        static void BuildRoof()
        {
            float angle = Mathf.Atan2(Ridge - Eave, HalfZ) * Mathf.Rad2Deg, overhang = .9f;
            float run = HalfZ + overhang, drop = run * (Ridge - Eave) / HalfZ;
            float length = Mathf.Sqrt(run * run + drop * drop), thick = .35f, width = 2 * HalfX + 1.6f;
            for (int s = -1; s <= 1; s += 2)
            {
                var rot = Quaternion.Euler(s * angle, 0, 0);
                var normal = rot * Vector3.up;
                var mid = new Vector3(0, Ridge - drop / 2, s * run / 2) + normal * thick / 2;
                var slab = Piece(structure, s > 0 ? "Roof front" : "Roof back", mid, new Vector3(width, thick, length), Palette.Roof, true, rot);
                // Shingle courses on top.
                for (float d = .5f; d < length - .2f; d += .75f)
                {
                    var along = rot * Vector3.forward * s;
                    var p = new Vector3(0, Ridge, 0) + along * d + normal * (thick + .04f);
                    Piece(structure, "Shingle course", p, new Vector3(width + .02f, .09f, .32f), Palette.RoofDark, false, rot);
                }
                // Fascia board along the eave.
                Piece(structure, "Fascia", new Vector3(0, Ridge - drop - .12f, s * (run + .02f)), new Vector3(width, .4f, .14f), Palette.TimberDark, false);
            }
            Piece(structure, "Ridge cap", new Vector3(0, Ridge + .42f, 0), new Vector3(width + .2f, .35f, .7f), Palette.TimberDark, false);
            // Chimney over the back-wall fireplace.
            Block(structure, "Chimney", new Vector3(-.9f, Eave - 1, -6.4f), new Vector3(.9f, Ridge + 1.6f, -5.2f), Palette.StoneDark);
            Block(structure, "Chimney cap", new Vector3(-1.1f, Ridge + 1.6f, -6.6f), new Vector3(1.1f, Ridge + 1.85f, -5f), Palette.Stone, false);
        }

        // Stone skirt, half-timber frame, windows, porch and sign.
        static void BuildFacade(float lowLocal)
        {
            const float proud = .09f;
            // Stone skirt to 1.2 m on every face.
            Block(structure, "Skirt front", new Vector3(-HalfX - proud, 0, HalfZ), new Vector3(-DoorHalf - .35f, 1.2f, HalfZ + proud), Palette.Stone, false);
            Block(structure, "Skirt front", new Vector3(DoorHalf + .35f, 0, HalfZ), new Vector3(HalfX + proud, 1.2f, HalfZ + proud), Palette.Stone, false);
            Block(structure, "Skirt back", new Vector3(-HalfX - proud, 0, -HalfZ - proud), new Vector3(HalfX + proud, 1.2f, -HalfZ), Palette.Stone, false);
            Block(structure, "Skirt left", new Vector3(-HalfX - proud, 0, -HalfZ), new Vector3(-HalfX, 1.2f, HalfZ), Palette.Stone, false);
            Block(structure, "Skirt right", new Vector3(HalfX, 0, -HalfZ), new Vector3(HalfX + proud, 1.2f, HalfZ), Palette.Stone, false);

            // Frame: corner posts, studs, rails at the floor lines.
            foreach (var cx in new[] { -1, 1 }) foreach (var cz in new[] { -1, 1 })
                Block(structure, "Corner post", new Vector3(cx * HalfX - .28f, 1.2f, cz * HalfZ - .28f), new Vector3(cx * HalfX + .28f, Eave, cz * HalfZ + .28f), Palette.TimberDark, false);
            foreach (var x in new[] { -7.4f, -3.7f, 3.7f, 7.4f, -DoorHalf - .2f, DoorHalf + .2f })
                Stud(new Vector3(x, 0, HalfZ), true, x == -DoorHalf - .2f || x == DoorHalf + .2f ? 0 : 1.2f);
            foreach (var x in new[] { -7.4f, -3.7f, 0, 3.7f, 7.4f }) Stud(new Vector3(x, 0, -HalfZ), true, 1.2f);
            foreach (var z in new[] { -4f, 4f }) { Stud(new Vector3(-HalfX, 0, z), false, 1.2f); Stud(new Vector3(HalfX, 0, z), false, 1.2f); }
            foreach (var y in new[] { 1.2f, Gallery + .2f, Eave - .15f })
            {
                Block(structure, "Rail", new Vector3(-HalfX, y - .14f, HalfZ), new Vector3(-DoorHalf - .2f, y + .14f, HalfZ + proud + .02f), Palette.TimberDark, false);
                Block(structure, "Rail", new Vector3(DoorHalf + .2f, y - .14f, HalfZ), new Vector3(HalfX, y + .14f, HalfZ + proud + .02f), Palette.TimberDark, false);
                Block(structure, "Rail", new Vector3(-HalfX, y - .14f, -HalfZ - proud - .02f), new Vector3(HalfX, y + .14f, -HalfZ), Palette.TimberDark, false);
                Block(structure, "Rail", new Vector3(-HalfX - proud - .02f, y - .14f, -HalfZ), new Vector3(-HalfX, y + .14f, HalfZ), Palette.TimberDark, false);
                Block(structure, "Rail", new Vector3(HalfX, y - .14f, -HalfZ), new Vector3(HalfX + proud + .02f, y + .14f, HalfZ), Palette.TimberDark, false);
            }
            Block(structure, "Gable king post", new Vector3(-HalfX - proud - .02f, Eave, -.16f), new Vector3(-HalfX, Ridge - .2f, .16f), Palette.TimberDark, false);
            // Gable door frame and lintel on the right.
            Block(structure, "Gable door frame", new Vector3(HalfX, Loft, -1.1f), new Vector3(HalfX + .12f, 10.6f, -.9f), Palette.TimberDark, false);
            Block(structure, "Gable door frame", new Vector3(HalfX, Loft, .9f), new Vector3(HalfX + .12f, 10.6f, 1.1f), Palette.TimberDark, false);
            Block(structure, "Gable door lintel", new Vector3(HalfX, 10.4f, -1.1f), new Vector3(HalfX + .12f, 10.65f, 1.1f), Palette.TimberDark, false);

            // Windows (glass shows inside and out).
            foreach (var x in new[] { -9f, -5.5f, 5.5f, 9f }) { Window(new Vector3(x, 2.5f, HalfZ - Wall / 2), true, 1.8f, 1.8f); Window(new Vector3(x, 6.1f, HalfZ - Wall / 2), true, 1.6f, 1.6f); }
            foreach (var x in new[] { -9f, -5.5f, 5.5f, 9f }) Window(new Vector3(x, 6.1f, -HalfZ + Wall / 2), true, 1.6f, 1.6f);
            foreach (var x in new[] { -9f, 5.5f, 9f }) Window(new Vector3(x, 2.2f, -HalfZ + Wall / 2), true, 1.4f, 1.4f);
            foreach (var s in new[] { -1, 1 })
            {
                foreach (var z in new[] { -6f, 6f }) Window(new Vector3(s * (HalfX - Wall / 2), 2.4f, z), false, 1.6f, 1.6f);
                foreach (var z in new[] { -2.4f, 2.4f }) Window(new Vector3(s * (HalfX - Wall / 2), 6.1f, z), false, 1.4f, 1.4f);
            }
            Window(new Vector3(-HalfX + Wall / 2, 11f, 0), false, 1.2f, 1.2f);

            // Porch canopy and the sign over the door.
            foreach (var x in new[] { -2.5f, 2.5f })
                Block(structure, "Porch post", new Vector3(x - .16f, lowLocal, HalfZ + 1.6f), new Vector3(x + .16f, 4.9f, HalfZ + 1.92f), Palette.TimberDark);
            Piece(structure, "Porch roof", new Vector3(0, 5.15f, HalfZ + 1.05f), new Vector3(6f, .25f, 2.4f), Palette.Roof, true, Quaternion.Euler(14, 0, 0));
            Piece(structure, "Porch beam", new Vector3(0, 4.9f, HalfZ + 1.76f), new Vector3(5.6f, .28f, .3f), Palette.TimberDark, false);
            // Painted board over the porch (fixed to the wall, readable from the meadow).
            Block(structure, "Sign board", new Vector3(-3.4f, 5.9f, HalfZ + .1f), new Vector3(3.4f, 7.3f, HalfZ + .28f), Palette.TimberDark, false);
            Block(structure, "Sign face", new Vector3(-3.2f, 6.05f, HalfZ + .28f), new Vector3(3.2f, 7.15f, HalfZ + .3f), Palette.Roof, false);
            var label = new GameObject("Guild House sign", typeof(RectTransform)).AddComponent<TMPro.TextMeshPro>();
            label.transform.SetParent(Root, false); label.transform.localPosition = new Vector3(0, 6.6f, HalfZ + .33f);
            label.transform.localRotation = Quaternion.Euler(0, 180, 0);
            label.font = UIKit.Font; label.text = "GUILD HOUSE"; label.fontSize = 9; label.color = UIKit.Gold; label.alignment = TMPro.TextAlignmentOptions.Center;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap; ((RectTransform)label.transform).sizeDelta = new Vector2(6.2f, 1.2f);
            // Door leaves swung open into the hall.
            foreach (var s in new[] { -1, 1 })
                Block(structure, "Door leaf", new Vector3(s * (DoorHalf + .05f) - .07f, 0, HalfZ - Wall - 1.6f), new Vector3(s * (DoorHalf + .05f) + .07f, DoorHeight - .1f, HalfZ - Wall), Palette.Timber);
            Block(structure, "Door lintel", new Vector3(-DoorHalf - .3f, DoorHeight, HalfZ - .05f), new Vector3(DoorHalf + .3f, DoorHeight + .35f, HalfZ + proud + .04f), Palette.TimberDark, false);
            Block(structure, "Threshold", new Vector3(-DoorHalf, -.02f, HalfZ - Wall - .1f), new Vector3(DoorHalf, .04f, HalfZ + .4f), Palette.TimberDark, false);
        }

        static void Stud(Vector3 at, bool alongX, float from)
        {
            const float w = .14f, proud = .1f;
            if (alongX) { float z0 = at.z > 0 ? at.z : at.z - proud, z1 = at.z > 0 ? at.z + proud : at.z; Block(structure, "Stud", new Vector3(at.x - w, from, z0), new Vector3(at.x + w, Eave, z1), Palette.TimberDark, false); }
            else { float x0 = at.x > 0 ? at.x : at.x - proud, x1 = at.x > 0 ? at.x + proud : at.x; Block(structure, "Stud", new Vector3(x0, from, at.z - w), new Vector3(x1, Eave, at.z + w), Palette.TimberDark, false); }
        }

        static void Window(Vector3 c, bool alongX, float w, float h)
        {
            Vector3 frame = alongX ? new Vector3(w + .3f, h + .3f, .7f) : new Vector3(.7f, h + .3f, w + .3f);
            Vector3 glass = alongX ? new Vector3(w, h, .76f) : new Vector3(.76f, h, w);
            Piece(structure, "Window frame", c, frame, Palette.TimberDark, false);
            Piece(structure, "Window glass", c, glass, Palette.Glass, false, null, .8f, .35f);
            Piece(structure, "Mullion", c, alongX ? new Vector3(.08f, h, .8f) : new Vector3(.8f, h, .08f), Palette.TimberDark, false);
            Piece(structure, "Transom", c, alongX ? new Vector3(w, .08f, .8f) : new Vector3(.8f, .08f, w), Palette.TimberDark, false);
            Piece(structure, "Sill", c + new Vector3(0, -h / 2 - .2f, 0), alongX ? new Vector3(w + .5f, .12f, .9f) : new Vector3(.9f, .12f, w + .5f), Palette.Timber, false);
        }

        // Steps from the threshold down to the meadow, following its slope.
        static void BuildFrontSteps()
        {
            const float rise = .25f, tread = .42f;
            float z = HalfZ + .4f;
            for (int n = 1; n < 40; n++)
            {
                float top = -n * rise;
                float ground = GroundY(Root.TransformPoint(new Vector3(0, 0, z + tread / 2))) - Root.position.y;
                if (top < ground + .05f) break;
                Block(structure, "Front step", new Vector3(-2.2f, ground - .6f, z), new Vector3(2.2f, top, z + tread), n % 2 == 0 ? Palette.Stone : Palette.StoneDark);
                z += tread;
            }
        }

        // --------------------------------------------------------- ground floor
        static void BuildGroundFloor()
        {
            float i = HalfX - Wall, k = HalfZ - Wall;
            // Rug in the hall.
            var rug = GameArt.Shape("Rug", PrimitiveType.Cylinder, structure, new Vector3(0, .03f, 3.4f), new Vector3(5.6f, .012f, 5.6f), Palette.Rug);
            GameArt.Shape("Rug border", PrimitiveType.Cylinder, structure, new Vector3(0, .025f, 3.4f), new Vector3(6.1f, .01f, 6.1f), Palette.Trim);

            // Fireplace on the back wall (under the gallery).
            Block(structure, "Hearth", new Vector3(-1.8f, 0, -k), new Vector3(1.8f, 2.7f, -k + .8f), Palette.StoneDark);
            Block(structure, "Hearth mantel", new Vector3(-2f, 2.7f, -k), new Vector3(2f, 2.9f, -k + 1f), Palette.Timber, false);
            Block(structure, "Firebox", new Vector3(-1.1f, .1f, -k + .78f), new Vector3(1.1f, 1.7f, -k + .81f), new Color(.08f, .05f, .04f), false);
            Block(structure, "Fire glow", new Vector3(-.7f, .15f, -k + .81f), new Vector3(.7f, .75f, -k + .84f), new Color(1f, .42f, .1f), false, true, .2f, 1.6f);
            Block(structure, "Log", new Vector3(-.6f, .12f, -k + .84f), new Vector3(.6f, .32f, -k + 1.1f), Palette.TimberDark, false);
            Block(structure, "Hearth stone", new Vector3(-2f, 0, -k + .8f), new Vector3(2f, .08f, -k + 1.6f), Palette.Stone, false);

            // Notice board inside the door (ties into the mission board).
            Block(structure, "Notice board", new Vector3(-4.6f, 1.3f, k - .12f), new Vector3(-2.6f, 2.8f, k), new Color(.66f, .48f, .3f), false);
            for (int n = 0; n < 6; n++)
                Block(structure, "Notice", new Vector3(-4.4f + (n % 3) * .62f, 1.5f + (n / 3) * .65f, k - .16f), new Vector3(-4.0f + (n % 3) * .62f, 2.0f + (n / 3) * .65f, k - .12f), n % 2 == 0 ? Palette.Plaster : new Color(.95f, .92f, .82f), false);

            // Under-loft map room: table and stools.
            Block(structure, "Map table", new Vector3(-1.6f, .85f, -2.6f), new Vector3(1.6f, 1f, -.6f), Palette.Timber);
            foreach (var x in new[] { -1.4f, 1.4f }) foreach (var z in new[] { -2.4f, -.8f }) Block(structure, "Table leg", new Vector3(x - .08f, 0, z - .08f), new Vector3(x + .08f, .85f, z + .08f), Palette.TimberDark, false);
            Block(structure, "Map", new Vector3(-1.2f, 1f, -2.3f), new Vector3(1.1f, 1.02f, -.9f), new Color(.93f, .86f, .64f), false);

            // Crate climb (front left): barrel 1.0 > crate 2.0 > bookcase 3.0 > gallery 4.0.
            Cylinder("Barrel", new Vector3(-4f, 0, 4.9f), .6f, 1f, Palette.Timber);
            Landing(new Vector3(-4.45f, 1f, 4.45f), new Vector3(-3.55f, 1f, 5.35f));
            Block(structure, "Crate", new Vector3(-6f, 0, 5.2f), new Vector3(-4.8f, 2f, 6.4f), new Color(.6f, .42f, .24f));
            CrateEdges(new Vector3(-6f, 0, 5.2f), new Vector3(-4.8f, 2f, 6.4f));
            Landing(new Vector3(-6f, 2f, 5.2f), new Vector3(-4.8f, 2f, 6.4f));
            Block(structure, "Bookcase", new Vector3(-7.45f, 0, 4.4f), new Vector3(-6.2f, 3f, 5.8f), Palette.Timber);
            for (int s = 0; s < 4; s++)
                for (int b = 0; b < 5; b++)
                    Block(structure, "Book", new Vector3(-6.22f, .2f + s * .7f, 4.5f + b * .26f), new Vector3(-6.12f, .62f + s * .7f - (b % 2) * .08f, 4.7f + b * .26f), BookColour(s * 5 + b), false);
            Landing(new Vector3(-7.45f, 3f, 4.4f), new Vector3(-6.2f, 3f, 5.8f));

            // Stairs along the inside of the front wall up to the right gallery.
            const int steps = 16; float x0 = 2.6f, x1 = 7.5f, run = (x1 - x0) / steps, riser = Gallery / steps;
            for (int s = 0; s < steps; s++)
            {
                float top = (s + 1) * riser, a = x0 + s * run, b = a + run;
                float bottom = s < 9 ? 0 : top - .3f;   // open under the top half: the hidden cubby
                Block(structure, "Stair", new Vector3(a, bottom, 5.5f), new Vector3(b + .02f, top, k), Palette.Plank);
                Block(structure, "Stair nosing", new Vector3(b - .05f, top - .05f, 5.5f), new Vector3(b + .03f, top + .01f, k), Palette.TimberDark, false);
            }
            Block(structure, "Stair stringer", new Vector3(x0, 0, 5.45f), new Vector3(x0 + 9 * run, 9 * riser, 5.5f), Palette.TimberDark, false);
            // Banister on the open side.
            float slope = Mathf.Atan2(Gallery, x1 - x0) * Mathf.Rad2Deg, len = Mathf.Sqrt(Gallery * Gallery + (x1 - x0) * (x1 - x0));
            Piece(structure, "Banister", new Vector3((x0 + x1) / 2, Gallery / 2 + .95f, 5.55f), new Vector3(len, .12f, .12f), Palette.TimberDark, false, Quaternion.Euler(0, 0, slope));
            Piece(structure, "Banister guard", new Vector3((x0 + x1) / 2, Gallery / 2 + .5f, 5.55f), new Vector3(len, .9f, .06f), Color.clear, true, Quaternion.Euler(0, 0, slope), .25f, 0, false);
            for (float x = x0 + .3f; x < x1; x += .6f)
            {
                float y = (x - x0) / (x1 - x0) * Gallery;
                Block(structure, "Baluster", new Vector3(x - .03f, y, 5.52f), new Vector3(x + .03f, y + .95f, 5.58f), Palette.TimberDark, false);
            }
            // Crates half hiding the cubby under the stairs (0.9 m gap to squeeze through).
            Block(structure, "Cubby crate", new Vector3(5.1f, 0, 5f), new Vector3(6.1f, 1f, 6f), new Color(.6f, .42f, .24f));
            CrateEdges(new Vector3(5.1f, 0, 5f), new Vector3(6.1f, 1f, 6f));
            Block(structure, "Cubby crate", new Vector3(5.3f, 1f, 5.2f), new Vector3(6f, 1.7f, 5.9f), new Color(.55f, .38f, .22f));

            // Wardrobe nook (back left) with the secret alcove behind it.
            Block(structure, "Wardrobe", new Vector3(-6.1f, 0, -6.1f), new Vector3(-3.9f, 3f, -5.3f), Palette.Timber);
            Block(structure, "Wardrobe mirror", new Vector3(-5.6f, .5f, -5.3f), new Vector3(-4.4f, 2.6f, -5.24f), new Color(.75f, .86f, .9f), false, true, .9f, .15f);
            Block(structure, "Wardrobe crown", new Vector3(-6.25f, 3f, -6.2f), new Vector3(-3.75f, 3.2f, -5.2f), Palette.TimberDark, false);
            Block(structure, "Dresser", new Vector3(-3.9f, 0, -k), new Vector3(-2.3f, 1.2f, -6f), Palette.Timber);
            Block(structure, "Crate stack", new Vector3(-8.5f, 0, -k), new Vector3(-7f, 1.6f, -5.8f), new Color(.6f, .42f, .24f));
            Block(structure, "Crate stack", new Vector3(-8.3f, 1.6f, -k), new Vector3(-7.2f, 2.6f, -6.2f), new Color(.55f, .38f, .22f));
            // Hats and scarves hanging in the nook.
            for (int n = 0; n < 4; n++)
                GameArt.Shape("Hook coat", PrimitiveType.Capsule, structure, new Vector3(-9.6f + n * .45f, 2.1f, -k + .2f), new Vector3(.35f, .5f, .2f), TechnicianLook.Defaults[n % TechnicianLook.Defaults.Length]);
        }

        // ------------------------------------------------------- upper levels
        static void BuildUpperLevels()
        {
            float i = HalfX - Wall, k = HalfZ - Wall, t = .35f;
            // Gallery round three walls.
            Block(structure, "Back gallery", new Vector3(-i, Gallery - t, -k), new Vector3(i, Gallery, -4.5f), Palette.Plank);
            Block(structure, "Left gallery", new Vector3(-i, Gallery - t, -4.5f), new Vector3(-7.5f, Gallery, k), Palette.Plank);
            Block(structure, "Right gallery", new Vector3(7.5f, Gallery - t, -4.5f), new Vector3(i, Gallery, k), Palette.Plank);
            foreach (var p in new[] { new Vector2(-7.5f, -4.5f), new Vector2(7.5f, -4.5f), new Vector2(-7.5f, 1.5f), new Vector2(7.5f, 1.5f), new Vector2(-7.5f, k - .2f), new Vector2(-2f, -4.5f), new Vector2(2f, -4.5f) })
                Block(structure, "Gallery post", new Vector3(p.x - .18f, 0, p.y - .18f), new Vector3(p.x + .18f, Gallery - t, p.y + .18f), Palette.TimberDark);
            Block(structure, "Gallery beam", new Vector3(-i, Gallery - t - .3f, -4.65f), new Vector3(i, Gallery - t, -4.35f), Palette.TimberDark, false);
            Block(structure, "Gallery beam", new Vector3(-7.65f, Gallery - t - .3f, -4.5f), new Vector3(-7.35f, Gallery - t, k), Palette.TimberDark, false);
            Block(structure, "Gallery beam", new Vector3(7.35f, Gallery - t - .3f, -4.5f), new Vector3(7.65f, Gallery - t, k), Palette.TimberDark, false);
            // Railings; gaps where a route arrives.
            Railing(new Vector3(-7.5f, Gallery, -4.5f), new Vector3(5.6f, Gallery, -4.5f));    // back (gap at the shelf tower)
            Railing(new Vector3(-7.5f, Gallery, -4.5f), new Vector3(-7.5f, Gallery, 4.4f));    // left (gap at the crate climb)
            Railing(new Vector3(-7.5f, Gallery, 6f), new Vector3(-7.5f, Gallery, k));
            Railing(new Vector3(7.5f, Gallery, -3f), new Vector3(7.5f, Gallery, 5.5f));       // right (gap at the stairs and tower)
            Landing(new Vector3(-8.1f, Gallery, 4.4f), new Vector3(-7.5f, Gallery, 6f));

            // Loft over the middle on four posts, with the claw wall as its face.
            Block(structure, "Loft", new Vector3(-4f, Loft - t, -3.5f), new Vector3(4f, Loft, 1.5f), Palette.Plank);
            foreach (var x in new[] { -3.8f, 3.8f }) foreach (var z in new[] { -3.3f, 1.3f })
                Block(structure, "Loft post", new Vector3(x - .2f, 0, z - .2f), new Vector3(x + .2f, Loft - t, z + .2f), Palette.TimberDark);
            Railing(new Vector3(-4f, Loft, -3.5f), new Vector3(4f, Loft, -3.5f));
            Railing(new Vector3(-4f, Loft, -3.5f), new Vector3(-4f, Loft, 1.5f));
            Railing(new Vector3(4f, Loft, -3.5f), new Vector3(4f, Loft, -.4f));
            Railing(new Vector3(4f, Loft, .4f), new Vector3(4f, Loft, 1.5f));
            Landing(new Vector3(-3.2f, Loft, 1.2f), new Vector3(3.2f, Loft, 1.5f));
            // Telescope and chest up in the loft.
            Cylinder("Telescope stand", new Vector3(-2.6f, Loft, -2.6f), .25f, .9f, Palette.TimberDark, false);
            Piece(structure, "Telescope", new Vector3(-2.6f, Loft + 1.2f, -2.4f), new Vector3(.22f, 1.4f, .22f), new Color(.72f, .56f, .25f), false, Quaternion.Euler(60, 30, 0), .7f);
            Block(structure, "Loft chest", new Vector3(1.6f, Loft, -3.3f), new Vector3(3f, Loft + .8f, -2.4f), Palette.Timber);

            // Claw climbing wall: 6 m wide, 8 m tall, facing the door.
            var wall = Block(structure, "Climbing wall", new Vector3(-3.2f, 0, 1.5f), new Vector3(3.2f, Loft, 1.8f), Palette.Climb);
            wall.AddComponent<ClimbWall>();
            var rng = new System.Random(7);
            Color[] holds = { Palette.Trim, Palette.Rug, new Color(.36f, .74f, .45f), new Color(.38f, .56f, .9f), new Color(.86f, .44f, .76f) };
            for (int n = 0; n < 46; n++)
            {
                float x = -2.9f + (float)rng.NextDouble() * 5.8f, y = .5f + n * (Loft - 1) / 46f + (float)rng.NextDouble() * .3f;
                var h = GameArt.Shape("Hold", PrimitiveType.Sphere, structure, new Vector3(x, y, 1.84f), new Vector3(.24f, .18f, .14f), holds[n % holds.Length]);
            }
            WorldSign(new Vector3(0, 3.2f, 1.9f), "CLAW WALL", .38f);

            // Shelf tower (back right): s1..s4 rise 0.9 m each, none above another.
            Shelf(new Vector3(5.8f, 4.9f, -4.4f), new Vector3(7.2f, 4.9f, -3.2f));
            Shelf(new Vector3(4.2f, 5.8f, -4.4f), new Vector3(5.6f, 5.8f, -3.2f));
            Shelf(new Vector3(4.2f, 6.7f, -3.0f), new Vector3(5.6f, 6.7f, -1.8f));
            Shelf(new Vector3(5.8f, 7.6f, -1.6f), new Vector3(7.2f, 7.6f, -.5f));
            // High beam: loft edge to the gable door, and the balcony outside.
            Block(structure, "High beam", new Vector3(4f, Loft - .3f, -.4f), new Vector3(i, Loft, .4f), Palette.Timber);
            Landing(new Vector3(4f, Loft, -.4f), new Vector3(4.6f, Loft, .4f));
            Block(structure, "Balcony", new Vector3(HalfX, Loft - .3f, -1.6f), new Vector3(HalfX + 2.2f, Loft, 1.6f), Palette.Plank);
            Landing(new Vector3(HalfX + 1.9f, Loft, -1.6f), new Vector3(HalfX + 2.2f, Loft, 1.6f));
            Railing(new Vector3(HalfX, Loft, -1.6f), new Vector3(HalfX + 2.2f, Loft, -1.6f));
            Railing(new Vector3(HalfX, Loft, 1.6f), new Vector3(HalfX + 2.2f, Loft, 1.6f));
            foreach (var z in new[] { -1.3f, 1.3f })
                Piece(structure, "Balcony bracket", new Vector3(HalfX + .75f, Loft - 1.05f, z), new Vector3(.2f, 2f, .2f), Palette.TimberDark, false, Quaternion.Euler(0, 0, 40));
            WorldSign(new Vector3(HalfX + 1.1f, Loft + 2.6f, 0), "GLIDE OFF HERE", .32f);
        }

        // --------------------------------------------------------------- lights
        static void BuildLights()
        {
            Lamp(new Vector3(0, 6.6f, 4.2f), 1.5f, 16);          // hall
            Lamp(new Vector3(-8.4f, 3.1f, 0), 1.2f, 9);          // tool locker
            Lamp(new Vector3(8.4f, 3.1f, 0), 1.2f, 9);           // sell counter
            Lamp(new Vector3(-5f, 3.1f, -4.6f), 1f, 8);          // wardrobe
            Lamp(new Vector3(0, 1.2f, -5.9f), 1.3f, 7, new Color(1, .55f, .25f)); // fire
            Lamp(new Vector3(0, 10.5f, -1f), 1f, 10);            // loft and beam
            Lamp(new Vector3(9f, 6.8f, 3f), .9f, 9);             // right gallery
            Lamp(new Vector3(-9f, 6.8f, 3f), .9f, 9);            // left gallery
            // Hanging lantern ring over the rug.
            Cylinder("Lantern ring", new Vector3(0, 6.3f, 4.2f), 1.1f, .1f, Palette.Iron, false);
            for (int n = 0; n < 6; n++)
            {
                float a = n * Mathf.PI / 3;
                GameArt.Shape("Lantern", PrimitiveType.Sphere, structure, new Vector3(Mathf.Cos(a) * 1.05f, 6.15f, 4.2f + Mathf.Sin(a) * 1.05f), Vector3.one * .26f, new Color(1, .82f, .5f), .2f, 1.6f);
            }
            Block(structure, "Lantern chain", new Vector3(-.03f, 6.4f, 4.17f), new Vector3(.03f, Eave + 1.5f, 4.23f), Palette.Iron, false);
        }

        // ------------------------------------------------------------- stations
        static bool BuildStations()
        {
            float i = HalfX - Wall;
            // Tool Locker: long counter on the left wall, pegboard of tools.
            var locker = Station<ToolLocker>("Guild Tool Locker", new Vector3(-8.5f, 0, 0), Vector3.right, 2.8f);
            Block(structure, "Locker counter", new Vector3(-i, 0, -2f), new Vector3(-9f, 1.1f, 2f), Palette.Timber);
            Block(structure, "Locker top", new Vector3(-i, 1.1f, -2.1f), new Vector3(-8.85f, 1.2f, 2.1f), Palette.TimberDark, false);
            Block(structure, "Pegboard", new Vector3(-i, 1.5f, -2.4f), new Vector3(-i + .06f, 3.4f, 2.4f), new Color(.72f, .56f, .36f), false);
            int slot = 0;
            foreach (var def in ItemCatalog.All)
            {
                if (!def.IsTool || slot >= 10) continue;
                float z = -2f + (slot % 5) * 1f, y = 2.0f + (slot / 5) * .8f; slot++;
                Block(structure, "Pegged tool", new Vector3(-i + .06f, y - .25f, z - .12f), new Vector3(-i + .16f, y + .25f, z + .12f), def.color, false);
            }
            var toolCase = GameArt.Model("tool-case/tool-case", locker.transform, 1.3f);
            if (toolCase) toolCase.transform.localPosition = new Vector3(0, 1.2f, -.9f); // on the counter
            WorldStations.MakeSign(locker.transform, "TOOL LOCKER", new Vector3(0, 3.1f, -.9f), UIKit.Gold, .4f);

            // Sell counter: right wall, salvage chest and scales.
            var sell = Station<SellStation>("Sell Station", new Vector3(8.5f, 0, 0), Vector3.left, 2.8f);
            Block(structure, "Sell counter", new Vector3(9f, 0, -2f), new Vector3(i, 1.1f, 2f), Palette.Timber);
            Block(structure, "Sell top", new Vector3(8.85f, 1.1f, -2.1f), new Vector3(i, 1.2f, 2.1f), Palette.TimberDark, false);
            Cylinder("Scale post", new Vector3(9.8f, 1.2f, -1.2f), .06f, .9f, Palette.Iron, false);
            Block(structure, "Scale beam", new Vector3(9.75f, 2.05f, -1.75f), new Vector3(9.85f, 2.12f, -.65f), Palette.Iron, false);
            Cylinder("Scale pan", new Vector3(9.8f, 1.7f, -1.7f), .25f, .04f, new Color(.78f, .62f, .3f), false);
            Cylinder("Scale pan", new Vector3(9.8f, 1.7f, -.7f), .25f, .04f, new Color(.78f, .62f, .3f), false);
            var chest = GameArt.Model("chest/open-chest", sell.transform, 1.2f);
            if (chest) { chest.transform.localPosition = new Vector3(-1.3f, 0, -.6f); sell.SetLid(chest.transform); }
            else Block(structure, "Salvage bin", new Vector3(8.2f, 0, .9f), new Vector3(9f, .8f, 1.9f), Palette.Timber);
            WorldStations.MakeSign(sell.transform, "SELL SALVAGE", new Vector3(0, 3.1f, 0), UIKit.Gold, .4f);

            // Wardrobe: change your technician's colours.
            var wardrobe = Station<Wardrobe>("Guild Wardrobe", new Vector3(-5f, 0, -4.8f), Vector3.forward, 2.6f);
            WorldStations.MakeSign(wardrobe.transform, "WARDROBE", new Vector3(0, 3.35f, -.5f), UIKit.Cream, .4f);
            return true;
        }

        // -------------------------------------------------------------- helpers
        static float GroundY(Vector3 world)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain) return terrain.SampleHeight(world) + terrain.transform.position.y;
            var hits = Physics.RaycastAll(new Vector3(world.x, 300, world.z), Vector3.down, 600, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var game = Expedition.Instance;
            foreach (var h in hits)
            {
                if (Root && h.collider.transform.IsChildOf(Root)) continue;
                if (game && game.creature && h.collider.transform.IsChildOf(game.creature.transform)) continue;
                if (h.collider.attachedRigidbody) continue;
                return h.point.y;
            }
            return world.y;
        }

        // Axis-aligned box between two local corners. Colliders stay on unless told otherwise.
        static GameObject Block(Transform parent, string name, Vector3 a, Vector3 b, Color color, bool collide = true, bool visible = true, float smooth = .2f, float emission = 0)
        {
            var min = Vector3.Min(a, b); var max = Vector3.Max(a, b);
            return Piece(parent, name, (min + max) / 2, max - min, color, collide, null, smooth, emission, visible);
        }

        static GameObject Piece(Transform parent, string name, Vector3 center, Vector3 size, Color color, bool collide, Quaternion? rotation = null, float smooth = .2f, float emission = 0, bool visible = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = new Vector3(Mathf.Max(.001f, size.x), Mathf.Max(.001f, size.y), Mathf.Max(.001f, size.z));
            if (!collide) Object.Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            if (!visible || color.a <= 0) { Object.Destroy(r); Object.Destroy(go.GetComponent<MeshFilter>()); return go; }
            r.sharedMaterial = GameArt.Colored(color, smooth, emission);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        static void Cylinder(string name, Vector3 baseCenter, float radius, float height, Color color, bool collide = true)
        {
            var go = GameArt.Shape(name, PrimitiveType.Cylinder, structure, baseCenter + Vector3.up * height / 2, new Vector3(radius * 2, height / 2, radius * 2), color);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            // Flat-topped box collision (a capsule top would roll you off).
            if (collide) Block(structure, name + " collision", baseCenter - new Vector3(radius * .72f, 0, radius * .72f), baseCenter + new Vector3(radius * .72f, height, radius * .72f), Color.clear, true, false);
        }

        // Guild-gold trim on the edges of a landing you can jump to.
        static void Landing(Vector3 a, Vector3 b)
        {
            float y = Mathf.Max(a.y, b.y), w = .07f;
            Block(structure, "Landing trim", new Vector3(a.x, y, a.z), new Vector3(b.x, y + .03f, a.z + w), Palette.Trim, false, true, .4f, .25f);
            Block(structure, "Landing trim", new Vector3(a.x, y, b.z - w), new Vector3(b.x, y + .03f, b.z), Palette.Trim, false, true, .4f, .25f);
            Block(structure, "Landing trim", new Vector3(a.x, y, a.z), new Vector3(a.x + w, y + .03f, b.z), Palette.Trim, false, true, .4f, .25f);
            Block(structure, "Landing trim", new Vector3(b.x - w, y, a.z), new Vector3(b.x, y + .03f, b.z), Palette.Trim, false, true, .4f, .25f);
        }

        static void Shelf(Vector3 a, Vector3 b)
        {
            Block(structure, "Tower shelf", new Vector3(a.x, a.y - .2f, a.z), new Vector3(b.x, b.y, b.z), Palette.Plank);
            Landing(a, b);
            foreach (var x in new[] { a.x + .08f, b.x - .08f }) foreach (var z in new[] { a.z + .08f, b.z - .08f })
                Block(structure, "Tower post", new Vector3(x - .06f, 0, z - .06f), new Vector3(x + .06f, a.y - .2f, z + .06f), Palette.TimberDark);
            // A few jars on the back edge, out of the way.
            Block(structure, "Jar", new Vector3(b.x - .35f, a.y, a.z + .1f), new Vector3(b.x - .15f, a.y + .3f, a.z + .3f), Palette.Glass, false, true, .8f, .1f);
        }

        static void CrateEdges(Vector3 a, Vector3 b)
        {
            foreach (var x in new[] { a.x, b.x }) foreach (var z in new[] { a.z, b.z })
                Block(structure, "Crate edge", new Vector3(x - .05f, a.y, z - .05f), new Vector3(x + .05f, b.y + .01f, z + .05f), Palette.TimberDark, false);
        }

        static void Railing(Vector3 a, Vector3 b)
        {
            const float h = .95f;
            var dir = b - a; float len = dir.magnitude; if (len < .1f) return;
            var mid = (a + b) / 2; var rot = Quaternion.LookRotation(dir.normalized);
            Piece(structure, "Rail", mid + Vector3.up * h, new Vector3(.12f, .1f, len), Palette.TimberDark, false, rot);
            Piece(structure, "Rail guard", mid + Vector3.up * (h / 2), new Vector3(.1f, h, len), Color.clear, true, rot, .2f, 0, false);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / .5f));
            for (int s = 0; s <= n; s++)
                Piece(structure, "Baluster", Vector3.Lerp(a, b, s / (float)n) + Vector3.up * (h / 2), new Vector3(.07f, h, .07f), Palette.TimberDark, false);
        }

        static void Gable(string name, float x0, float x1, Vector2[] tris)
        {
            var verts = new List<Vector3>(); var idx = new List<int>();
            foreach (var x in new[] { x0, x1 })
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int s = verts.Count;
                    for (int v = 0; v < 3; v++) verts.Add(new Vector3(x, tris[t + v].y, tris[t + v].x));
                    for (int v = 0; v < 3; v++) verts.Add(new Vector3(x, tris[t + v].y, tris[t + v].x));
                    idx.Add(s); idx.Add(s + 1); idx.Add(s + 2);          // one side
                    idx.Add(s + 5); idx.Add(s + 4); idx.Add(s + 3);      // the other side
                }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetTriangles(idx, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(structure, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = GameArt.Colored(Palette.Plaster, .2f);
        }

        static void Lamp(Vector3 local, float intensity, float range, Color? color = null)
        {
            var l = new GameObject("Hall light").AddComponent<Light>();
            l.transform.SetParent(Root, false); l.transform.localPosition = local;
            l.type = LightType.Point; l.color = color ?? new Color(1, .8f, .55f); l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
        }

        static void WorldSign(Vector3 local, string text, float scale)
        {
            var anchor = new GameObject(text + " sign").transform; anchor.SetParent(Root, false); anchor.localPosition = local;
            WorldStations.MakeSign(anchor, text, Vector3.zero, UIKit.Cream, scale);
        }

        static T Station<T>(string name, Vector3 local, Vector3 facing, float radius) where T : Station
        {
            var s = new GameObject(name).AddComponent<T>();
            s.transform.SetParent(Root, false);
            s.transform.localPosition = local;
            s.transform.localRotation = Quaternion.LookRotation(facing);
            s.radius = radius;
            return s;
        }

        static Color BookColour(int n)
        {
            Color[] c = { new Color(.55f, .18f, .16f), new Color(.2f, .36f, .5f), new Color(.3f, .48f, .3f), new Color(.72f, .56f, .26f), new Color(.4f, .26f, .46f) };
            return c[n % c.Length];
        }

        // Meadow props (trees, rocks, bushes) standing where the house goes are
        // hidden so nothing pokes through its walls or blocks the door steps.
        static void ClearSite()
        {
            var game = Expedition.Instance;
            var area = new Bounds(Vector3.zero, Vector3.zero);
            var corners = new[] { new Vector3(-HalfX - 1, 0, -HalfZ - 1), new Vector3(HalfX + 3.5f, 0, -HalfZ - 1), new Vector3(-HalfX - 1, 0, HalfZ + 6), new Vector3(HalfX + 3.5f, 0, HalfZ + 6) };
            for (int c = 0; c < corners.Length; c++) { var p = Root.InverseTransformPoint(Root.TransformPoint(corners[c])); if (c == 0) area = new Bounds(p, Vector3.zero); else area.Encapsulate(p); }
            area.Encapsulate(new Vector3(0, -8, 0)); area.Encapsulate(new Vector3(0, Ridge + 3, 0));
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var t = r.transform;
                if (t.IsChildOf(Root) || r is ParticleSystemRenderer || r is LineRenderer) continue;
                if (game && (t.IsChildOf(game.transform) || game.creature && t.IsChildOf(game.creature.transform) || game.player && t.IsChildOf(game.player.transform))) continue;
                if (t.GetComponentInParent<Station>() || t.GetComponentInParent<DebrisPickup>() || t.GetComponentInParent<Canvas>()) continue;
                var c = Root.InverseTransformPoint(r.bounds.center);
                if (!area.Contains(c)) continue;
                // Hide the prop's own root (the child of the container it sits in).
                var prop = t; while (prop.parent && prop.parent.childCount < 12 && prop.parent.parent) prop = prop.parent;
                prop.gameObject.SetActive(false);
            }
        }
    }

    // Marks a surface the Climbing Claw can grip besides Mossback himself.
    public sealed class ClimbWall : MonoBehaviour { }

    // Whether the technician is inside the hall: the camera pulls in, and a
    // one-time caption explains the layout.
    public sealed class HallVolume : MonoBehaviour
    {
        public Vector3 min, max;
        public bool Inside { get; private set; }
        bool was, toldLayout;
        void Update()
        {
            var g = Expedition.Instance; if (!g || !g.player) return;
            var p = transform.InverseTransformPoint(g.player.transform.position);
            Inside = p.x > min.x && p.x < max.x && p.z > min.z && p.z < max.z && p.y > min.y && p.y < max.y;
            if (Inside == was) return;
            was = Inside;
            g.player.distance = Inside ? 5f : MovementTuning.Current.distance;
            if (Inside && !toldLayout)
            {
                toldLayout = true;
                g.Caption = "The Guild House: Tool Locker left, sell counter right, wardrobe at the back. Stairs, crates, shelves and the claw wall lead up.";
            }
        }
    }

    // Hidden trinkets in the hall, put back at the start of every mission.
    public sealed class HallFinds : MonoBehaviour
    {
        public (ItemId id, Vector3 local)[] spots = new (ItemId, Vector3)[0];
        readonly List<DebrisPickup> placed = new List<DebrisPickup>();
        Expedition game;
        void Start() { game = Expedition.Instance; if (game) game.Began += Place; Place(); }
        void OnDestroy() { if (game) game.Began -= Place; }
        void Place()
        {
            foreach (var d in placed) if (d) Destroy(d.gameObject);
            placed.Clear();
            foreach (var (id, local) in spots)
            {
                var d = DebrisPickup.Place(id, transform.TransformPoint(local), null);
                if (d) placed.Add(d);
            }
        }
    }

    // Opens the Guild Member editor in the guild house.
    public sealed class Wardrobe : Station
    {
        public override string Prompt => "<color=#FFBC4A>" + GameInput.Display(GameInput.Interact) + "</color>  Wardrobe: change your technician's colours";
        public override void Interact() { var ed = GuildMemberEditor.Instance; if (ed) ed.Open(); }
    }
}

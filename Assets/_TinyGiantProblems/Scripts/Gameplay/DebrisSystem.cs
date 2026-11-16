using UnityEngine;

namespace TinyGiantProblems
{
    // Mossback sheds moss, twigs and burrs onto its deck and the meadow below:
    // during shakes and sneezes, and now and then while it shifts its weight.
    // Also handles walking over loose salvage to pick it up.
    [DefaultExecutionOrder(40)]
    public sealed class DebrisSystem : MonoBehaviour
    {
        public static DebrisSystem Instance { get; private set; }
        public int maxLoose = 70;
        public float pickupRadius = 1.6f;
        public Vector3 droneLocal = new Vector3(3.5f, -5.5f, 21f);
        Expedition game;
        float nextShed;

        void Awake() => Instance = this;
        void OnDestroy()
        {
            if (game) { game.Began -= OnBegin; game.ReactionImpact -= OnImpact; }
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            game = Expedition.Instance;
            if (!game) { enabled = false; return; }
            game.Began += OnBegin;
            game.ReactionImpact += OnImpact;
            nextShed = Random.Range(18f, 30f);
        }

        void OnBegin()
        {
            for (int i = DebrisPickup.All.Count - 1; i >= 0; i--)
                if (DebrisPickup.All[i]) Destroy(DebrisPickup.All[i].gameObject);
            nextShed = Random.Range(18f, 30f);
            PlaceLostDrone();
            // A little salvage already waiting below, so the yard has a reason to exist from minute one.
            for (int i = 0; i < 6; i++) Shed(true, false);
        }

        void PlaceLostDrone()
        {
            var frame = game.creature.transform;
            var p = frame.TransformPoint(droneLocal);
            if (Physics.Raycast(p + Vector3.up * 30, Vector3.down, out var hit, 80, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(frame))
                p = hit.point + Vector3.up * .3f;
            DebrisPickup.Place(ItemId.LostDrone, p, null);
        }

        void OnImpact(string reaction)
        {
            int n = reaction == "SNEEZE" ? 4 : 8;
            for (int i = 0; i < n; i++) Shed(Random.value < .7f, true);
            if (game) game.Caption += reaction == "SNEEZE" ? "  Debris rained down!" : "  Loose moss went flying. Salvage below!";
        }

        // Pops a piece of debris off the coat, either over the side or onto the deck.
        public void Shed(bool overTheSide, bool fling)
        {
            if (DebrisPickup.All.Count >= maxLoose) return;
            var frame = game.creature.transform;
            float side = Random.value < .5f ? -1 : 1;
            float z = Random.Range(-8f, 16f);
            ItemId id = Random.value < .45f ? ItemId.MossClump : Random.value < .6f ? ItemId.Twig : ItemId.Burr;
            Vector3 pos, vel;
            if (overTheSide)
            {
                pos = frame.TransformPoint(new Vector3(side * 7.5f, 9f, z));
                vel = frame.right * side * Random.Range(5f, 9f) + Vector3.up * Random.Range(2f, 5f);
                if (!fling)
                {
                    // Pre-placed: drop straight onto the meadow near the flank.
                    var ground = frame.TransformPoint(new Vector3(side * Random.Range(10f, 13f), 2, z));
                    if (Physics.Raycast(ground, Vector3.down, out var hit, 80, ~0, QueryTriggerInteraction.Ignore))
                    {
                        DebrisPickup.Place(id, hit.point + Vector3.up * .25f, null);
                        return;
                    }
                }
            }
            else
            {
                pos = frame.TransformPoint(new Vector3(Random.Range(-3.5f, 3.5f), 13.4f, z));
                vel = Random.insideUnitSphere * 2 + Vector3.up * 2;
            }
            DebrisPickup.Spawn(id, pos, vel);
        }

        void Update()
        {
            if (!game || !game.IsPlaying || game.Paused) return;
            nextShed -= Time.deltaTime;
            if (nextShed <= 0 && game.WarningRemaining <= 0)
            {
                Shed(true, true);
                if (Random.value < .5f) Shed(false, true);
                nextShed = Random.Range(20f, 38f);
            }
            var economy = GuildEconomy.Instance;
            if (!economy) return;
            var p = game.player;
            var center = p.transform.position + Vector3.up * .8f;
            for (int i = DebrisPickup.All.Count - 1; i >= 0; i--)
            {
                var d = DebrisPickup.All[i];
                if (!d || d.Collecting || d.Age < .6f) continue;
                if ((d.transform.position - center).sqrMagnitude > pickupRadius * pickupRadius) continue;
                if (economy.Inventory.RoomFor(d.item) < 1) continue;
                d.Attract(p.transform);
            }
        }
    }
}

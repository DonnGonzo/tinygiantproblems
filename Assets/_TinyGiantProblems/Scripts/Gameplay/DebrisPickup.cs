using System.Collections.Generic;
using UnityEngine;

namespace TinyGiantProblems
{
    // A loose item in the world: falls with physics, settles, then waits to be
    // collected by walking over it or pulling it in with the Multi-Grabber.
    public sealed class DebrisPickup : MonoBehaviour
    {
        public static readonly List<DebrisPickup> All = new List<DebrisPickup>();
        public ItemId item;
        public int count = 1;
        public bool Settled { get; private set; }
        public bool Collecting => magnet != null;
        public float Age { get; private set; }
        Rigidbody body;
        SphereCollider shape;
        Transform visual, magnet;
        float magnetSpeed, spin;
        ParticleSystem sparkle;

        public static DebrisPickup Spawn(ItemId id, Vector3 position, Vector3 velocity, int count = 1)
        {
            var def = ItemCatalog.Get(id);
            if (def == null) return null;
            var go = new GameObject(def.name + " (pickup)");
            go.transform.position = position;
            go.transform.rotation = Random.rotation;
            var d = go.AddComponent<DebrisPickup>();
            d.item = id; d.count = count;
            d.visual = new GameObject("Visual").transform;
            d.visual.SetParent(go.transform, false);
            var model = GameArt.ItemVisual(def, d.visual, def.size);
            // Imported models use ground pivots; centre them on the physics sphere.
            model.transform.localPosition = Vector3.down * def.size * .35f;
            d.shape = go.AddComponent<SphereCollider>();
            d.shape.radius = Mathf.Max(.15f, def.size * .38f);
            d.body = go.AddComponent<Rigidbody>();
            d.body.mass = .4f;
            d.body.linearDamping = .15f;
            d.body.angularDamping = .6f;
            d.body.interpolation = RigidbodyInterpolation.Interpolate;
            d.body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            d.body.linearVelocity = velocity;
            d.body.angularVelocity = Random.insideUnitSphere * 6;
            var player = Expedition.Instance ? Expedition.Instance.player : null;
            if (player && player.capsule) Physics.IgnoreCollision(d.shape, player.capsule);
            if (def.sellValue >= 30)
            {
                d.sparkle = GameArt.Particles("Sparkle", go.transform, new Color(1, .95f, .6f, 1), .12f, .4f, .9f, false);
                var em = d.sparkle.emission; em.rateOverTime = 6;
                var sh = d.sparkle.shape; sh.radius = def.size * .6f;
                d.sparkle.Play();
            }
            return d;
        }

        // Places an already-settled item (used for authored finds such as the lost drone).
        public static DebrisPickup Place(ItemId id, Vector3 position, Transform parent)
        {
            var d = Spawn(id, position, Vector3.zero);
            if (!d) return null;
            d.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            d.Settle(parent);
            return d;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            Age += Time.deltaTime;
            if (magnet)
            {
                magnetSpeed = Mathf.Min(magnetSpeed + Time.deltaTime * 30, 24);
                var target = magnet.position + Vector3.up * .9f;
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
                visual.localScale = Vector3.one * Mathf.Clamp01(Vector3.Distance(transform.position, target) / 1.2f + .3f);
                if ((transform.position - target).sqrMagnitude < .09f) Arrive();
                return;
            }
            if (!Settled)
            {
                if (transform.position.y < -40) { Destroy(gameObject); return; }
                if (Age > 5f || (Age > .7f && body && body.IsSleeping())) Settle(null);
            }
            else
            {
                // Gentle idle bob so loose salvage reads as collectable.
                spin += Time.deltaTime;
                visual.localPosition = Vector3.up * (.12f + Mathf.Sin(spin * 2.2f) * .06f);
                visual.localRotation = Quaternion.Euler(0, spin * 40, 0);
            }
        }

        public void Settle(Transform parent)
        {
            if (Settled) return;
            Settled = true;
            if (body) { body.isKinematic = true; body.interpolation = RigidbodyInterpolation.None; }
            if (shape) shape.isTrigger = true;
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            if (!parent && Physics.Raycast(transform.position + Vector3.up * .3f, Vector3.down, out var hit, 3, ~0, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point + Vector3.up * shape.radius * .6f;
                var g = Expedition.Instance;
                // Ride along with Mossback when it lands on the deck or coat.
                if (g && g.creature && hit.collider.transform.IsChildOf(g.creature.transform)) parent = hit.collider.transform;
            }
            if (parent) transform.SetParent(parent, true);
        }

        public bool Attract(Transform toward, float startSpeed = 4)
        {
            if (magnet) return true;
            magnet = toward; magnetSpeed = startSpeed;
            transform.SetParent(null, true);
            if (body) body.isKinematic = true;
            if (shape) shape.enabled = false;
            return true;
        }

        void Arrive()
        {
            var economy = GuildEconomy.Instance;
            int left = economy ? economy.Collect(item, count) : 0;
            if (left > 0)
            {
                // Bag filled up while this was flying in: drop the rest at the player's feet.
                count = left; magnet = null; Settled = false;
                if (shape) { shape.enabled = true; shape.isTrigger = false; }
                if (body) { body.isKinematic = false; body.linearVelocity = Vector3.up * 2; }
                visual.localScale = Vector3.one; Age = 0;
                if (Expedition.Instance) Expedition.Instance.Caption = "Your bag is full. Sell salvage at the yard or open TAB to sort.";
                return;
            }
            Destroy(gameObject);
        }
    }
}

using UnityEngine;

namespace TinyGiantProblems
{
    // Tiny synthesized UI/feedback sounds so new systems are never silent.
    // Replace with authored clips later; callers only use the static fields.
    public static class ProceduralSfx
    {
        const int Rate = 44100;
        static AudioClip coin, pickup, pop, sell, buy, deny, scrub, suck, whoosh, equip, munch;
        public static AudioClip Coin => coin ? coin : coin = Notes("coin", new[] { 988f, 1319f }, .07f, .18f);
        public static AudioClip Pickup => pickup ? pickup : pickup = Sweep("pickup", 520, 980, .09f, .35f);
        public static AudioClip Pop => pop ? pop : pop = Sweep("pop", 900, 240, .08f, .45f);
        public static AudioClip Sell => sell ? sell : sell = Notes("sell", new[] { 784f, 988f, 1175f, 1568f }, .065f, .22f);
        public static AudioClip Buy => buy ? buy : buy = Notes("buy", new[] { 659f, 880f, 1319f }, .08f, .25f);
        public static AudioClip Deny => deny ? deny : deny = Notes("deny", new[] { 220f, 185f }, .09f, .3f, true);
        public static AudioClip Scrub => scrub ? scrub : scrub = Noise("scrub", .16f, .25f, 8);
        public static AudioClip Suck => suck ? suck : suck = Noise("suck", .22f, .2f, 2, 300, 1400);
        public static AudioClip Whoosh => whoosh ? whoosh : whoosh = Noise("whoosh", .35f, .25f, 1, 200, 900);
        public static AudioClip Equip => equip ? equip : equip = Sweep("equip", 300, 620, .05f, .25f);
        public static AudioClip Munch => munch ? munch : munch = Noise("munch", .3f, .4f, 14);
        // Movement and UI feedback.
        static AudioClip jump, land, step, click, hover, objective, fanfare, thud;
        public static AudioClip Jump => jump ? jump : jump = Sweep("jump", 240, 520, .09f, .22f);
        public static AudioClip Land => land ? land : land = Noise("land", .14f, .5f, 1, 120, 400);
        public static AudioClip Step => step ? step : step = Noise("step", .05f, .28f, 1, 300, 900);
        public static AudioClip Click => click ? click : click = Sweep("click", 1400, 900, .035f, .2f);
        public static AudioClip Hover => hover ? hover : hover = Sweep("hover", 1800, 1900, .02f, .06f);
        public static AudioClip Objective => objective ? objective : objective = Notes("objective", new[] { 659f, 988f, 1319f }, .07f, .22f);
        public static AudioClip Fanfare => fanfare ? fanfare : fanfare = Notes("fanfare", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, .11f, .24f);
        public static AudioClip Thud => thud ? thud : thud = Noise("thud", .25f, .7f, 1, 60, 220);

        static float Env(float t, float len) => Mathf.Clamp01(t / .006f) * Mathf.Clamp01((len - t) / (len * .6f));

        static AudioClip Notes(string name, float[] freqs, float noteLen, float volume, bool square = false)
        {
            int per = (int)(noteLen * Rate), n = per * freqs.Length + Rate / 10;
            var data = new float[n];
            for (int k = 0; k < freqs.Length; k++)
                for (int i = 0; i < per + Rate / 10 && k * per + i < n; i++)
                {
                    float t = i / (float)Rate, len = noteLen + .1f;
                    float w = Mathf.Sin(2 * Mathf.PI * freqs[k] * t);
                    if (square) w = Mathf.Sign(w) * .5f;
                    data[k * per + i] += w * volume * Env(t, len) * Mathf.Exp(-t * 18);
                }
            return Make(name, data);
        }

        static AudioClip Sweep(string name, float from, float to, float len, float volume)
        {
            int n = (int)(len * Rate); var data = new float[n]; float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, f = Mathf.Lerp(from, to, t / len);
                phase += 2 * Mathf.PI * f / Rate;
                data[i] = Mathf.Sin(phase) * volume * Env(t, len);
            }
            return Make(name, data);
        }

        static AudioClip Noise(string name, float len, float volume, float pulses, float lowHz = 600, float highHz = 4000)
        {
            int n = (int)(len * Rate); var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float lp = 0, hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)rng.NextDouble() * 2 - 1;
                float cut = Mathf.Lerp(lowHz, highHz, t / len);
                float a = Mathf.Clamp01(2 * Mathf.PI * cut / Rate);
                lp += a * (white - lp);
                hp = .97f * (hp + lp - prev); prev = lp;
                float pulse = .55f + .45f * Mathf.Sin(t * pulses * 2 * Mathf.PI);
                data[i] = hp * volume * Env(t, len) * pulse * 2.5f;
            }
            return Make(name, data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create("Synth " + name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void Play(AudioClip clip, float volume = 1)
        {
            var g = Expedition.Instance;
            if (g && g.effects && clip) g.effects.PlayOneShot(clip, volume);
        }
    }
}

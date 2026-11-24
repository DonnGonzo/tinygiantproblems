using UnityEngine;

namespace TinyGiantProblems
{
    // Player preferences (Settings menu): look sensitivity and inversion,
    // screen shake, master/music/effects volume and graphics quality.
    // Stored in PlayerPrefs; changes apply immediately.
    public static class GameSettings
    {
        public static float LookSensitivity { get => PlayerPrefs.GetFloat("TGP.Set.Look", 1); set { PlayerPrefs.SetFloat("TGP.Set.Look", Mathf.Clamp(value, .2f, 3)); Changed?.Invoke(); } }
        public static bool InvertY { get => PlayerPrefs.GetInt("TGP.Set.InvertY", 0) == 1; set { PlayerPrefs.SetInt("TGP.Set.InvertY", value ? 1 : 0); Changed?.Invoke(); } }
        public static float ScreenShake { get => PlayerPrefs.GetFloat("TGP.Set.Shake", 1); set { PlayerPrefs.SetFloat("TGP.Set.Shake", Mathf.Clamp01(value)); Changed?.Invoke(); } }
        public static float MasterVolume { get => PlayerPrefs.GetFloat("TGP.Set.Master", 1); set { PlayerPrefs.SetFloat("TGP.Set.Master", Mathf.Clamp01(value)); Apply(); } }
        public static float MusicVolume { get => PlayerPrefs.GetFloat("TGP.Set.Music", .7f); set { PlayerPrefs.SetFloat("TGP.Set.Music", Mathf.Clamp01(value)); Apply(); } }
        public static float EffectsVolume { get => PlayerPrefs.GetFloat("TGP.Set.Sfx", 1); set { PlayerPrefs.SetFloat("TGP.Set.Sfx", Mathf.Clamp01(value)); Apply(); } }
        public static int Quality { get => PlayerPrefs.GetInt("TGP.Set.Quality", QualitySettings.GetQualityLevel()); set { PlayerPrefs.SetInt("TGP.Set.Quality", value); Apply(); } }
        public static event System.Action Changed;

        // Applies volumes and quality to the running game.
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            var g = Expedition.Instance;
            if (g && g.music) g.music.volume = MusicVolume * .3f;
            if (g && g.effects) g.effects.volume = EffectsVolume;
            int q = Mathf.Clamp(Quality, 0, QualitySettings.names.Length - 1);
            if (QualitySettings.GetQualityLevel() != q) QualitySettings.SetQualityLevel(q, true);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}

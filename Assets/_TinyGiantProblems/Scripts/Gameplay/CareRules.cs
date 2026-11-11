using System;
namespace TinyGiantProblems
{
    public enum CareTool { Vacuum, Groomer, Sprayer, Tether }
    public enum ExpeditionPhase { Briefing, Working, Extraction, Debrief }
    [Serializable]
    public sealed class CareProgress
    {
        public float Progress;
        public bool Complete => Progress >= 1f;
        public bool Apply(CareTool required, CareTool selected, float distance, bool blocked, float seconds)
        {
            if (Complete || blocked || required != selected || distance > 3.2f || seconds <= 0) return false;
            Progress = Math.Min(1f, Progress + Math.Min(seconds, 0.1f) / 6f);
            return Complete;
        }
    }
    public static class CareRules
    {
        public static bool CanExtract(int completed, float comfort) => completed == 3 && comfort >= 70;
        public static int Reward(int completed, int recoveries, float seconds) =>
            Math.Max(0, completed * 100 + 150 - recoveries * 25 - (int)Math.Max(0, seconds - 480) / 10);
        public static bool LaunchAllowed(bool braced, bool tethered, float grace) => !braced && !tethered && grace <= 0;
    }
}

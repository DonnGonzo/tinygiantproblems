using UnityEngine;

namespace TinyGiantProblems
{
    [CreateAssetMenu(menuName = "Tiny Giant Problems/Mission")]
    public sealed class MissionDef : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string brief;
        [Tooltip("Difficulty tier 1-4: faster sneezes/shakes and more mess")]
        [Range(1, 4)] public int tier = 1;
        [Tooltip("Mission ids that must be completed first")]
        public string[] requires = new string[0];
        public ObjectiveDef[] objectives = new ObjectiveDef[0];
        public ChallengeDef[] challenges = new ChallengeDef[0];
        public int rewardCoins = 100;
        [Tooltip("Optional item granted on first completion")]
        public ItemId rewardItem = ItemId.None;

        // Seconds between reactions shrink and mess grows with the tier.
        public float ReactionScale => 1.15f - .15f * tier;
        public float MessScale => .85f + .15f * tier;
    }
}

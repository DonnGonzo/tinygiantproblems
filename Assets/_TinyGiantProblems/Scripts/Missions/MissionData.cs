using System;
using UnityEngine;

namespace TinyGiantProblems
{
    // Everything the game can report toward a mission objective.
    public enum TaskKind
    {
        CareJob,        // subtype = care job index
        ClearMess,      // subtype = MessKind, -1 any
        SellDebris,     // pieces sold (yard counter or Supply Crate)
        FindTrinket,    // shiny pebble / guild badge picked up
        RecoverDrone,   // lost survey drone sold
        CalmGiant,      // snack eaten
        PlacePlatform,  // Floating Platform set on the back
        PlaceKit,       // platform kit placed (subtype = ItemId, -1 any)
        ClawClimb,      // metres climbed in one claw climb (best, not sum)
        BraceReaction,  // sneeze/shake survived while on Mossback
        Glide,          // seconds spent gliding
        Scan,           // scanner pings
        Comfort,        // Mossback comfort % (current value, not sum)
        CollectDebris,  // debris pieces picked up
    }

    public enum MessKind { Grime, Burr, Mite }

    public enum ChallengeKind
    {
        TimeLimit,      // extract within `seconds`
        NoFallHurt,     // never winded by a long fall
        NoLaunch,       // never thrown by a sneeze or shake
        NoRescue,       // never used the safety winch
        NoSting,        // never stung by a Bramblemite
        AllTrinkets,    // found every hidden trinket
    }

    [Serializable]
    public sealed class ObjectiveDef
    {
        public string title;
        public TaskKind kind;
        [Tooltip("-1 = any. CareJob: job index. ClearMess: MessKind. PlaceKit: ItemId.")]
        public int subtype = -1;
        public int target = 1;
        [Tooltip("Optional side task: not needed to extract, pays its own reward")]
        public bool optional;
        [Tooltip("Coins paid the moment this objective is done (on top of the mission reward)")]
        public int reward;
        public bool UsesBest => kind == TaskKind.ClawClimb || kind == TaskKind.Comfort;
    }

    [Serializable]
    public sealed class ChallengeDef
    {
        public string title;
        public ChallengeKind kind;
        public float seconds;
        public int reward;
    }
}

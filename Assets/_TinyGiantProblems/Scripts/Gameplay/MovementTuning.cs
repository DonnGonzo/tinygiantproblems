using UnityEngine;

namespace TinyGiantProblems
{
    // Every feel value for the technician and the follow camera, editable in the
    // Inspector (Resources/MovementTuning.asset). A tiny person on a giant: quick
    // to react, but with a little weight (short acceleration ramp, heavier fall).
    [CreateAssetMenu(menuName = "Tiny Giant Problems/Movement Tuning")]
    public sealed class MovementTuning : ScriptableObject
    {
        [Header("Ground movement")]
        public float walkSpeed = 4.6f;
        public float sprintSpeed = 7.4f;
        [Tooltip("m/s² while speeding up on the ground")] public float acceleration = 30;
        [Tooltip("m/s² while slowing down on the ground")] public float deceleration = 38;
        [Tooltip("m/s² of steering while airborne")] public float airAcceleration = 9;
        [Tooltip("Degrees per second the body turns toward the move direction")] public float turnSpeed = 720;
        [Tooltip("Walkable slope; steeper ground makes you slide")] public float maxSlope = 52;
        public float slideSpeed = 6;

        [Header("Jumping and falling")]
        public float jumpHeight = 1.35f;
        public float gravity = 22;
        [Tooltip("Extra gravity while falling, for a weightier arc")] public float fallGravityMultiplier = 1.45f;
        [Tooltip("Gravity multiplier when jump is released early (short hops)")] public float lowJumpMultiplier = 1.9f;
        public float maxFallSpeed = 30;
        [Tooltip("Seconds after leaving a ledge when jumping still works")] public float coyoteTime = .13f;
        [Tooltip("Seconds a jump press is remembered before landing")] public float jumpBuffer = .15f;

        [Header("Camera")]
        public float mouseSensitivity = .12f;
        public float gamepadSensitivity = 160;
        public bool invertY;
        public float minPitch = -12, maxPitch = 65;
        public float distance = 8, minDistance = 3.5f, maxDistance = 15;
        [Tooltip("Seconds to catch up with the technician")] public float followDamping = .08f;
        [Tooltip("Camera keeps this far from walls")] public float collisionRadius = .3f;
        [Tooltip("Seconds to ease back out after a wall pushed it in")] public float collisionRecover = .35f;
        public float focusHeight = 1.3f;

        static MovementTuning cached;
        public static MovementTuning Current
        {
            get
            {
                if (!cached) cached = Resources.Load<MovementTuning>("MovementTuning");
                if (!cached) cached = CreateInstance<MovementTuning>();
                return cached;
            }
        }
    }
}

using System;
using UnityEngine;

namespace Hockey.Core
{
    /// <summary>
    /// Central, designer-tunable configuration for a match. A default instance is created in code by
    /// the bootstrap if no asset is provided, so the game runs with sensible values out of the box.
    /// Create one via Assets &gt; Create &gt; Hockey &gt; Game Config to tweak in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Hockey/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Match")]
        [Tooltip("Skaters per team, excluding the goalie. 3 = 3v3 (default); scales to 5 = 5v5.")]
        [Range(1, 5)] public int playersPerSide = 3;
        public bool useGoalies = true;
        [Range(1, 3)] public int periods = 3;
        [Tooltip("Length of each period in seconds (arcade-short by default).")]
        public float periodSeconds = 180f;
        public Difficulty difficulty = Difficulty.Pro;

        [Header("Tuning")]
        public SkatingTuning skating = SkatingTuning.Default;
        public PuckTuning puck = PuckTuning.Default;

        /// <summary>Total skaters on the ice per team including the goalie.</summary>
        public int PlayersOnIcePerSide => playersPerSide + (useGoalies ? 1 : 0);

        public static GameConfig CreateDefault()
        {
            var c = CreateInstance<GameConfig>();
            c.playersPerSide = 3;
            c.useGoalies = true;
            c.periods = 3;
            c.periodSeconds = 180f;
            c.difficulty = Difficulty.Pro;
            c.skating = SkatingTuning.Default;
            c.puck = PuckTuning.Default;
            return c;
        }
    }

    /// <summary>How skaters accelerate, carve, and stop. All units are SI (metres, seconds).</summary>
    [Serializable]
    public struct SkatingTuning
    {
        public float maxSpeed;           // m/s top speed
        public float acceleration;       // m/s^2 of forward skate force
        public float sprintMultiplier;   // top-speed multiplier while sprinting
        public float turnResponse;       // how quickly facing rotates toward the move direction (1/s)
        public float lateralGrip;        // edge grip; lower = more drift/slide when turning
        public float stopDamping;        // deceleration applied when there is no input

        public static SkatingTuning Default => new SkatingTuning
        {
            maxSpeed = 9f,
            acceleration = 28f,
            sprintMultiplier = 1.35f,
            turnResponse = 12f,
            lateralGrip = 8f,
            stopDamping = 3.5f
        };
    }

    /// <summary>Puck mass, ice friction, and shot/pass speeds.</summary>
    [Serializable]
    public struct PuckTuning
    {
        public float mass;                // kg (regulation ~0.16-0.17)
        public float linearDamping;       // ice friction acting on the puck
        public float shotSpeed;           // m/s for a full-power shot
        public float passSpeed;           // m/s for a pass
        public float maxControlDistance;  // puck "sticks" to a carrier within this range

        public static PuckTuning Default => new PuckTuning
        {
            mass = 0.17f,
            linearDamping = 0.35f,
            shotSpeed = 28f,
            passSpeed = 18f,
            maxControlDistance = 1.2f
        };
    }
}

using System;
using UnityEngine;

namespace Hockey.Core
{
    /// <summary>
    /// Tunable knobs that make AI players easier or harder. Difficulty primarily tunes the
    /// OPPONENT; teammate competence is deliberately floored high so the player always has a
    /// capable line (avoiding the "my teammate sucks" feeling, which is the #1 thing to avoid).
    /// </summary>
    [Serializable]
    public struct AISkillProfile
    {
        [Range(0f, 1f)] public float reaction;      // 1 = reacts to play changes almost instantly
        [Range(0f, 1f)] public float passAccuracy;  // 1 = passes go exactly where intended
        [Range(0f, 1f)] public float shotAccuracy;  // 1 = shots hit their aim point
        [Range(0f, 1f)] public float positioning;   // support lanes, spacing, back-check discipline
        [Range(0f, 1f)] public float aggression;    // forecheck / hitting tendency
        [Range(0f, 1f)] public float speedScale;    // skating speed multiplier vs the base tuning
        [Range(0f, 1f)] public float goalieSkill;   // save reaction & angle play (for goalies)

        /// <summary>Opponent skill curve. "Pro" (default) is meant to be genuinely beatable on a good night.</summary>
        public static AISkillProfile ForOpponent(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Rookie:
                    return new AISkillProfile { reaction = 0.35f, passAccuracy = 0.55f, shotAccuracy = 0.45f, positioning = 0.45f, aggression = 0.35f, speedScale = 0.85f, goalieSkill = 0.55f };
                case Difficulty.AllStar:
                    return new AISkillProfile { reaction = 0.80f, passAccuracy = 0.88f, shotAccuracy = 0.80f, positioning = 0.88f, aggression = 0.75f, speedScale = 1.00f, goalieSkill = 0.85f };
                case Difficulty.Legend:
                    return new AISkillProfile { reaction = 0.95f, passAccuracy = 0.96f, shotAccuracy = 0.92f, positioning = 0.97f, aggression = 0.90f, speedScale = 1.05f, goalieSkill = 0.93f };
                case Difficulty.Pro:
                default:
                    return new AISkillProfile { reaction = 0.60f, passAccuracy = 0.75f, shotAccuracy = 0.65f, positioning = 0.70f, aggression = 0.55f, speedScale = 0.95f, goalieSkill = 0.72f };
            }
        }

        /// <summary>
        /// Teammate skill curve. Starts from the opponent curve but floors the "smart" stats so even
        /// on Rookie your linemates position well, find lanes, and complete passes.
        /// </summary>
        public static AISkillProfile ForTeammate(Difficulty d)
        {
            var p = ForOpponent(d);
            p.positioning = Mathf.Max(p.positioning, 0.80f);
            p.passAccuracy = Mathf.Max(p.passAccuracy, 0.80f);
            p.reaction = Mathf.Max(p.reaction, 0.65f);
            p.speedScale = Mathf.Max(p.speedScale, 0.95f);
            return p;
        }
    }
}

using UnityEngine;
using Hockey.Core;

namespace Hockey.Gameplay
{
    /// <summary>
    /// Trigger volume sitting in the mouth of a net. When the puck enters, the team attacking that net
    /// (the opponent of the defending side) is awarded a goal.
    /// </summary>
    public class GoalTrigger : MonoBehaviour
    {
        public TeamSide DefendingSide;

        void OnTriggerEnter(Collider other)
        {
            if (MatchManager.Instance == null) return;
            var puck = other.GetComponentInParent<PuckController>();
            if (puck == null) return;
            MatchManager.Instance.OnGoal(DefendingSide.Opponent());
        }
    }
}

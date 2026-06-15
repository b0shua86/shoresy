using UnityEngine;
using Hockey.Core;
using Hockey.Gameplay;

namespace Hockey.Presentation
{
    /// <summary>
    /// Broadcast-style chase camera: sits behind the play along the up-ice axis (from the Home end) and
    /// elevated, tracking the puck and easing position + aim. Because Home attacks +Z, this makes
    /// "push up = skate up-ice" for the human. Cinemachine + replays replace this in the presentation
    /// milestone.
    /// </summary>
    public class BroadcastCamera : MonoBehaviour
    {
        public float height = 16f;
        public float backDistance = 21f;
        public float xDamp = 0.15f;     // how much the camera drifts sideways with the puck
        public float posLerp = 2.5f;
        public float lookLerp = 3.5f;

        void LateUpdate()
        {
            var mm = MatchManager.Instance;
            if (mm == null) return;

            // Push in on a fight; otherwise chase the puck.
            var fight = mm.ActiveFight;
            bool inFight = mm.Phase == MatchPhase.Fight && fight != null;
            Vector3 focus = inFight ? fight.FocusPoint
                : (mm.Puck != null ? mm.Puck.transform.position : Vector3.zero);
            focus.y = 0f;

            float camHeight = inFight ? 5f : height;
            float back = inFight ? 8f : backDistance;
            float sideDamp = inFight ? 1f : xDamp;
            Vector3 desired = new Vector3(focus.x * sideDamp, camHeight, focus.z - back);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-posLerp * Time.deltaTime));

            Quaternion look = Quaternion.LookRotation((focus + Vector3.up) - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-lookLerp * Time.deltaTime));
        }
    }
}

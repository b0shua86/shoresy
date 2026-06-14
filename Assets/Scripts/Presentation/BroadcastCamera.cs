using UnityEngine;
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
        public float height = 13f;
        public float backDistance = 18f;
        public float xDamp = 0.25f;     // how much the camera drifts sideways with the puck
        public float posLerp = 4f;
        public float lookLerp = 6f;

        void LateUpdate()
        {
            var mm = MatchManager.Instance;
            if (mm == null) return;
            Vector3 focus = mm.Puck != null ? mm.Puck.transform.position : Vector3.zero;
            focus.y = 0f;

            Vector3 desired = new Vector3(focus.x * xDamp, height, focus.z - backDistance);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-posLerp * Time.deltaTime));

            Quaternion look = Quaternion.LookRotation((focus + Vector3.up) - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-lookLerp * Time.deltaTime));
        }
    }
}

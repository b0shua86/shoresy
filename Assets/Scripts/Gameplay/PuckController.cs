using UnityEngine;
using Hockey.Core;

namespace Hockey.Gameplay
{
    /// <summary>
    /// The puck. When free it slides with ice friction; when controlled it tracks the carrier's
    /// stick tip so dangling reads nicely. Shooting/passing releases it with a velocity plus a short
    /// re-capture cooldown so it doesn't instantly stick back to the shooter.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PuckController : MonoBehaviour
    {
        public const float RestHeight = 0.06f;

        public SkaterController Carrier { get; private set; }
        public Rigidbody Body { get; private set; }

        float _recaptureAt;

        public void Configure(PuckTuning tuning)
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = tuning.mass;
            Body.linearDamping = tuning.linearDamping;
            Body.angularDamping = 0.5f;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public bool CanBeCaptured => Carrier == null && Time.time >= _recaptureAt;

        public void SetCarrier(SkaterController skater)
        {
            Carrier = skater;
            if (Body == null) Body = GetComponent<Rigidbody>();
            if (skater != null)
            {
                Body.isKinematic = true;
            }
        }

        /// <summary>Release the puck with a velocity (a shot or a pass).</summary>
        public void Release(Vector3 velocity, float recaptureCooldown = 0.3f)
        {
            Carrier = null;
            if (Body == null) Body = GetComponent<Rigidbody>();
            Body.isKinematic = false;
            Body.linearVelocity = velocity;
            _recaptureAt = Time.time + recaptureCooldown;
        }

        void FixedUpdate()
        {
            if (Carrier == null || Body == null) return;
            Vector3 target = Carrier.StickTip;
            target.y = RestHeight;
            // Lag toward the stick tip a touch so possession feels physical rather than glued.
            Body.MovePosition(Vector3.Lerp(Body.position, target, 0.5f));
        }
    }
}

using UnityEngine;
using Hockey.Core;

namespace Hockey.Gameplay
{
    /// <summary>
    /// Physics-driven arcade skater. A "driver" (human <see cref="PlayerInputDriver"/> or an AI
    /// controller) sets the movement intent each frame and calls the action methods; this class turns
    /// that into skating, puck carrying, shooting, passing and getting knocked off the puck.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SkaterController : MonoBehaviour
    {
        public const float Radius = 0.4f;
        public const float StandHeight = 1.0f; // capsule centre height; bottom rests on ice (y=0)

        [Header("Identity")]
        public TeamSide Team;
        public PlayerRole Role;
        public int JerseyNumber;

        /// <summary>True while the human is driving this skater (AI controllers ignore it then).</summary>
        public bool IsHumanControlled;

        public bool IsGoalie => Role == PlayerRole.Goalie;
        public Rigidbody Body { get; private set; }

        /// <summary>The point just ahead of the skater where the puck sits while carried.</summary>
        public Vector3 StickTip => transform.position + transform.forward * (Radius + 0.55f);

        public bool HasPuck =>
            MatchManager.Instance != null &&
            MatchManager.Instance.Puck != null &&
            MatchManager.Instance.Puck.Carrier == this;

        public Vector3 PlanarVelocity
        {
            get { var v = Body != null ? Body.linearVelocity : Vector3.zero; v.y = 0f; return v; }
        }

        public bool IsStunned => Time.time < _stunnedUntil;

        SkatingTuning _tuning = SkatingTuning.Default;
        float _speedScale = 1f;
        Vector2 _moveInput;
        bool _sprint;
        float _stunnedUntil;

        public void Configure(SkatingTuning tuning, float speedScale = 1f)
        {
            _tuning = tuning;
            _speedScale = speedScale;
            Body = GetComponent<Rigidbody>();
            Body.mass = 80f;
            Body.useGravity = true;
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Body.linearDamping = 0f;
            Body.angularDamping = 0.05f;
        }

        // ---- intent, set by a driver each frame ----
        public void SetMoveInput(Vector2 worldXZ) => _moveInput = Vector2.ClampMagnitude(worldXZ, 1f);
        public void SetSprint(bool s) => _sprint = s;
        public void SetSpeedScale(float s) => _speedScale = s;

        void FixedUpdate()
        {
            if (Body == null) return;
            float dt = Time.fixedDeltaTime;
            Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
            float inputMag = inputDir.magnitude;
            Vector3 planar = PlanarVelocity;

            if (IsStunned)
            {
                Body.AddForce(-planar * _tuning.stopDamping, ForceMode.Acceleration);
                return;
            }

            float topSpeed = _tuning.maxSpeed * _speedScale * (_sprint ? _tuning.sprintMultiplier : 1f);

            if (inputMag > 0.05f)
            {
                Vector3 desiredVel = inputDir.normalized * topSpeed * Mathf.Clamp01(inputMag);
                Body.AddForce((desiredVel - planar) * _tuning.acceleration, ForceMode.Acceleration);

                // Edge grip: bleed off sideways drift relative to facing for crisp carving.
                Vector3 lateral = Vector3.Project(planar, transform.right);
                Body.AddForce(-lateral * _tuning.lateralGrip, ForceMode.Acceleration);

                // Turn to face travel direction.
                Quaternion target = Quaternion.LookRotation(desiredVel.normalized, Vector3.up);
                float turnSpeed = _tuning.turnResponse * 40f; // deg/sec
                Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, target, turnSpeed * dt));
            }
            else
            {
                Body.AddForce(-planar * _tuning.stopDamping, ForceMode.Acceleration);
            }

            // Clamp planar speed (keep vertical component for gravity/landing).
            planar = PlanarVelocity;
            if (planar.magnitude > topSpeed)
            {
                Vector3 c = planar.normalized * topSpeed;
                Body.linearVelocity = new Vector3(c.x, Body.linearVelocity.y, c.z);
            }
        }

        // ---- actions ----
        public void Shoot(Vector3 aimDir)
        {
            if (!HasPuck) return;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;
            MatchManager.Instance.Puck.Release(aimDir.normalized * MatchManager.Instance.Config.puck.shotSpeed);
        }

        public void Pass(SkaterController target)
        {
            if (!HasPuck) return;
            Vector3 dir = target != null ? (target.StickTip - StickTip) : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            MatchManager.Instance.Puck.Release(dir.normalized * MatchManager.Instance.Config.puck.passSpeed);
        }

        /// <summary>Knock this skater off the puck and briefly disable their skating.</summary>
        public void Stun(float seconds)
        {
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
            if (HasPuck) MatchManager.Instance.Puck.Release(PlanarVelocity * 0.5f);
        }

        public const float CheckRange = 1.7f;
        public const float MinCheckSpeed = 4f;

        /// <summary>Throw a body check: if moving fast enough with an opponent just ahead, knock them down.</summary>
        public void TryCheck()
        {
            var mm = MatchManager.Instance;
            if (mm == null || PlanarVelocity.magnitude < MinCheckSpeed) return;
            SkaterController target = null;
            float best = CheckRange;
            foreach (var s in mm.Skaters)
            {
                if (s == null || s == this || s.Team == Team || s.IsGoalie) continue;
                Vector3 to = s.transform.position - transform.position; to.y = 0f;
                float d = to.magnitude;
                if (d > best) continue;
                Vector3 toN = to.sqrMagnitude > 0.001f ? to.normalized : transform.forward;
                if (Vector3.Dot(transform.forward, toN) < 0.4f) continue; // must be in front
                best = d; target = s;
            }
            if (target != null)
            {
                Vector3 dir = target.transform.position - transform.position; dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
                target.ReceiveHit(dir.normalized, PlanarVelocity.magnitude);
            }
        }

        /// <summary>Take a hit: get knocked back and briefly stunned (dropping the puck).</summary>
        public void ReceiveHit(Vector3 dir, float power)
        {
            Stun(0.8f);
            if (Body != null) Body.AddForce(dir * Mathf.Clamp(power, 4f, 12f) * 16f, ForceMode.Impulse);
        }
    }
}

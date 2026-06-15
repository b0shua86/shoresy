using UnityEngine;
using UnityEngine.InputSystem;
using Hockey.Core;

namespace Hockey.Gameplay
{
    /// <summary>
    /// Human input for the currently controlled skater, read directly from keyboard + gamepad via the
    /// Input System (no .inputactions asset required). Movement is camera-relative.
    ///
    /// Keyboard: Move WASD · Sprint Shift · Shoot Space · Pass E · Check F · Switch Tab
    /// Gamepad:  Move LStick · Sprint LStick-press · Shoot A/South · Pass X/West · Check B/East · Switch Y/North
    /// </summary>
    public class PlayerInputDriver : MonoBehaviour
    {
        public SkaterController Controlled;

        void Start()
        {
            if (Controlled != null) Controlled.IsHumanControlled = true;
        }

        void Update()
        {
            var mm = MatchManager.Instance;
            if (Controlled == null || mm == null) return;

            // Auto-switch to whichever teammate just gained possession.
            var carrier = mm.Puck != null ? mm.Puck.Carrier : null;
            if (carrier != null && carrier.Team == Controlled.Team && !carrier.IsGoalie && carrier != Controlled)
                SwitchTo(carrier);

            // Don't accept input between whistles.
            bool inPlay = mm.Phase == MatchPhase.Play || mm.Phase == MatchPhase.Faceoff;

            Vector2 raw = ReadMove();
            Vector3 world = inPlay ? CameraRelative(raw) : Vector3.zero;
            Controlled.SetMoveInput(new Vector2(world.x, world.z));
            Controlled.SetSprint(inPlay && ReadSprint());

            if (!inPlay) return;
            if (ShootPressed()) Controlled.Shoot(AimDir());
            if (PassPressed()) Controlled.Pass(FindPassTarget());
            if (CheckPressed()) Controlled.TryCheck();
            if (DropGlovesPressed())
            {
                var opp = NearestOpponent(2.4f);
                if (opp != null) { mm.StartFight(Controlled, opp); return; }
            }
            if (SwitchPressed()) SwitchTo(NearestToPuck());
        }

        void SwitchTo(SkaterController s)
        {
            if (s == null || s == Controlled) return;
            Controlled.IsHumanControlled = false;
            Controlled.SetMoveInput(Vector2.zero);
            Controlled = s;
            Controlled.IsHumanControlled = true;
        }

        // ---- raw reads ----
        static Vector2 ReadMove()
        {
            Vector2 v = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed) v.y -= 1f;
                if (kb.aKey.isPressed) v.x -= 1f;
                if (kb.dKey.isPressed) v.x += 1f;
            }
            var gp = Gamepad.current;
            if (gp != null && v == Vector2.zero) v = gp.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
        }

        static bool ReadSprint()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;
            var gp = Gamepad.current;
            return gp != null && gp.leftStickButton.isPressed;
        }

        static bool ShootPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonSouth.wasPressedThisFrame;
        }

        static bool PassPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonWest.wasPressedThisFrame;
        }

        static bool CheckPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonEast.wasPressedThisFrame;
        }

        static bool DropGlovesPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.gKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.rightShoulder.wasPressedThisFrame;
        }

        static bool SwitchPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonNorth.wasPressedThisFrame;
        }

        static Vector3 CameraRelative(Vector2 raw)
        {
            var cam = Camera.main;
            if (cam == null) return new Vector3(raw.x, 0f, raw.y);
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            return Vector3.ClampMagnitude(r * raw.x + f * raw.y, 1f);
        }

        Vector3 AimDir()
        {
            var mm = MatchManager.Instance;
            Vector3 goal = mm.Rink.AttackGoalCenter(Controlled.Team);
            Vector3 dir = goal - Controlled.transform.position; dir.y = 0f;
            return dir.sqrMagnitude > 0.01f ? dir.normalized : Controlled.transform.forward;
        }

        SkaterController FindPassTarget()
        {
            var mm = MatchManager.Instance;
            SkaterController best = null;
            float bestScore = float.NegativeInfinity;
            Vector3 fwd = Controlled.transform.forward;
            foreach (var s in mm.Skaters)
            {
                if (s == null || s == Controlled || s.Team != Controlled.Team || s.IsGoalie) continue;
                Vector3 to = s.transform.position - Controlled.transform.position; to.y = 0f;
                float dist = to.magnitude;
                if (dist < 1f || dist > 30f) continue;
                float forwardness = Vector3.Dot(fwd, to.normalized);
                float score = forwardness * 2f - dist * 0.05f;
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        SkaterController NearestToPuck()
        {
            var mm = MatchManager.Instance;
            if (mm.Puck == null) return null;
            SkaterController best = null; float bestD = float.MaxValue;
            Vector3 pp = mm.Puck.transform.position;
            foreach (var s in mm.Skaters)
            {
                if (s == null || s.Team != Controlled.Team || s.IsGoalie) continue;
                float d = Vector3.Distance(s.transform.position, pp);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        SkaterController NearestOpponent(float maxDist)
        {
            var mm = MatchManager.Instance;
            SkaterController best = null; float bestD = maxDist;
            foreach (var s in mm.Skaters)
            {
                if (s == null || s.Team == Controlled.Team || s.IsGoalie) continue;
                float d = Vector3.Distance(s.transform.position, Controlled.transform.position);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Hockey.Gameplay
{
    /// <summary>
    /// Self-contained drop-the-gloves brawl between two skaters, run during <c>MatchPhase.Fight</c>.
    /// Each side is driven by the human (when IsHumanControlled) or a small AI. Jab is fast and cheap,
    /// Haymaker is telegraphed but heavy, Block soaks damage, Dodge grants brief i-frames — all gated
    /// by stamina. First to drop the other's health to zero wins; <see cref="MatchManager"/> handles
    /// the aftermath via the supplied callback.
    ///
    /// Human controls — Jab: Space/South · Haymaker: F/East · Block: hold Shift/LT · Dodge: Tab/West.
    /// </summary>
    public class FightController : MonoBehaviour
    {
        class Side
        {
            public SkaterController sk;
            public float hp = 1f, st = 1f;
            public bool blocking;
            public float dodgeUntil, cdUntil, heavyAt, aiNext;
            public bool heavyPending;
            public bool Human => sk != null && sk.IsHumanControlled;
            public string Name => sk != null ? $"{sk.Team} #{sk.JerseyNumber}" : "?";
        }

        const float JabDmg = 0.07f, JabStam = 0.06f, JabCd = 0.40f;
        const float HeavyDmg = 0.22f, HeavyStam = 0.22f, HeavyCd = 0.95f, HeavyWindup = 0.40f;
        const float DodgeStam = 0.14f, DodgeCd = 0.65f, DodgeIFrames = 0.30f;
        const float BlockChip = 0.04f, BlockMult = 0.2f;
        const float StamRegen = 0.22f, BlockRegen = 0.10f;

        Side _a, _b;
        System.Action<SkaterController, SkaterController> _onDone;
        bool _done;

        public string Message { get; private set; } = "DROP THE GLOVES!";
        public float HpA => _a != null ? _a.hp : 0f;
        public float HpB => _b != null ? _b.hp : 0f;
        public float StA => _a != null ? _a.st : 0f;
        public float StB => _b != null ? _b.st : 0f;
        public string NameA => _a != null ? _a.Name : "";
        public string NameB => _b != null ? _b.Name : "";

        /// <summary>Midpoint of the two fighters, for the camera to push in on.</summary>
        public Vector3 FocusPoint
        {
            get
            {
                Vector3 a = _a != null && _a.sk != null ? _a.sk.transform.position : Vector3.zero;
                Vector3 b = _b != null && _b.sk != null ? _b.sk.transform.position : Vector3.zero;
                Vector3 m = (a + b) * 0.5f; m.y = 0f; return m;
            }
        }

        public void Begin(SkaterController a, SkaterController b, System.Action<SkaterController, SkaterController> onDone)
        {
            _a = new Side { sk = a };
            _b = new Side { sk = b };
            _onDone = onDone;
            FaceToward(a, b);
            FaceToward(b, a);
        }

        static void FaceToward(SkaterController s, SkaterController other)
        {
            if (s == null || other == null) return;
            Vector3 d = other.transform.position - s.transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) s.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        void Update()
        {
            if (_done || _a == null || _b == null) return;
            float dt = Time.deltaTime;
            Regen(_a, dt); Regen(_b, dt);
            Drive(_a, _b); Drive(_b, _a);
            ResolvePending(_a, _b); ResolvePending(_b, _a);
        }

        static void Regen(Side s, float dt)
        {
            s.st = Mathf.Clamp01(s.st + (s.blocking ? BlockRegen : StamRegen) * dt);
        }

        void Drive(Side s, Side opp)
        {
            if (s.Human) DriveHuman(s, opp);
            else DriveAI(s, opp);
        }

        void DriveHuman(Side s, Side opp)
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool block = (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
                         || (gp != null && gp.leftTrigger.ReadValue() > 0.5f);
            s.blocking = block && s.st > 0.02f;

            if (Pressed(kb?.spaceKey, gp?.buttonSouth)) Jab(s, opp);
            if (Pressed(kb?.fKey, gp?.buttonEast)) Heavy(s);
            if (Pressed(kb?.tabKey, gp?.buttonWest)) Dodge(s);
        }

        static bool Pressed(ButtonControl k, ButtonControl g)
            => (k != null && k.wasPressedThisFrame) || (g != null && g.wasPressedThisFrame);

        void DriveAI(Side s, Side opp)
        {
            if (Time.time < s.aiNext) return;
            var mm = MatchManager.Instance;
            float skill = mm != null ? Mathf.Clamp01(0.25f + 0.2f * (int)mm.Difficulty) : 0.5f;
            s.aiNext = Time.time + Mathf.Lerp(0.34f, 0.14f, skill);

            // React to a telegraphed haymaker.
            if (opp.heavyPending && Random.value < 0.4f + skill * 0.4f)
            {
                if (Random.value < 0.5f) Dodge(s); else s.blocking = true;
                return;
            }
            s.blocking = false;

            if (s.st < 0.2f) { s.blocking = true; return; }                       // turtle to recover
            if (s.hp < 0.35f && Random.value < 0.5f) { s.blocking = true; return; }

            if (Random.value < 0.18f + skill * 0.2f && !opp.blocking) Heavy(s);
            else Jab(s, opp);
        }

        void Jab(Side att, Side def)
        {
            if (Time.time < att.cdUntil || att.heavyPending || att.st < JabStam) return;
            att.st -= JabStam; att.cdUntil = Time.time + JabCd; att.blocking = false;
            Land(att, def, JabDmg, "jab");
        }

        void Heavy(Side att)
        {
            if (Time.time < att.cdUntil || att.heavyPending || att.st < HeavyStam) return;
            att.st -= HeavyStam; att.cdUntil = Time.time + HeavyCd; att.blocking = false;
            att.heavyPending = true; att.heavyAt = Time.time + HeavyWindup;
            Message = $"{att.Name} winds up…";
        }

        void Dodge(Side s)
        {
            if (Time.time < s.cdUntil || s.st < DodgeStam) return;
            s.st -= DodgeStam; s.cdUntil = Time.time + DodgeCd; s.dodgeUntil = Time.time + DodgeIFrames;
            s.blocking = false;
        }

        void ResolvePending(Side att, Side def)
        {
            if (!att.heavyPending || Time.time < att.heavyAt) return;
            att.heavyPending = false;
            Land(att, def, HeavyDmg, "HAYMAKER");
        }

        void Land(Side att, Side def, float dmg, string what)
        {
            if (_done) return;
            if (Time.time < def.dodgeUntil) { Message = $"{def.Name} slips the {what}!"; return; }
            float mult = 1f;
            if (def.blocking) { mult = BlockMult; def.st = Mathf.Max(0f, def.st - BlockChip); Message = $"{def.Name} blocks the {what}"; }
            else Message = $"{att.Name} lands a {what}!";
            def.hp -= dmg * mult;
            if (def.hp <= 0f) { def.hp = 0f; Finish(att, def); }
        }

        void Finish(Side winner, Side loser)
        {
            if (_done) return;
            _done = true;
            Message = $"{winner.Name} wins the scrap!";
            _onDone?.Invoke(winner.sk, loser.sk);
        }
    }
}

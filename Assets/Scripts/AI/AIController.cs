using UnityEngine;
using Hockey.Core;
using Hockey.Gameplay;

namespace Hockey.AI
{
    /// <summary>
    /// Per-skater AI. Holds team structure via a key rule — only the closest teammate to the puck
    /// pursues it; everyone else moves to a role anchor — so the team supports the play instead of
    /// swarming. Skill comes from an <see cref="AISkillProfile"/>: teammates are floored competent so
    /// the player's line is genuinely good, while opponents scale with the selected difficulty, which
    /// can be changed live (this controller re-reads it automatically).
    /// </summary>
    [RequireComponent(typeof(SkaterController))]
    public class AIController : MonoBehaviour
    {
        public AISkillProfile Skill = AISkillProfile.ForOpponent(Difficulty.Pro);

        SkaterController _self;
        Difficulty _lastDiff = (Difficulty)(-1);
        float _nextDecision;
        Vector3 _moveTarget;
        bool _wantShoot, _wantPass;
        SkaterController _passTo;

        void Awake() { _self = GetComponent<SkaterController>(); }

        void Update()
        {
            if (_self.IsHumanControlled) return;
            var mm = MatchManager.Instance;
            if (mm == null || mm.Puck == null) return;

            // Live difficulty: recompute skill if it changed (e.g. from the pause menu).
            if (mm.Difficulty != _lastDiff)
            {
                _lastDiff = mm.Difficulty;
                Skill = _self.Team == TeamSide.Away
                    ? AISkillProfile.ForOpponent(_lastDiff)
                    : AISkillProfile.ForTeammate(_lastDiff);
                _self.SetSpeedScale(Skill.speedScale);
            }

            if (mm.Phase != MatchPhase.Play) { _self.SetMoveInput(Vector2.zero); return; }

            if (Time.time >= _nextDecision)
            {
                Decide(mm);
                _nextDecision = Time.time + Mathf.Lerp(0.28f, 0.06f, Skill.reaction);
            }

            // Steer toward the chosen target.
            Vector3 to = _moveTarget - transform.position; to.y = 0f;
            Vector2 input = to.sqrMagnitude > 0.09f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
            _self.SetMoveInput(input);
            _self.SetSprint(to.magnitude > 6f);

            if (_self.HasPuck)
            {
                if (_wantShoot)
                {
                    _self.Shoot(AimWithError(mm.Rink.AttackGoalCenter(_self.Team) - _self.transform.position, Skill.shotAccuracy));
                    _wantShoot = false;
                }
                else if (_wantPass && _passTo != null)
                {
                    _self.Pass(_passTo);
                    _wantPass = false; _passTo = null;
                }
            }
        }

        void Decide(MatchManager mm)
        {
            _wantShoot = _wantPass = false; _passTo = null;
            Vector3 puckPos = mm.Puck.transform.position;

            if (_self.IsGoalie) { _moveTarget = GoaliePos(mm); return; }

            var carrier = mm.Puck.Carrier;
            bool iCarry = carrier == _self;
            bool weHavePuck = carrier != null && carrier.Team == _self.Team;

            if (iCarry)
            {
                Vector3 goal = mm.Rink.AttackGoalCenter(_self.Team);
                float distToGoal = Vector3.Distance(_self.transform.position, goal);
                float shotRange = Mathf.Lerp(8f, 16f, Skill.shotAccuracy);
                if (distToGoal < shotRange) { _wantShoot = true; _moveTarget = goal; return; }

                var open = BestPassOption(mm, goal);
                if (open != null && Random.value < Skill.passAccuracy * 0.4f) { _wantPass = true; _passTo = open; }
                _moveTarget = goal; // drive the net
                return;
            }

            if (weHavePuck)
            {
                _moveTarget = FormationUtil.RoleAnchor(_self.Team, _self.Role, mm.Rink, attacking: true);
                return;
            }

            // Opponent has the puck, or it's loose: closest teammate forechecks, others hold structure.
            _moveTarget = IsClosestTeammateToPuck(mm, puckPos)
                ? puckPos
                : FormationUtil.RoleAnchor(_self.Team, _self.Role, mm.Rink, attacking: false);
        }

        bool IsClosestTeammateToPuck(MatchManager mm, Vector3 puckPos)
        {
            float myD = Vector3.Distance(transform.position, puckPos);
            foreach (var s in mm.Skaters)
            {
                if (s == null || s == _self || s.Team != _self.Team || s.IsGoalie) continue;
                if (Vector3.Distance(s.transform.position, puckPos) < myD - 0.5f) return false;
            }
            return true;
        }

        SkaterController BestPassOption(MatchManager mm, Vector3 goal)
        {
            SkaterController best = null;
            float bestScore = 0.2f;
            float myGoalDist = Vector3.Distance(_self.transform.position, goal);
            foreach (var s in mm.Skaters)
            {
                if (s == null || s == _self || s.Team != _self.Team || s.IsGoalie) continue;
                float dist = Vector3.Distance(_self.transform.position, s.transform.position);
                if (dist > 25f || dist < 3f) continue;
                float advantage = myGoalDist - Vector3.Distance(s.transform.position, goal); // teammate closer to goal
                float score = advantage * 0.1f;
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        Vector3 GoaliePos(MatchManager mm)
        {
            Vector3 ownGoal = mm.Rink.OwnGoalCenter(_self.Team);
            Vector3 dir = mm.Puck.transform.position - ownGoal; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward * _self.Team.AttackDirZ();
            dir.Normalize();
            float depth = Mathf.Lerp(1.0f, 2.4f, Skill.goalieSkill);
            Vector3 pos = ownGoal + dir * depth;
            pos.x = Mathf.Clamp(pos.x, ownGoal.x - 2.2f, ownGoal.x + 2.2f);
            return pos;
        }

        Vector3 AimWithError(Vector3 idealDir, float accuracy)
        {
            idealDir.y = 0f;
            float maxErrDeg = Mathf.Lerp(18f, 1.5f, accuracy);
            return Quaternion.Euler(0f, Random.Range(-maxErrDeg, maxErrDeg), 0f) * idealDir.normalized;
        }
    }
}

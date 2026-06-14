using UnityEngine;
using Hockey.Core;

namespace Hockey.Gameplay
{
    /// <summary>
    /// Positions skaters for faceoffs and provides role "anchor" positions that the AI uses to spread
    /// out and hold sensible structure instead of all chasing the puck.
    /// </summary>
    public static class FormationUtil
    {
        public static void PlaceForFaceoff(MatchManager mm)
        {
            foreach (var s in mm.Skaters)
            {
                if (s == null) continue;
                Vector3 pos = RoleAnchor(s.Team, s.Role, mm.Rink, attacking: false);
                pos.y = SkaterController.StandHeight;
                s.transform.position = pos;
                if (s.Body != null)
                {
                    s.Body.linearVelocity = Vector3.zero;
                    s.Body.angularVelocity = Vector3.zero;
                }
                Vector3 look = mm.Rink.CenterFaceoff - pos; look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    s.transform.rotation = Quaternion.LookRotation(look, Vector3.up);
            }
        }

        /// <summary>A reasonable position for a role. Defensive structure by default; push up when attacking.</summary>
        public static Vector3 RoleAnchor(TeamSide team, PlayerRole role, RinkLayout rink, bool attacking)
        {
            float dir = team.AttackDirZ();                  // +1 = Home attacks +Z
            float halfW = rink.width * 0.5f - 2f;
            float ownGoalZ = rink.OwnGoalCenter(team).z;     // negative for Home

            if (role == PlayerRole.Goalie)
                return rink.OwnGoalCenter(team) + new Vector3(0f, 0f, dir * 1.5f);

            float depth;
            switch (role)
            {
                case PlayerRole.LeftDefense:
                case PlayerRole.RightDefense: depth = 0.22f; break;
                case PlayerRole.Center: depth = 0.40f; break;
                default: depth = 0.34f; break; // wings
            }
            if (attacking) depth += 0.20f;
            float z = ownGoalZ + dir * rink.length * depth;

            float x;
            switch (role)
            {
                case PlayerRole.LeftWing:
                case PlayerRole.LeftDefense: x = -halfW * 0.55f; break;
                case PlayerRole.RightWing:
                case PlayerRole.RightDefense: x = halfW * 0.55f; break;
                default: x = 0f; break;
            }
            return new Vector3(x, 0f, z);
        }

        /// <summary>Assigns roles to a list of skaters for a given side based on team size.</summary>
        public static PlayerRole RoleForIndex(int index, int playersPerSide)
        {
            // index 0..playersPerSide-1 (skaters only; goalie handled separately)
            if (playersPerSide <= 1) return PlayerRole.Center;
            switch (index)
            {
                case 0: return PlayerRole.Center;
                case 1: return PlayerRole.LeftWing;
                case 2: return PlayerRole.RightWing;
                case 3: return PlayerRole.LeftDefense;
                default: return PlayerRole.RightDefense;
            }
        }
    }
}

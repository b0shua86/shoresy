using Hockey.Core;

namespace Hockey.Presentation
{
    /// <summary>
    /// Maps a spawned skater (team + spawn index, or goalie) to a generated character model under
    /// Resources/Characters. Spawn index 0 is the centre — i.e. the human's default skater — so the
    /// player controls the home star. Cycles if a team has fewer unique models than players on the ice.
    /// </summary>
    public static class CharacterRoster
    {
        static readonly string[] Home =
        {
            "skater_home_star",     // index 0 = centre = human-controlled
            "skater_home_captain",
            "skater_home_winger",
            "skater_home_defender",
            "skater_home_grinder",
        };

        static readonly string[] Away =
        {
            "skater_away_captain",
            "skater_away_winger",
            "skater_away_enforcer",
            "skater_away_grinder",
        };

        public static string Resolve(TeamSide side, PlayerRole role, int index)
        {
            if (role == PlayerRole.Goalie)
                return side == TeamSide.Home ? "goalie_home" : "goalie_away";

            var pool = side == TeamSide.Home ? Home : Away;
            if (index < 0) index = 0;
            return pool[index % pool.Length];
        }
    }
}

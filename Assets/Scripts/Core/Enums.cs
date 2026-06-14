namespace Hockey.Core
{
    /// <summary>Which side a team belongs to. Home defends -Z and attacks +Z (see <see cref="TeamSideExtensions"/>).</summary>
    public enum TeamSide
    {
        Home = 0,
        Away = 1
    }

    /// <summary>On-ice role. Drives AI positioning, faceoff alignment, and line construction.</summary>
    public enum PlayerRole
    {
        Center,
        LeftWing,
        RightWing,
        LeftDefense,
        RightDefense,
        Goalie
    }

    /// <summary>Selectable difficulty. Maps to an <see cref="AISkillProfile"/> for the opponent.</summary>
    public enum Difficulty
    {
        Rookie,
        Pro,
        AllStar,
        Legend
    }

    /// <summary>High-level match phases driven by the match state machine.</summary>
    public enum MatchPhase
    {
        PreGame,
        Faceoff,
        Play,
        Stoppage,
        Intermission,
        Fight,
        GameOver
    }

    public static class TeamSideExtensions
    {
        public static TeamSide Opponent(this TeamSide side)
            => side == TeamSide.Home ? TeamSide.Away : TeamSide.Home;

        /// <summary>Attacking direction along the rink long (Z) axis: +1 for Home, -1 for Away.</summary>
        public static float AttackDirZ(this TeamSide side)
            => side == TeamSide.Home ? 1f : -1f;
    }
}

namespace HeinzBOTtle.Leaderboards;

/// <summary>
/// Represents an individual leaderboard ranking. This class does not provide information about the player associated with the ranking.
/// </summary>
public class LBRanking {

    /// <summary>The 1-indexed position held on the leaderboard represented by this ranking.</summary>
    public int Position { get; }
    /// <summary>The clean name of the Hypixel minigame represented by this ranking.</summary>
    public string GameTitle { get; }
    /// <summary>The clean name of the minigame's statistic represented by this ranking.</summary>
    public string GameStat { get; }

    public LBRanking(int position, string gameTitle, string gameStat) {
        Position = position;
        GameTitle = gameTitle;
        GameStat = gameStat;
    }

    /// <returns>A phrase summarizing this ranking.</returns>
    public override string ToString() {
        return $"`#{LBMethods.FormatPosition(Position)}` in {GameTitle}" + (GameStat.Equals("") ? "" : $" ({GameStat})");
    }

    public static bool operator <(LBRanking a, LBRanking b) => a.Position < b.Position;
    public static bool operator >(LBRanking a, LBRanking b) => a.Position > b.Position;
    public static bool operator <=(LBRanking a, LBRanking b) => a.Position <= b.Position;
    public static bool operator >=(LBRanking a, LBRanking b) => a.Position >= b.Position;

}

namespace HeinzBOTtle.Leaderboards;

/// <summary>
/// Provides information about all of a player's leaderboard rankings at a given time.
/// </summary>
public class LBRankingData {

    /// <summary>The clean username of the player holding these rankings.</summary>
    public string ProperUsername { get; }
    /// <summary>The list of the player's rankings.</summary>
    public List<LBRanking> Rankings { get; }

    public LBRankingData(Json player) {
        ProperUsername = player.GetString("player.displayname") ?? "?????";
        Rankings = new List<LBRanking>();
    }

    public LBRankingData(string properUsername) {
        ProperUsername = properUsername;
        Rankings = new List<LBRanking>();
    }

}

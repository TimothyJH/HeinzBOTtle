using Discord;

namespace HeinzBOTtle.Leaderboards;

/// <summary>
/// Represents a leaderboard scored with respect to the sum of multiple nodes' values.
/// </summary>
public class AdditiveLeaderboard : Leaderboard {

    /// <summary>The array of dot-delimited JSON nodes from a Hypixel API player response associated with the score for this leaderboard.</summary>
    public string[] Nodes { get; }

    public AdditiveLeaderboard(string gameTitle, string gameStat, Color color, string[] nodes) : base(gameTitle, gameStat, color, false) {
        Nodes = nodes;
    }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        int sum = 0;
        foreach (string node in Nodes)
            sum += (int)(player.GetDouble(node) ?? 0.0);
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", sum);
    }

}

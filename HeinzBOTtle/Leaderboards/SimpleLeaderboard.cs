using Discord;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards;

/// <summary>
/// Represents a leaderboard scored with respect to the value of a single node.
/// </summary>
public class SimpleLeaderboard : Leaderboard {

    /// <summary>The dot-delimited JSON node from a Hypixel API player response associated with the score for this leaderboard.</summary>
    public string Node { get; }

    public SimpleLeaderboard(string gameTitle, string gameStat, Color color, string node) : base(gameTitle, gameStat, color, false) {
        Node = node;
    }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        if (player.GetValueKind(Node) != JsonValueKind.Number)
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", (int)(player.GetDouble(Node) ?? 0.0));
    }

}

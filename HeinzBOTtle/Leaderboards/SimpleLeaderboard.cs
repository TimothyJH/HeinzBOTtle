using Discord;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards;

public class SimpleLeaderboard : Leaderboard {

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

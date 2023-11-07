using Discord;

namespace HeinzBOTtle.Leaderboards;

public class AdditiveLeaderboard : Leaderboard {

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

using Discord;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards.Special;

public class WoolGamesCombinedWinsLeaderboard : Leaderboard {

    public WoolGamesCombinedWinsLeaderboard() : base("Wool Games", "Combined Wins", Color.Teal, false) { }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        int wins = 0;
        if (player.GetValueKind("player.stats.WoolGames.wool_wars.stats.wins") == JsonValueKind.Number)
            wins += (int)(player.GetDouble("player.stats.WoolGames.wool_wars.stats.wins") ?? 0.0);
        if (player.GetValueKind("player.achievements.woolgames_sheep_wars_winner") == JsonValueKind.Number)
            wins += (int)(player.GetDouble("player.achievements.woolgames_sheep_wars_winner") ?? 0.0);
        if (player.GetValueKind("player.stats.WoolGames.capture_the_wool.stats.participated_wins") == JsonValueKind.Number)
            wins += (int)(player.GetDouble("player.stats.WoolGames.capture_the_wool.stats.participated_wins") ?? 0.0);
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", wins); 
    }

}

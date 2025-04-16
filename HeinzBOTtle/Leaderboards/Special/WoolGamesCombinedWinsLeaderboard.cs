using Discord;
using HeinzBOTtle.Hypixel;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards.Special;

public class WoolGamesCombinedWinsLeaderboard : Leaderboard {

    public WoolGamesCombinedWinsLeaderboard() : base("Wool Games", "Combined Wins", Color.Teal, false) { }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", HypixelMethods.GetTotalWoolGamesWins(player)); 
    }

}

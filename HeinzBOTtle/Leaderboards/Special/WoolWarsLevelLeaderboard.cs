using Discord;
using HeinzBOTtle.Hypixel;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards.Special;

public class WoolWarsLevelLeaderboard : Leaderboard {

    public WoolWarsLevelLeaderboard() : base("Wool Wars", "Level", Color.Teal, false) { }

    public override string GenerateDisplayStat(PlayerEntry entry) {
        double level = HypixelMethods.GetWoolWarsLevelFromXP(entry.Stat);
        level = double.Round(level, 2, MidpointRounding.ToZero);
        return HypixelMethods.PadDecimalPlaces(HypixelMethods.WithCommas(level));
    }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        if (player.GetValueKind("player.stats.WoolGames.progression.experience") != JsonValueKind.Number)
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);

        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", (int)(player.GetDouble("player.stats.WoolGames.progression.experience") ?? 0.0));
    }

}

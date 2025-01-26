using Discord;
using HeinzBOTtle.Hypixel;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards.Special;

public class SkyWarsLevelLeaderboard : Leaderboard {

    public SkyWarsLevelLeaderboard() : base("SkyWars", "Level", Color.Teal, false) { }

    public override string GenerateDisplayStat(PlayerEntry entry) {
        double level = HypixelMethods.GetSkyWarsLevelFromXP(entry.Stat);
        level = double.Round(level, 2, MidpointRounding.ToZero);
        return HypixelMethods.PadDecimalPlaces(HypixelMethods.WithCommas(level));
    }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        if (player.GetValueKind("player.stats.SkyWars.skywars_experience") != JsonValueKind.Number)
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", (int)(player.GetDouble("player.stats.SkyWars.skywars_experience") ?? 0.0));
    }

}

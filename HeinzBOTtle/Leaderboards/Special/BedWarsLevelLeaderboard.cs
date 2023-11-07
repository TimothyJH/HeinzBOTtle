using Discord;
using HeinzBOTtle.Hypixel;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards.Special;

public class BedWarsLevelLeaderboard : Leaderboard {

    public BedWarsLevelLeaderboard() : base("Bed Wars", "Level", Color.Teal, false) { }

    public override string GenerateDisplayStat(PlayerEntry entry) {
        double level = HypixelMethods.GetBedWarsLevelFromXP(entry.Stat);
        level = double.Round(level, 2, MidpointRounding.ToZero);
        return HypixelMethods.PadDecimalPlaces(HypixelMethods.WithCommas(level));
    }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        if (player.GetValueKind("player.stats.Bedwars.Experience") != JsonValueKind.Number)
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);

        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", (int)(player.GetDouble("player.stats.Bedwars.Experience") ?? 0.0));
    }

}

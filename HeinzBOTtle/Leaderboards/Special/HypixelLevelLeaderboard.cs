using Discord;
using HeinzBOTtle.Hypixel;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards.Special;

public class HypixelLevelLeaderboard : Leaderboard {

    public HypixelLevelLeaderboard() : base("Hypixel Level", "", Color.DarkPurple, false) { }

    public override string GenerateDisplayStat(PlayerEntry entry) {
        double level = HypixelMethods.GetNetworkLevelFromXP(entry.Stat);
        level = double.Round(level, 2, MidpointRounding.ToZero);
        return HypixelMethods.PadDecimalPlaces(HypixelMethods.WithCommas(level));
    }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        if (player.GetValueKind("player.networkExp") != JsonValueKind.Number)
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", (int)(player.GetDouble("player.networkExp") ?? 0.0));
    }

}

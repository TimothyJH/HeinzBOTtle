﻿using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Requirements;

namespace HeinzBOTtle.Ranks;

public static class RankMethods {

    /// <param name="title">The rank title as found on an guild API response</param>
    /// <returns>The corresponding rank if it exists and is not a staff rank, otherwise null.</returns>
    public static Rank? StringToRank(string title) {
        switch (title) {
            case "Member":
                return Rank.Member;
            case "Scout":
                return Rank.Scout;
            case "Lieutenant":
                return Rank.Lieutenant;
            case "Veteran":
                return Rank.Veteran;
            default:
                return null;
        }
    }

    /// <param name="timestampMilliseconds">The UNIX timestamp (in milliseconds) representing when the player joined the guild</param>
    /// <param name="now">The UTC timestamp representing now</param>
    /// <returns>The amount of time in days between the provided timestamp and now if the provided timestamp is in the past, otherwise -1.</returns>
    public static double DaysSinceTimestamp(long timestampMilliseconds, DateTimeOffset now) {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds);
        if (timestamp > now)
            return -1;
        return (now - timestamp).TotalDays;
    }

    /// <summary></summary>
    /// <param name="player">The JSON representing the player</param>
    /// <param name="daysInGuild">The amount of days spent in the guild by the player</param>
    /// <returns>The highest guild non-staff rank for which the provided player qualifies.</returns>
    public static Rank FindBestEligibleRank(Json player, int daysInGuild) {
        if (daysInGuild >= 365 && (int)HypixelMethods.GetNetworkLevelFromXP(player.GetDouble("player.networkExp") ?? 0.0) >= 200 && 
                (player.GetDouble("player.achievementPoints") ?? 0.0) >= 8000 && ReqMethods.GetRequirementsMet(player).Count >= 3)
            return Rank.Veteran;
        else if (daysInGuild >= 250 && (int)HypixelMethods.GetNetworkLevelFromXP(player.GetDouble("player.networkExp") ?? 0.0) >= 100)
            return Rank.Lieutenant;
        else if (daysInGuild >= 90)
            return Rank.Scout;
        else
            return Rank.Member;
    }

    /// <summary>This method ignores staff ranks but does not assume that the player meets the other non-time-based requirements.
    /// If the returned amount of days is zero or negative, the player may be promoted to the returned rank.</summary>
    /// <param name="player">The JSON representing the player</param>
    /// <param name="daysInGuild">The amount of days spent in the guild by the player</param>
    /// <param name="currentRank">The current rank of the player in the guild</param>
    /// <returns>A tuple consisting of the amount of days until the player should be promoted and the rank to which the player should be promoted.</returns>
    public static (double, Rank)? CalculateDaysUntilPromotion(Json player, double daysInGuild, Rank currentRank) {
        List<(double, Rank)> distances = new List<(double, Rank)>();
        // Below Scout
        if (currentRank < Rank.Scout)
            distances.Add((90.0 - daysInGuild, Rank.Scout));
        // Below Lieutenant
        if (currentRank < Rank.Lieutenant) {
            if ((int)HypixelMethods.GetNetworkLevelFromXP(player.GetDouble("player.networkExp") ?? 0.0) >= 100)
                distances.Add((250.0 - daysInGuild, Rank.Lieutenant));
        }
        // Below Veteran
        if (currentRank < Rank.Veteran) {
            if ((int)HypixelMethods.GetNetworkLevelFromXP(player.GetDouble("player.networkExp") ?? 0.0) >= 200 &&
                    (player.GetDouble("player.achievementPoints") ?? 0.0) >= 8000 && ReqMethods.GetRequirementsMet(player).Count >= 3)
                distances.Add((365.0 - daysInGuild, Rank.Veteran));
        }
        if (distances.Count == 0)
            return null;
        (double, Rank) bestDistance = distances[0];
        for (int i = 1; i < distances.Count; i++) {
            if (bestDistance.Item1 > 0 && distances[i].Item1 <= 0) { // Best Positive, Current Negative
                bestDistance = distances[i];
            } else if (bestDistance.Item1 <= 0 && distances[i].Item1 <= 0) { // Both Distances Negative 
                if (distances[i].Item2 > bestDistance.Item2)
                    bestDistance = distances[i];
            } else if (bestDistance.Item1 > 0.0 && bestDistance.Item1 > 0.0) { // Both Distances Positive
                if (distances[i].Item1 <= bestDistance.Item1)
                    bestDistance = distances[i];
            } // Best Negative, Current Positive --> No change
        }
        return bestDistance;
    }

}

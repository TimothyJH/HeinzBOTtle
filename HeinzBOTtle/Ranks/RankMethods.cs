using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Requirements;

namespace HeinzBOTtle.Ranks;

public static class RankMethods {

    /// <param name="timestampMilliseconds">The UNIX timestamp (in milliseconds) representing when the player joined the guild</param>
    /// <param name="now">The UTC timestamp representing now</param>
    /// <returns>The amount of time between the provided timestamp and now if the provided timestamp is in the past, otherwise null.</returns>
    public static TimeSpan? TimeSinceTimestamp(long timestampMilliseconds, DateTimeOffset now) {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds);
        if (timestamp > now)
            return null;
        return now - timestamp;
    }

    /// <param name="player">The JSON representing the player to be tested</param>
    /// <param name="timeInGuild">The current time streak of the player being in the guild</param>
    /// <param name="targetRank">The rank to to test for qualification</param>
    /// <param name="previousHighestRank">The highest rank held by the player so far</param>
    /// <returns>A time span less than or equal to zero if the player currently qualifies for the rank, a positive time span representing the time until qualification is met,
    /// or the maximum time span if the the player is missing non-time-based requirements for qualification.</returns>
    public static TimeSpan RankQualification(Json player, TimeSpan timeInGuild, Rank targetRank, Rank previousHighestRank = Rank.None) {
        TimeSpan timeRequirement = TimeSpan.Zero;
        bool nonTimeReqs = false;
        switch (targetRank) {
            case Rank.None:
                return TimeSpan.Zero;
            case Rank.Member:
                return ReqMethods.GetRequirementsMet(player).Count >= 1 && (int)HypixelMethods.GetNetworkLevelFromXP(player.GetDouble("player.networkExp") ?? 0.0) >= 85
                    ? TimeSpan.Zero : TimeSpan.MaxValue;
            case Rank.Scout:
                timeRequirement = TimeSpan.FromDays(90.0);
                if (previousHighestRank >= Rank.Scout)
                    timeRequirement /= 2;
                nonTimeReqs = true;
                break;
            case Rank.Lieutenant:
                timeRequirement = TimeSpan.FromDays(250.0);
                if (previousHighestRank >= Rank.Lieutenant)
                    timeRequirement /= 2;
                nonTimeReqs = (int)HypixelMethods.GetNetworkLevelFromXP(player.GetDouble("player.networkExp") ?? 0.0) >= 100;
                break;
            case Rank.Veteran:
                timeRequirement = TimeSpan.FromDays(365.0);
                if (previousHighestRank >= Rank.Lieutenant)
                    timeRequirement /= 2;
                nonTimeReqs = (int)HypixelMethods.GetNetworkLevelFromXP(player.GetDouble("player.networkExp") ?? 0.0) >= 200 &&
                    (player.GetDouble("player.achievementPoints") ?? 0.0) >= 8000 && ReqMethods.GetRequirementsMet(player).Count >= 3;
                break;
        }
        return nonTimeReqs ? timeRequirement - timeInGuild : TimeSpan.MaxValue;
    }

    /// <summary></summary>
    /// <param name="player">The JSON representing the player</param>
    /// <param name="daysInGuild">The amount of days spent in the guild by the player</param>
    /// <returns>The highest guild non-staff rank for which the provided player qualifies.</returns>
    public static Rank FindBestEligibleRank(Json player, TimeSpan timeInGuild, Rank previousHighestRank = Rank.None) {
        List<Rank> ranks = new List<Rank>() { Rank.Veteran, Rank.Lieutenant, Rank.Scout, Rank.Member };
        foreach (Rank rank in ranks)
            if (RankQualification(player, timeInGuild, rank, previousHighestRank) <= TimeSpan.Zero)
                return rank;
        return Rank.None;
    }

    /// <summary>This method ignores staff ranks but does not assume that the player meets the other non-time-based requirements.
    /// If the returned amount of time is zero or negative, the player may be promoted to the returned rank.</summary>
    /// <param name="player">The JSON representing the player</param>
    /// <param name="timeInGuild">The amount of time spent in the guild by the player</param>
    /// <param name="currentRank">The current rank of the player in the guild</param>
    /// <returns>A tuple consisting of the amount of time until the player should be promoted and the rank to which the player should be promoted.</returns>
    public static (TimeSpan, Rank)? CalculateTimeUntilPromotion(Json player, TimeSpan timeInGuild, Rank currentRank, Rank previousHighestRank = Rank.None) {
        List<(TimeSpan, Rank)> distances = new List<(TimeSpan, Rank)>();
        // Below Scout
        if (currentRank < Rank.Scout) {
            TimeSpan distance = RankQualification(player, timeInGuild, Rank.Scout, previousHighestRank);
            if (distance != TimeSpan.MaxValue)
                distances.Add((distance, Rank.Scout));
        }
        // Below Lieutenant
        if (currentRank < Rank.Lieutenant) {
            TimeSpan distance = RankQualification(player, timeInGuild, Rank.Lieutenant, previousHighestRank);
            if (distance != TimeSpan.MaxValue)
                distances.Add((distance, Rank.Lieutenant));
        }
        // Below Veteran
        if (currentRank < Rank.Veteran) {
            TimeSpan distance = RankQualification(player, timeInGuild, Rank.Veteran, previousHighestRank);
            if (distance != TimeSpan.MaxValue)
                distances.Add((distance, Rank.Veteran));
        }
        if (distances.Count == 0)
            return null;
        (TimeSpan, Rank) bestDistance = distances[0];
        for (int i = 1; i < distances.Count; i++) {
            if (bestDistance.Item1 > TimeSpan.Zero && distances[i].Item1 <= TimeSpan.Zero) { // Best Positive, Current Negative
                bestDistance = distances[i];
            } else if (bestDistance.Item1 <= TimeSpan.Zero && distances[i].Item1 <= TimeSpan.Zero) { // Both Distances Negative 
                if (distances[i].Item2 > bestDistance.Item2)
                    bestDistance = distances[i];
            } else if (bestDistance.Item1 > TimeSpan.Zero && bestDistance.Item1 > TimeSpan.Zero) { // Both Distances Positive
                if (distances[i].Item1 <= bestDistance.Item1)
                    bestDistance = distances[i];
            } // Best Negative, Current Positive --> No change
        }
        return bestDistance;
    }

}

using Discord;
using HeinzBOTtle.Hypixel;

namespace HeinzBOTtle.Leaderboards.Special;

public class AverageLeaderboardPositionLeaderboard : Leaderboard {

    public AverageLeaderboardPositionLeaderboard() : base("Average Leaderboard Position", "", Color.Red, true) { }

    // The stat value is expected to be 100x the average position.
    public override string GenerateDisplayStat(PlayerEntry entry) {
        double averagePosition = entry.Stat / 100.0;
        averagePosition = double.Round(averagePosition, 2);
        return HypixelMethods.PadDecimalPlaces(averagePosition.ToString());
    }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        throw new InvalidOperationException(); // This is to be unused.
    }

    private PlayerEntry? CalculatePlayer(LBRankingData player) {
        int positionsSum = 0;
        foreach (LBRanking ranking in player.Rankings)
            positionsSum += ranking.Position;
        double averagePosition = (double)positionsSum / player.Rankings.Count;
        return new PlayerEntry(player.ProperUsername, (int)(averagePosition * 100));
    }

    public void EnterPlayer(LBRankingData player) {
        PlayerEntry? entry = CalculatePlayer(player);
        if (entry == null)
            return;
        for (int i = board.Count - 1; i >= -1; i--) {
            if (i == -1) {
                board.Insert(0, entry);
                break;
            }
            if (IsReversed ? entry >= board[i] : entry <= board[i]) {
                board.Insert(i + 1, entry);
                break;
            }
        }
    }

}

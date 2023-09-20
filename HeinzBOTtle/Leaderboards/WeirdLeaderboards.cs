using Discord;
using HeinzBOTtle.Requirements;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards {

    class AverageLeaderboardPositionLeaderboard : Leaderboard {

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
                if (IsReversed ? (entry >= board[i]) : (entry <= board[i])) {
                    board.Insert(i + 1, entry);
                    break;
                }
            }
        }

    }

    class BedWarsLevelLeaderboard : Leaderboard {

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

    class GuildQuestChallengesCompletedLeaderboard : Leaderboard {

        public GuildQuestChallengesCompletedLeaderboard() : base("Guild Quest Challenges Completed", "", Color.Red, false) { }

        protected override PlayerEntry? CalculatePlayer(Json player) {
            string uuid = player.GetString("player.uuid") ?? "";
            if (!HBData.QuestParticipationsLeaderboardMap.ContainsKey(uuid))
                return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", HBData.QuestParticipationsLeaderboardMap[uuid]);
        }

    }

    class HeinzReqsLeaderboard : Leaderboard {

        public HeinzReqsLeaderboard() : base("Heinz Requirements Met", "", Color.Red, false) { }

        protected override PlayerEntry? CalculatePlayer(Json player) {
            List<Requirement> met = new List<Requirement>();
            foreach (Requirement req in HBData.RequirementList) {
                if (req.MeetsRequirement(player))
                    met.Add(req);
            }
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", met.Count);
        }

    }

    class HypixelLevelLeaderboard : Leaderboard {

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

    class PitXPLeaderboard {
        // Soon!
    }

    class SkyWarsLevelLeaderboard : Leaderboard {

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

    class WoolWarsLevelLeaderboard : Leaderboard {

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


}

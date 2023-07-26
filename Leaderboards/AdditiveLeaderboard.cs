using Discord;

namespace HeinzBOTtle.Leaderboards {

    class AdditiveLeaderboard : Leaderboard {

        public string Node1 { get; }
        public string Node2 { get; }

        public AdditiveLeaderboard(string gameTitle, string gameStat, Color color, string node1, string node2) : base(gameTitle, gameStat, color, false) {
            Node1 = node1;
            Node2 = node2;
        }

        protected override PlayerEntry? CalculatePlayer(Json player) {
            int stat1 = (int)(player.GetDouble(Node1) ?? 0.0);
            int stat2 = (int)(player.GetDouble(Node2) ?? 0.0);
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", stat1 + stat2);
        }

    }

}

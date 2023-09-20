namespace HeinzBOTtle.Leaderboards {
    
    class LBRankingData {

        public string ProperUsername { get; }
        public List<LBRanking> Rankings { get; }

        public LBRankingData(Json player) {
            ProperUsername = player.GetString("player.displayname") ?? "?????";
            Rankings = new List<LBRanking>();
        }

        public LBRankingData(string properUsername) {
            ProperUsername = properUsername;
            Rankings = new List<LBRanking>();
        }

    }

}

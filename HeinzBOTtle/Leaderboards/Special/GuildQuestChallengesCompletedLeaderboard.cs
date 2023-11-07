using Discord;

namespace HeinzBOTtle.Leaderboards.Special;

public class GuildQuestChallengesCompletedLeaderboard : Leaderboard {

    public GuildQuestChallengesCompletedLeaderboard() : base("Guild Quest Challenges Completed", "", Color.Red, false) { }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        string uuid = player.GetString("player.uuid") ?? "";
        if (!HBData.QuestParticipationsLeaderboardMap.ContainsKey(uuid))
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", HBData.QuestParticipationsLeaderboardMap[uuid]);
    }

}

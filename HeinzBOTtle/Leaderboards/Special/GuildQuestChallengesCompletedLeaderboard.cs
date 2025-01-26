using Discord;

namespace HeinzBOTtle.Leaderboards.Special;

public class GuildQuestChallengesCompletedLeaderboard : Leaderboard {

    public Dictionary<string, int>? QuestParticipationsMap { get; set; } = null;

    public GuildQuestChallengesCompletedLeaderboard() : base("Guild Quest Challenges Completed", "", Color.Red, false) { }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        string uuid = player.GetString("player.uuid") ?? "";
        if (!QuestParticipationsMap!.TryGetValue(uuid, out int value))
            return new PlayerEntry(player.GetString("player.displayname") ?? "?????", 0);
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", value);
    }

}

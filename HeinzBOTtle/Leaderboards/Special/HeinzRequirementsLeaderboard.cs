using Discord;
using HeinzBOTtle.Requirements;

namespace HeinzBOTtle.Leaderboards.Special;

public class HeinzRequirementsLeaderboard : Leaderboard {

    public HeinzRequirementsLeaderboard() : base("Heinz Requirements Met", "", Color.Red, false) { }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        List<Requirement> met = new List<Requirement>();
        foreach (Requirement req in HBData.RequirementList) {
            if (req.MeetsRequirement(player))
                met.Add(req);
        }
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", met.Count);
    }

}

using Discord;
using HeinzBOTtle.Requirements;
using HeinzBOTtle.Statics;

namespace HeinzBOTtle.Leaderboards.Special;

public class HeinzRequirementsLeaderboard : Leaderboard {

    public HeinzRequirementsLeaderboard() : base("Heinz Requirements Met", "", Color.Red, false) { }

    protected override PlayerEntry? CalculatePlayer(Json player) {
        List<Requirement> met = new List<Requirement>();
        foreach (Requirement req in HBAssets.RequirementList) {
            if (req.MeetsRequirement(player))
                met.Add(req);
        }
        return new PlayerEntry(player.GetString("player.displayname") ?? "?????", met.Count);
    }

}

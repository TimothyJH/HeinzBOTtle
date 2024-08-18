using Discord.WebSocket;

namespace HeinzBOTtle.Requirements;

public static class ReqMethods {

    /// <param name="playerJson">The JSON representing the player to evaluate.</param>
    /// <returns>A list of all guild game requirement met by the player represented by the provided player JSON.</returns>
    public static List<Requirement> GetRequirementsMet(Json playerJson) {
        List<Requirement> met = new List<Requirement>();
        foreach (Requirement req in HBData.RequirementList) {
            if (req.MeetsRequirement(playerJson))
                met.Add(req);
        }
        return met;
    }

    /// <param name="list">The list of requirements to format</param>
    /// <returns>A formatted string of the requirements' respective game titles separated by commas.</returns>
    public static string FormatRequirementsList(List<Requirement> list) {
        if (list.Count == 0)
            return "";
        string formatted = "";
        foreach (Requirement req in list)
            formatted += req.GameTitle + ", ";
        return formatted[..^2];
    }

}

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

    public static void RefreshRequirementRoles() {
        HBData.RequirementRoleMap.Clear();
        SocketGuild guild = HBData.DiscordClient.GetGuild(HBData.DiscordGuildID);
        List<SocketRole> roles = new List<SocketRole>();
        foreach (SocketRole role in guild.Roles)
            roles.Add(role);
        bool safe = true;
        foreach (Requirement req in HBData.RequirementList) {
            SocketRole? role = roles.Find(x => req.Title == x.Name);
            if (role == null) {
                safe = false;
                HBData.Log.Info($"FATAL: Unable to find role match for requirement with title \"{req.Title}\"!");
            } else
                HBData.RequirementRoleMap[req] = role.Id;
        }
        if (!safe)
            throw new InvalidDataException("The requirements could not be mapped to roles successfully.");
    }

}

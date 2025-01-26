namespace HeinzBOTtle.Statics;

/// <summary>
/// The singleton class that provides configuration values set in "config.json" to <see cref="HBConfig"/>.
/// </summary>
public class HBConfigSingleton {

    public bool IsCorrect { get; } = false;
    public string HypixelKey { get; } = "";
    public string DiscordToken { get; } = "";
    public string HypixelGuildID { get; } = "";
    public ulong DiscordGuildID { get; } = 0;
    public ulong LeaderboardsChannelID { get; } = 0;
    public ulong AchievementsChannelID { get; } = 0;
    public ulong ReviewChannelID { get; } = 0;
    public ulong LogsChannelID { get; } = 0;
    public ulong GuestRoleID { get; } = 0;
    public ulong GuildMemberRoleID { get; } = 0;
    public ulong HonoraryQuestRoleID { get; } = 0;
    public ulong TreehardRoleID { get; } = 0;
    public ulong TreehardPlusRoleID { get; } = 0;
    public ulong ChallengerRoleID { get; } = 0;
    public ulong LeaderboarderRoleID { get; } = 0;
    public string LogDestinationPath { get; } = ".";
    public string DatabaseLogin { get; } = "";

    private HBConfigSingleton(string configFilePath) {
        Json json;
        try {
            json = new Json(File.ReadAllText(configFilePath));
        } catch (Exception e) {
            HBData.Log.Info("Unable to properly access configuration file: " + e.Message);
            return;
        }
        if (json.IsEmpty()) {
            HBData.Log.Info($"Invalid configuration file!");
            return;
        }
        List<string> missingProperties = new List<string>();

        string? rawHypixelKey = json.GetString("HypixelKey");
        HypixelKey = rawHypixelKey ?? "";
        if (rawHypixelKey == null)
            missingProperties.Add("HypixelKey");

        string? rawDiscordToken = json.GetString("DiscordToken");
        DiscordToken = rawDiscordToken ?? "";
        if (rawDiscordToken == null)
            missingProperties.Add("DiscordToken");

        string? rawHypixelGuildID = json.GetString("HypixelGuildID");
        HypixelGuildID = rawHypixelGuildID ?? "";
        if (rawHypixelGuildID == null)
            missingProperties.Add("HypixelGuildID");

        ulong? rawDiscordGuildID = json.GetUInt64("DiscordGuildID");
        DiscordGuildID = rawDiscordGuildID ?? 0;
        if (rawDiscordGuildID == null)
            missingProperties.Add("DiscordGuildID");

        ulong? rawLeaderboardsChannelID = json.GetUInt64("LeaderboardsChannelID");
        LeaderboardsChannelID = rawLeaderboardsChannelID ?? 0;
        if (rawLeaderboardsChannelID == null)
            missingProperties.Add("LeaderboardsChannelID");
        
        ulong? rawAchievementsChannelID = json.GetUInt64("AchievementsChannelID");
        AchievementsChannelID = rawAchievementsChannelID ?? 0;
        if (rawAchievementsChannelID == null)
            missingProperties.Add("AchievementsChannelID");

        ulong? rawLogsChannelID = json.GetUInt64("LogsChannelID");
        LogsChannelID = rawLogsChannelID ?? 0;
        if (rawLogsChannelID == null)
            missingProperties.Add("LogsChannelID");

        string? rawLogDestinationPath = json.GetString("LogDestinationPath");
        LogDestinationPath = rawLogDestinationPath ?? ".";
        if (rawLogDestinationPath == null)
            missingProperties.Add("LogDestinationPath (Note that as a result of this missing, this log will be stored in the working directory.)");

        string? rawDatabaseLogin = json.GetString("DatabaseLogin");
        DatabaseLogin = rawDatabaseLogin ?? "";
        if (rawDatabaseLogin == null)
            missingProperties.Add("DatabaseLogin");

        ulong? rawReviewChannelID = json.GetUInt64("ReviewChannelID");
        ReviewChannelID = rawReviewChannelID ?? 0;
        if (rawReviewChannelID == null)
            missingProperties.Add("ReviewChannelID");

        if (missingProperties.Count > 0) {
            string message = "Invalid configuration file! The following properties are missing:";
            foreach (string missing in missingProperties)
                message += $"\n- {missing}";
            HBData.Log.Info(message);
            return;
        }

        List<string> missingRoles = new List<string>();
        foreach (string roleTitle in HBAssets.NecessaryRoles) {
            ulong? value = json.GetUInt64($"Roles.{roleTitle}");
            if (value != null)
                HBData.RoleMap[roleTitle] = (ulong)value;
            else
                missingRoles.Add(roleTitle);
        }
        if (missingRoles.Count != 0) {
            string message = "Invalid configuration file! The following roles are missing from the configuration:";
            foreach (string missing in missingRoles)
                message += $"\n- {missing}";
            HBData.Log.Info(message);
            return;
        }
        GuestRoleID = HBData.RoleMap["Guest"];
        GuildMemberRoleID = HBData.RoleMap["Guild Member"];
        HonoraryQuestRoleID = HBData.RoleMap["Honorary Quest"];
        TreehardRoleID = HBData.RoleMap["Treehard"];
        TreehardPlusRoleID = HBData.RoleMap["Treehard+"];
        ChallengerRoleID = HBData.RoleMap["Challenger"];
        LeaderboarderRoleID = HBData.RoleMap["Leaderboarder"];
        IsCorrect = true;
    }

    // Singleton Management

    private static HBConfigSingleton? singleton = null;

    public static HBConfigSingleton GetSingleton() {
        if (singleton != null)
            return singleton;
        singleton = new HBConfigSingleton("config.json");
        return singleton;
    }

}

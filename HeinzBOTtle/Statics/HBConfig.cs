namespace HeinzBOTtle.Statics;

public static class HBConfig {

    /// <summary>The secret key used to make requests to the Hypixel API.</summary>
    public static string HypixelKey => HBConfigSingleton.GetSingleton().HypixelKey;
    /// <summary>The secret token used to authenticate and control the Discord bot.</summary>
    public static string DiscordToken => HBConfigSingleton.GetSingleton().DiscordToken;
    /// <summary>The database document ID used to retrieve Hypixel guild information with the Hypixel API.</summary>
    public static string HypixelGuildID => HBConfigSingleton.GetSingleton().HypixelGuildID;
    /// <summary>The ID of the Discord guild within which the bot operates.</summary>
    public static ulong DiscordGuildID => HBConfigSingleton.GetSingleton().DiscordGuildID;
    /// <summary>The ID of the Discord text channel dedicated to hosting the leaderboards.</summary>
    public static ulong LeaderboardsChannelID => HBConfigSingleton.GetSingleton().LeaderboardsChannelID;
    /// <summary>The ID of the Discord text channel dedicated to hosting player achievements.</summary>
    public static ulong AchievementsChannelID => HBConfigSingleton.GetSingleton().AchievementsChannelID;
    /// <summary>The ID of the Discord text channel to which link reviews are sent.</summary>
    public static ulong ReviewChannelID => HBConfigSingleton.GetSingleton().ReviewChannelID;
    /// <summary>The ID of the Discord text channel dedicated to in-guild logging.</summary>
    public static ulong LogsChannelID => HBConfigSingleton.GetSingleton().LogsChannelID;
    /// <summary>The Discord role ID for the Guest role.</summary>
    public static ulong GuestRoleID => HBConfigSingleton.GetSingleton().GuestRoleID;
    /// <summary>The Discord role ID for the Guild Member role.</summary>
    public static ulong GuildMemberRoleID => HBConfigSingleton.GetSingleton().GuildMemberRoleID;
    /// <summary>The Discord role ID for the Honorary Quest role.</summary>
    public static ulong HonoraryQuestRoleID => HBConfigSingleton.GetSingleton().HonoraryQuestRoleID;
    /// <summary>The Discord role ID for the Treehard role.</summary>
    public static ulong TreehardRoleID => HBConfigSingleton.GetSingleton().TreehardRoleID;
    /// <summary>The Discord role ID for the Treehard+ role.</summary>
    public static ulong TreehardPlusRoleID => HBConfigSingleton.GetSingleton().TreehardPlusRoleID;
    /// <summary>The Discord role ID for the Challenger role (for positions on a leaderboard #1-#100).</summary>
    public static ulong ChallengerRoleID => HBConfigSingleton.GetSingleton().ChallengerRoleID;
    /// <summary>The Discord role ID for the Leaderboarder role (for positions on a leaderboard #1-#10).</summary>
    public static ulong LeaderboarderRoleID => HBConfigSingleton.GetSingleton().LeaderboarderRoleID;
    /// <summary>The place where the log files should be stored when the program terminates.</summary>
    public static string LogDestinationPath => HBConfigSingleton.GetSingleton().LogDestinationPath;
    /// <summary>The login details for the database in the form "IP[:PORT] DATABASENAME USERNAME PASSWORD"</summary>
    public static string DatabaseLogin => HBConfigSingleton.GetSingleton().DatabaseLogin;

}

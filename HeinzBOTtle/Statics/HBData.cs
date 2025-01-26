using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Leaderboards;

namespace HeinzBOTtle.Statics;

public static class HBData {

    /// <summary>The logging interface for all displayed messages.</summary>
    public static HBLog Log { get; } = new HBLog("log-full.txt", "log-reduced.txt");
    /// <summary>The cache of requests to the Hypixel API.</summary>
    public static Dictionary<string, CachedInfo> APICache { get; } = new Dictionary<string, CachedInfo>();
    /// <summary>The cache of leaderboard rankings where a player's username (normalized to lowercase) is mapped to the player's ranking information.</summary>
    public static Dictionary<string, LBRankingData> LeaderboardRankings { get; } = new Dictionary<string, LBRankingData>();
    /// <summary>Timestamp indicating when the last leaderboard update began.</summary>
    public static long LeaderboardsLastUpdated { get; set; } = 0;
    /// <summary>A semaphore indicating whether a leaderboard update is in progress.</summary>
    public static Semaphore LeaderboardsUpdating { get; } = new Semaphore(1, 1);
    /// <summary>A semaphore controlling the order of concurrent database modifications.</summary>
    public static Semaphore ModifyingDatabase { get; } = new Semaphore(1, 1);
    /// <summary>A dictionary mapping names of Discord roles to their corresponding role IDs in the guild.</summary>
    public static Dictionary<string, ulong> RoleMap { get; } = new Dictionary<string, ulong>();

}

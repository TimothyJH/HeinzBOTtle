using HeinzBOTtle.Statics;
using System.Text.Json;

namespace HeinzBOTtle.Hypixel;

public static class HypixelMethods {

    /// <summary>Makes a player request to the Hypixel API. The cache may be used if the same player was requested less than 10 minutes ago.</summary>
    /// <param name="identifier">The username or UUID of the player whose data to request</param>
    /// <param name="uuid">True if the identifier is a UUID, false if it is a username</param>
    /// <returns>The player JSON response.</returns>
    public static async Task<Json> RetrievePlayerAPI(string identifier, bool uuid = false) {
        return await RequestHypixelAPI("player", uuid ? "uuid" : "name", identifier.ToLower());
    }

    /// <summary>Makes a guild request to the Hypixel API. The cache may be used if the same guild was requested less than 10 minutes ago.</summary>
    /// <param name="guildID">The database ID of the guild whose data to request</param>
    /// <returns>The guild JSON response.</returns>
    public static async Task<Json> RetrieveGuildAPI(string guildID) {
        return await RequestHypixelAPI("guild", "id", guildID);
    }

    /// <summary>Makes an in-game leaderboards request to the Hypixel API. The cache may be used if the leaderboards were requested less than 10 minutes ago.</summary>
    /// <returns>The leaderboards JSON response.</returns>
    public static async Task<Json> RetrieveLeaderboardsAPI() {
        return await RequestHypixelAPI("v2/leaderboards");
    }

    /// <summary>Makes a request to specified endpoint the Hypixel API with a default timeout of 10 seconds.</summary>
    /// <param name="endpoint">The endpoint to contact</param>
    /// <param name="parameter">The parameter of the request (optional)</param>
    /// <param name="argument">The argument to the parameter of the request (only required when there is a parameter)</param>
    /// <param name="timeout">The amount of time in milliseconds to wait for the request to finish before cancelling it (optional)</param>
    /// <returns>The JSON response if the query was successful, otherwise a JSON object with "success" => false.</returns>
    private static async Task<Json> RequestHypixelAPI(string endpoint, string parameter = "", string? argument = "", int timeout = 10000) {
        string cacheKey = $"{endpoint}{(parameter != "" ? $"?{parameter}={argument}" : "")}";
        if (HBData.APICache.TryGetValue(cacheKey, out CachedInfo? cacheValue) && DateTime.Now.Ticks - cacheValue.Timestamp < 600L * 10000000L) {
            // This is the case where the API is being polled when we already have a recent enough copy.
            await HBData.Log.InfoAsync($"API cache hit: {cacheKey}");
            return cacheValue.JsonResponse;
        }
        string query = $"https://api.hypixel.net/{endpoint}?key={HBConfig.HypixelKey}{(parameter != null ? $"&{parameter}={argument}" : "")}";
        Task<HttpResponseMessage> responseTask = HBClients.HttpClient.GetAsync(query);
        Task waitTimerTask = Task.Delay(timeout);
        await HBData.Log.InfoAsync($"Made API request: {cacheKey}");
        Task.WaitAny(responseTask, waitTimerTask);
        if (!responseTask.IsCompleted)
            return new Json("{\"success\": false}"); ;
        string json = await responseTask.Result.Content.ReadAsStringAsync();
        Json formatted = new Json(json);
        HBData.APICache[cacheKey] = new CachedInfo(DateTime.Now.Ticks, formatted);
        return formatted;
    }

    /// <summary>Removes entries older than 10 minutes from <see cref="HBData.APICache"/>.</summary>
    public static void CleanCache() {
        if (HBData.APICache.Count == 0)
            return;
        List<string> oldEntries = new List<string>();
        foreach (string key in HBData.APICache.Keys) {
            if (DateTime.Now.Ticks - HBData.APICache[key].Timestamp > 600L * 10000000L)
                oldEntries.Add(key);
        }
        foreach (string username in oldEntries)
            HBData.APICache.Remove(username);
    }

    /// <summary>This method might cause an exception if the provided string is not of length 32.</summary>
    /// <param name="rawUUID">The raw, undashed UUID</param>
    /// <returns>A Minecraft UUID that is dashed as presented in Minecraft.</returns>
    public static string ToDashedUUID(string rawUUID) {
        return $"{rawUUID[0..8]}-{rawUUID[8..12]}-{rawUUID[12..16]}-{rawUUID[16..20]}-{rawUUID[20..32]}";
    }

    /// <param name="username">The username to check</param>
    /// <returns>True if the provided username is a possible Minecraft username, otherwise false.</returns>
    public static bool IsValidUsername(string username) {
        if (username == null || username.Length == 0 || username.Length > 16)
            return false;
        for (int i = 0; i < username.Length; i++) {
            char c = username[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
                return false;
        }
        return true;
    }

    /// <summary>Finds the best position held by the player on all the API-provided in-game leaderboards that don't reset.</summary>
    /// <param name="uuid">The undashed UUID of the player whose best position to find</param>
    /// <returns>-1 if there is an error, <see cref="int.MaxValue"/> if the player is not on the searched leaderboards, or the 1-based position that was found.</returns>
    public static async Task<int> BestLeaderboardPositionAsync(string uuid) {
        // Requesting and processing baseline leaderboard information:
        Task initialBuffer = Task.Delay(1000);
        Json? leaderboards = await RetrieveLeaderboardsAPI();
        initialBuffer.Wait();
        if (leaderboards == null || leaderboards.GetBoolean("success") == false) {
            await HBData.Log.InfoAsync("ERROR: In-game leaderboards retrieval unsuccessful :(");
            return -1;
        }
        Dictionary<string, List<JsonElement>>? groups = leaderboards.GetObject<string, List<JsonElement>>("leaderboards");
        if (groups == null || groups.Count == 0) {
            await HBData.Log.InfoAsync("ERROR: In-game leaderboards retrieval unsuccessful :(");
            return -1;
        }

        // Examining the leaderboards
        int bestPosition = int.MaxValue;
        string[] excludedPrefixes = { "Daily", "Weekly", "Monthly" };
        foreach (List<JsonElement> group in groups.Values) {
            foreach (JsonElement leaderboardRaw in group) {
                if (leaderboardRaw.ValueKind != JsonValueKind.Object)
                    continue;
                Dictionary<string, JsonElement>? leaderboard;
                try {
                    leaderboard = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(leaderboardRaw);
                } catch {
                    leaderboard = null;
                }
                if (leaderboard == null || !leaderboard.ContainsKey("prefix") || !leaderboard.ContainsKey("leaders"))
                    continue;
                string? prefix = leaderboard["prefix"].GetString();
                if (prefix == null || excludedPrefixes.Contains(prefix))
                    continue;
                List<string>? players;
                try {
                    players = JsonSerializer.Deserialize<List<string>>(leaderboard["leaders"]);
                } catch {
                    players = null;
                }
                if (players == null || players.Count == 0)
                    continue;
                int limit = int.Min(100, players.Count);
                for (int i = 0; i < limit; i++) {
                    string leaderboarder = players[i].Replace("-", "");
                    if (uuid.Equals(leaderboarder) && (i + 1) < bestPosition)
                        bestPosition = i + 1;
                }
            }
        }

        return bestPosition;
    }

    /// <param name="player">The JSON for the player whose wins are to be calculated</param>
    /// <returns>The total number of wins the player has among all the Wool Games.</returns>
    public static int GetTotalWoolGamesWins(Json player) {
        int wins = 0;
        if (player.GetValueKind("player.achievements.woolgames_wool_wars_winner") == JsonValueKind.Number)
            wins += (int)(player.GetDouble("player.achievements.woolgames_wool_wars_winner") ?? 0.0);
        if (player.GetValueKind("player.achievements.woolgames_sheep_wars_winner") == JsonValueKind.Number)
            wins += (int)(player.GetDouble("player.achievements.woolgames_sheep_wars_winner") ?? 0.0);
        if (player.GetValueKind("player.stats.WoolGames.capture_the_wool.stats.participated_wins") == JsonValueKind.Number)
            wins += (int)(player.GetDouble("player.stats.WoolGames.capture_the_wool.stats.participated_wins") ?? 0.0);
        return wins;
    }

    // Taken from forums (lol)
    public static double GetNetworkLevelFromXP(double xp) {
        return Math.Sqrt(2.0 * xp + 30625.0) / 50.0 - 2.5;
    }

    // Derived based on inverse
    public static double GetNetworkXPFromLevel(double level) {
        return (Math.Pow(50.0 * (level + 25.0), 2.0) - 30625.0) / 2.0;
    }

    // Adapted from forums
    // https://hypixel.net/threads/solved-api-how-do-i-calculate-skywars-experience-into-levels.3171613/
    private static readonly int[] swXPTable = { 0, 20, 70, 150, 250, 500, 1000, 2000, 3500, 6000, 10000, 15000 };
    public static double GetSkyWarsLevelFromXP(int xp) {
        if (xp >= 15000)
            return ((double)xp - 15000) / 10000.0 + 12.0;
        else {
            for (int i = 0; i < swXPTable.Length; i++) {
                if (xp < swXPTable[i])
                    return i + ((double)xp - swXPTable[i - 1]) / (swXPTable[i] - swXPTable[i - 1]);
            }
            return 0.0;
        }
    }

    // Adapted from hypixel-php
    // https://github.com/Plancke/hypixel-php/blob/master/src/util/games/bedwars/ExpCalculator.php
    private static readonly int bwEasyLevels = 4;
    private static readonly int[] bwEasyLevelsXP = { 500, 1000, 2000, 3500 };
    private static readonly int bwEasyLevelsXPTotal = 7000;
    private static readonly int bwXPPerLevel = 5000;
    private static readonly int bwXPPerPrestige = 96 * bwXPPerLevel + bwEasyLevelsXPTotal;
    private static readonly int bwLevelsPerPrestige = 100;
    private static readonly int bwHighestPrestige = 10;
    private static int BW_GetXPForLevel(int level) {
        if (level == 0)
            return 0;
        int respectedLevel = BW_GetLevelRespectingPrestige(level);
        if (respectedLevel <= bwEasyLevels)
            return bwEasyLevelsXP[respectedLevel - 1];
        return bwXPPerLevel;
    }
    private static int BW_GetLevelRespectingPrestige(int level) {
        if (level > bwHighestPrestige * bwLevelsPerPrestige)
            return level - bwHighestPrestige * bwLevelsPerPrestige;
        else
            return level % bwLevelsPerPrestige;
    }
    public static double GetBedWarsLevelFromXP(int xp) {
        int prestiges = xp / bwXPPerPrestige;
        int level = prestiges * bwLevelsPerPrestige;
        int xpWithoutPrestiges = xp - prestiges * bwXPPerPrestige;
        for (int i = 1; i <= bwEasyLevels; ++i) {
            int xpForEasyLevel = BW_GetXPForLevel(i);
            if (xpWithoutPrestiges < xpForEasyLevel)
                break;
            level++;
            xpWithoutPrestiges -= xpForEasyLevel;
        }
        return level + (double)xpWithoutPrestiges / bwXPPerLevel;
    }

    // Adapted from forums
    // https://hypixel.net/threads/discord-js-get-woolwars-star-from-exp.4949443/
    private static readonly int[] wwMinimalXP = { 0, 1000, 3000, 6000, 10000, 15000 };
    private static readonly int wwBaseLevel = wwMinimalXP.Length;
    private static readonly int wwBaseXP = wwMinimalXP[wwMinimalXP.Length - 1];
    public static double GetWoolWarsLevelFromXP(int xp) {
        if (xp >= wwBaseXP)
            return (xp - wwBaseXP) / 5000.0 + wwBaseLevel;
        else {
            for (int i = 0; i < wwMinimalXP.Length; i++) {
                if (xp < wwMinimalXP[i])
                    return i + ((double)xp - wwMinimalXP[i - 1]) / (wwMinimalXP[i] - wwMinimalXP[i - 1]);
            }
            return 0.0;
        }
    }

    /// <summary>Formats the provided integer string with commas. This method assumes that there are no commas and that the string is a clean integer.</summary>
    /// <param name="old">The clean integer string to format</param>
    /// <returns>The number formatted with commas.</returns>
    public static string WithCommas(string old) {
        string formatted = "";
        int digit = 0;
        for (int i = old.Length - 1; i >= 0; i--) {
            formatted = old[i] + formatted;
            digit++;
            if (digit % 3 == 0 && i != 0)
                formatted = ',' + formatted;
        }
        return formatted;
    }

    /// <summary>Formats the provided integer with commas.</summary>
    /// <param name="value">The integer to format</param>
    /// <returns>The number formatted with commas.</returns>
    public static string WithCommas(int value) {
        if (value < 1000)
            return value.ToString();
        return WithCommas(value.ToString());
    }

    /// <summary>Formats the provided double with commas.</summary>
    /// <param name="value">The double to format</param>
    /// <returns>The number formatted with commas.</returns>
    public static string WithCommas(double value) {
        if (value < 1000)
            return value.ToString();
        string old = value.ToString();
        if (old.Contains('.')) {
            string[] split = old.Split('.', 2);
            return WithCommas(split[0]) + "." + split[1];
        } else
            return WithCommas(old);
    }

    /// <summary>Formats the provided numerical string to contain at least two digits after the decimal point. It may contain commas.</summary>
    /// <param name="formattedDouble">The numerical string to format</param>
    /// <returns>The number formatted to contain at least two digits after the decimal point.</returns>
    public static string PadDecimalPlaces(string formattedDouble) {
        if (!formattedDouble.Contains('.'))
            return formattedDouble + ".00";
        string[] split = formattedDouble.Split(".", 2);
        if (split[1].Length == 1)
            return formattedDouble + "0";
        return formattedDouble;
    }

    /// <summary>Formats the provided numerical string to contain at least one digit after the decimal point. It may contain commas.</summary>
    /// <param name="formattedDouble">The numerical string to format</param>
    /// <returns>The number formatted to contain at least one digit after the decimal point.</returns>
    public static string PadOneDecimalPlace(string formattedDouble) {
        if (!formattedDouble.Contains('.'))
            return formattedDouble + ".0";
        return formattedDouble;
    }

}

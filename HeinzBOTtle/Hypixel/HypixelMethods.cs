namespace HeinzBOTtle.Hypixel;

public static class HypixelMethods {

    /// <summary>Makes a player request to the Hypixel API. The cache may be used if the same player was requested less than 10 minutes ago.</summary>
    /// <param name="identifier">The username or UUID of the player whose data to request</param>
    /// <param name="uuid">True if the identifier is a UUID, false if it is a username</param>
    /// <returns>The player JSON response.</returns>
    public static async Task<Json> RetrievePlayerAPI(string identifier, bool uuid = false) {
        identifier = identifier.ToLower();
        if (HBData.APICache.ContainsKey(identifier) && DateTime.Now.Ticks - HBData.APICache[identifier].Timestamp < 600L * 10000000L) {
            // This is the case where the API is being polled when we already have a recent enough copy.
            await HBData.Log.InfoAsync("API cache hit for player: " + identifier);
            return HBData.APICache[identifier].JsonResponse;
        } else {
            Task<HttpResponseMessage> responseTask = HBData.HttpClient.GetAsync($"https://api.hypixel.net/player?key={HBData.HypixelKey}&{(uuid ? "uuid" : "name")}={identifier}");
            Task waitTimerTask = Task.Delay(10000);
            await HBData.Log.InfoAsync("Made API request for player: " + identifier);
            Task.WaitAny(responseTask, waitTimerTask);
            if (!responseTask.IsCompleted)
                return new Json("{\"success\": false}");
            string json = await responseTask.Result.Content.ReadAsStringAsync();
            Json formatted = new Json(json);
            HBData.APICache[identifier] = new CachedInfo(DateTime.Now.Ticks, formatted);
            return formatted;
        }
    }

    /// <summary>Makes a guild request to the Hypixel API. The cache may be used if the same guild was requested less than 10 minutes ago.</summary>
    /// <param name="guildID">The database ID of the guild whose data to request</param>
    /// <returns>The guild JSON response.</returns>
    public static async Task<Json> RetrieveGuildAPI(string guildID) {
        if (HBData.APICache.ContainsKey(guildID) && DateTime.Now.Ticks - HBData.APICache[guildID].Timestamp < 600L * 10000000L) {
            // This is the case where the API is being polled when we already have a recent enough copy.
            await HBData.Log.InfoAsync("API cache hit for guild: " + guildID);
            return HBData.APICache[guildID].JsonResponse;
        } else {
            Task<HttpResponseMessage> responseTask = HBData.HttpClient.GetAsync($"https://api.hypixel.net/guild?key={HBData.HypixelKey}&id={guildID}");
            Task waitTimerTask = Task.Delay(10000);
            await HBData.Log.InfoAsync("Made API request for guild: " + guildID);
            Task.WaitAny(responseTask, waitTimerTask);
            if (!responseTask.IsCompleted)
                return new Json("{\"success\": false}");
            string json = await responseTask.Result.Content.ReadAsStringAsync();
            Json formatted = new Json(json);
            HBData.APICache[guildID] = new CachedInfo(DateTime.Now.Ticks, formatted);
            return formatted;
        }
    }

    /// <summary>Makes a request to specified endpoint the Hypixel API with a timeout of 10 seconds.</summary>
    /// <param name="endpoint">The endpoint to contact</param>
    /// <param name="parameter">The parameter of the request</param>
    /// <param name="argument">The argument to the parameter of the request</param>
    /// <returns>The JSON response.</returns>
    public static async Task<Json?> RequestWithTimeout(string endpoint, string parameter, string argument) {
        Task<HttpResponseMessage> responseTask = HBData.HttpClient.GetAsync("https://api.hypixel.net/" + endpoint + "?key=" + HBData.HypixelKey + "&" + parameter + "=" + argument);
        Task waitTimerTask = Task.Delay(10000);
        await HBData.Log.InfoAsync($"Made API request at '{endpoint}' where '{parameter}' = '{argument}'");
        Task.WaitAny(responseTask, waitTimerTask);
        if (!responseTask.IsCompleted)
            return null;
        string json = await responseTask.Result.Content.ReadAsStringAsync();
        Json formatted = new Json(json);
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

namespace HeinzBOTtle {

    class CachedPlayerInfo {

        public long Timestamp { get; set; }
        public string JsonResponse { get; set; }

        public CachedPlayerInfo(long timestamp, string jsonResponse) {
            Timestamp = timestamp;
            JsonResponse = jsonResponse;
        }

    }
    
    class HypixelMethods {

        public static Dictionary<string, CachedPlayerInfo> PlayerCache = new Dictionary<string, CachedPlayerInfo>();

        public static async Task<string> RetrievePlayerAPI(string username) {
            username = username.ToLower();
            if (PlayerCache.ContainsKey(username) && (DateTime.Now.Ticks - PlayerCache[username].Timestamp < 600L * 10000000L)) {
                // This is the case where the API is being polled when we already have a recent enough copy.
                return PlayerCache[username].JsonResponse;
            } else {
                Task<HttpResponseMessage> responseTask = Program.httpClient.GetAsync("https://api.hypixel.net/player?key=" + Program.hypixelKey + "&name=" + username);
                Task waitTimerTask = Task.Delay(10000);
                Console.WriteLine("Made API request for player: " + username);
                Task.WaitAny(responseTask, waitTimerTask);
                if (!responseTask.IsCompleted)
                    return "{\"success\": false}";
                string json = await responseTask.Result.Content.ReadAsStringAsync();
                PlayerCache[username] = new CachedPlayerInfo(DateTime.Now.Ticks, json);
                return json;
            }
        }

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
            return (Math.Sqrt((2.0 * xp) + 30625.0) / 50.0) - 2.5;
        }

        // Derived based on inverse
        public static double GetNetworkXPFromLevel(double level) {
            return (Math.Pow(50.0 * (level + 25.0), 2.0) - 30625.0) / 2.0;
        }

    }

}

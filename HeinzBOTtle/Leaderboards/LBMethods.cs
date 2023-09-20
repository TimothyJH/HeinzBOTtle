using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards {

    static class LBMethods {

        public static void WipeLeaderboards() {
            foreach (Leaderboard leaderboard in HBData.LeaderboardList)
                leaderboard.Reset();
        }

        // Assumes non-negative value even though parameter integer is signed
        public static string FormatPosition(int number) {
            if (number < 10)
                return "00" + number;
            else if (number > 9 && number < 100)
                return "0" + number;
            else
                return number.ToString();
        }

        public static async Task WipeLeaderboardsChannel(SocketTextChannel lbChannel) {
            List<IMessage> oldMessages = new List<IMessage>();
            IAsyncEnumerable<IReadOnlyCollection<IMessage>> pagesAsync = lbChannel.GetMessagesAsync(100);
            List<IReadOnlyCollection<IMessage>> pages = await pagesAsync.ToListAsync();
            foreach (IReadOnlyCollection<IMessage> page in pages) {
                foreach (IMessage message in page) {
                    oldMessages.Add(message);
                }
            }
            foreach (IMessage message in oldMessages) {
                await Task.Delay(1000);
                if (message.Thread != null) {
                    await message.Thread.DeleteAsync();
                    await Task.Delay(1000);
                }
                await message.DeleteAsync();
            }
        }

        public static async Task UpdateLeaderboards() {
            // Performing some setup:
            HBData.LeaderboardsUpdating = true;
            HBData.LeaderboardRankings.Clear();
            HBData.QuestParticipationsLeaderboardMap.Clear();
            SocketTextChannel lbChannel = (SocketTextChannel)HBData.DiscordClient.GetChannel(HBData.LeaderboardsChannelID);
            WipeLeaderboards();

            // Requesting and processing guild information:
            Json? guild = await HypixelMethods.RequestWithTimeout("guild", "id", HBData.HypixelGuildID);
            Task initialBuffer = Task.Delay(1000);
            if (guild == null || guild.GetBoolean("success") == false) {
                Console.WriteLine("ERROR: Guild information retrieval unsuccessful :(");
                HBData.LeaderboardsUpdating = false;
                return;
            }
            List<JsonElement>? members = guild.GetArray("guild.members");
            if (members == null || members.Count == 0) {
                Console.WriteLine("ERROR: Guild member list retrieval unsuccessful :(");
                HBData.LeaderboardsUpdating = false;
                return;
            }

            // Getting the members' UUIDs and guild quest participations from the JSON array:
            List<string> uuids = new List<string>();
            foreach (JsonElement member in members) {
                if (member.ValueKind != JsonValueKind.Object)
                    continue;
                
                Dictionary<string, JsonElement>? memberDic;
                try {
                    memberDic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(member);
                } catch {
                    memberDic = null;
                }
                
                if (memberDic == null || !memberDic.ContainsKey("uuid"))
                    continue;
                JsonElement uuid = memberDic["uuid"];
                string? uuidString = uuid.GetString();
                if (uuidString == null)
                    continue;
                uuids.Add(uuidString);

                if (memberDic.ContainsKey("questParticipation")) {
                    JsonElement questParticipations = memberDic["questParticipation"];
                    int questParticipationsInt = (int)questParticipations.GetDouble();
                    HBData.QuestParticipationsLeaderboardMap.Add(uuidString, questParticipationsInt);
                }
                
                
            }

            // Starting to wipe old leaderboards:
            Task channelWipeTask = WipeLeaderboardsChannel(lbChannel);

            // Requesting player data and populating leaderboards:
            await initialBuffer;
            foreach (string uuid in uuids) {
                Json? json = await HypixelMethods.RequestWithTimeout("player", "uuid", uuid);
                Task buffer = Task.Delay(1000);
                if (json == null || json.GetBoolean("success") == false || json.GetValueKind("player") != JsonValueKind.Object)
                    Console.WriteLine("Ignoring player " + uuid + " in leaderboards update");
                else {
                    HBData.LeaderboardRankings.Add((json.GetString("player.displayname") ?? "?????").ToLower(), new LBRankingData(json));
                    foreach (Leaderboard leaderboard in HBData.LeaderboardList) {
                        if (leaderboard is not AverageLeaderboardPositionLeaderboard)
                            leaderboard.EnterPlayer(json);
                    }
                }
                await buffer;
            }

            // Refreshing rankings cache:
            RefreshRankings();

            // Populating the leaderboard for Average Leaderboard Position:
            AverageLeaderboardPositionLeaderboard averageLeaderboardPositionLeaderboard = (AverageLeaderboardPositionLeaderboard)
                (HBData.LeaderboardList.Find(x => x is AverageLeaderboardPositionLeaderboard) ?? new AverageLeaderboardPositionLeaderboard());
            foreach (LBRankingData player in HBData.LeaderboardRankings.Values)
                averageLeaderboardPositionLeaderboard.EnterPlayer(player);
            RefreshRankings(averageLeaderboardPositionLeaderboard);
                

            // Awaiting the completion of leaderboards wiping:
            await channelWipeTask;

            // Post new leaderboards:
            foreach (Leaderboard leaderboard in HBData.LeaderboardList) {
                RestUserMessage header = await lbChannel.SendMessageAsync(embed: leaderboard.GenerateHeaderEmbed());
                await Task.Delay(1000);
                SocketThreadChannel thread = await lbChannel.CreateThreadAsync(leaderboard.GameTitle + (leaderboard.GameStat.Equals("") ? "" : $" ({leaderboard.GameStat})"), message: header);
                foreach (Embed scoreChunk in leaderboard.GenerateScoresEmbeds()) {
                    await Task.Delay(1000);
                    await thread.SendMessageAsync(embed: scoreChunk);
                }
                await Task.Delay(1000);
            }

            // Pin the leaderboard headers so that they display in alphabetical order:
            List<IMessage> headers = new List<IMessage>();
            IAsyncEnumerable<IReadOnlyCollection<IMessage>> pagesAsync = lbChannel.GetMessagesAsync(HBData.LeaderboardList.Count);
            List<IReadOnlyCollection<IMessage>> pages = await pagesAsync.ToListAsync();
            foreach (IReadOnlyCollection<IMessage> page in pages) {
                foreach (IMessage header in page) {
                    headers.Add(header);
                }
            }
            foreach (IMessage header in headers) {
                await Task.Delay(1000);
                try {
                    await ((IUserMessage)header).PinAsync();
                } catch { }
            }
            await Task.Delay(1000);

            // Delete the pin messages:
            List<IMessage> headersPins = new List<IMessage>();
            IAsyncEnumerable<IReadOnlyCollection<IMessage>> pagesPinsAsync = lbChannel.GetMessagesAsync(HBData.LeaderboardList.Count);
            List<IReadOnlyCollection<IMessage>> pagesPins = await pagesPinsAsync.ToListAsync();
            foreach (IReadOnlyCollection<IMessage> page in pagesPins) {
                foreach (IMessage header in page) {
                    headersPins.Add(header);
                }
            }
            await Task.Delay(1000);
            await lbChannel.DeleteMessagesAsync(headersPins);
            await Task.Delay(1000);

            // Cleaning up:
            EmbedBuilder done = new EmbedBuilder();
            done.WithDescription("Please note that the leaderboards system is new and may contain errors.\n\nThe leaderboards are finished updating.").WithColor(Color.Green).WithCurrentTimestamp();
            await lbChannel.SendMessageAsync(embed: done.Build());
            HBData.LeaderboardsUpdating = false;
        }

        public static void RefreshRankings() {
            foreach (Leaderboard leaderboard in HBData.LeaderboardList)
                RefreshRankings(leaderboard);
        }

        public static void RefreshRankings(Leaderboard leaderboard) {
            Dictionary<string, LBRanking> boardRankings = leaderboard.GenerateRankings();
            foreach (string key in boardRankings.Keys) {
                LBRanking target = boardRankings[key];
                List<LBRanking> playerRankings = HBData.LeaderboardRankings[key].Rankings;
                for (int i = playerRankings.Count - 1; i >= -1; i--) {
                    if (i == -1) {
                        playerRankings.Insert(0, target);
                        break;
                    }
                    if (target >= playerRankings[i]) {
                        playerRankings.Insert(i + 1, target);
                        break;
                    }
                }
            }
        }

        public static async Task RefreshRankingsFromChannelAsync(SocketTextChannel channel) {
            List<IMessage> messages = new List<IMessage>();
            List<IReadOnlyCollection<IMessage>> pages = await channel.GetMessagesAsync(HBData.LeaderboardList.Count + 5).ToListAsync();
            foreach (IReadOnlyCollection<IMessage> page in pages) {
                foreach (IMessage message in page)
                    messages.Add(message);
            }
            bool initializedPlayers = false;
            for (int i = messages.Count - 1; i >= 0; i--) {
                IMessage message = messages[i];
                if (message.Embeds.Count == 0 || message.Thread == null)
                    continue;
                IEmbed embed = message.Embeds.First();
                Leaderboard? leaderboard = HBData.LeaderboardList.Find(x => x.GameTitle.Equals(embed.Title) && x.GameStat.Equals(embed.Description ?? ""));
                if (leaderboard == null)
                    continue;
                if (initializedPlayers)
                    await RefreshRankingsFromChannelAsync(leaderboard, message.Thread, false);
                else {
                    await RefreshRankingsFromChannelAsync(leaderboard, message.Thread, true);
                    initializedPlayers = true;
                }
            }
        }

        public static async Task RefreshRankingsFromChannelAsync(Leaderboard leaderboard, IThreadChannel thread, bool initializePlayers) {
            Dictionary<string, LBRanking> boardRankings = await leaderboard.GenerateRankingsFromThreadAsync(thread, initializePlayers);
            foreach (string key in boardRankings.Keys) {
                LBRanking target = boardRankings[key];
                List<LBRanking> playerRankings = HBData.LeaderboardRankings[key].Rankings;
                for (int i = playerRankings.Count - 1; i >= -1; i--) {
                    if (i == -1) {
                        playerRankings.Insert(0, target);
                        break;
                    }
                    if (target >= playerRankings[i]) {
                        playerRankings.Insert(i + 1, target);
                        break;
                    }
                }
            }
        }

    }

}

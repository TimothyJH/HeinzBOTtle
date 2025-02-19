﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Leaderboards.Special;
using HeinzBOTtle.Statics;
using System.Text.Json;

namespace HeinzBOTtle.Leaderboards;

public static class LBMethods {

    /// <summary>Resets all entries in all leaderboards.</summary>
    public static void WipeLeaderboards() {
        foreach (Leaderboard leaderboard in HBAssets.LeaderboardList)
            leaderboard.Reset();
    }

    /// <summary>Removes the last 100 messages in the provided Discord text channel. This is to be used to clear the channel associated with the leaderboards.</summary>
    /// <param name="channel">The channel to wipe</param>
    public static async Task WipeLeaderboardsChannel(SocketTextChannel channel) {
        List<IMessage> oldMessages = new List<IMessage>();
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> pagesAsync = channel.GetMessagesAsync(100);
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

    /// <summary>Updates the leaderboards. This task covers everything necessary after the successful execution of the command to update the leaderboards.</summary>
    public static async Task UpdateLeaderboards() {
        // Performing some setup:
        HBData.LeaderboardRankings.Clear();
        SocketTextChannel lbChannel = (SocketTextChannel)HBClients.DiscordClient.GetChannel(HBConfig.LeaderboardsChannelID);
        WipeLeaderboards();

        // Requesting and processing guild information:
        Json? guild = await HypixelMethods.RetrieveGuildAPI(HBConfig.HypixelGuildID);
        Task initialBuffer = Task.Delay(1000);
        if (guild == null || guild.GetBoolean("success") == false) {
            await HBData.Log.InfoAsync("ERROR: Guild information retrieval unsuccessful :(");
            return;
        }
        List<JsonElement>? members = guild.GetArray("guild.members");
        if (members == null || members.Count == 0) {
            await HBData.Log.InfoAsync("ERROR: Guild member list retrieval unsuccessful :(");
            return;
        }

        // Getting the members' UUIDs and guild quest participations from the JSON array:
        Dictionary<string, int> questParticipationsMap = new Dictionary<string, int>();
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
                questParticipationsMap.Add(uuidString, (int)questParticipations.GetDouble());
            }
        }

        // Starting to wipe old leaderboards:
        Task channelWipeTask = WipeLeaderboardsChannel(lbChannel);

        // Requesting player data and populating leaderboards:
        GuildQuestChallengesCompletedLeaderboard guildQuestChallengesCompletedLeaderboard = (GuildQuestChallengesCompletedLeaderboard)
            (HBAssets.LeaderboardList.Find(x => x is GuildQuestChallengesCompletedLeaderboard) ?? new GuildQuestChallengesCompletedLeaderboard());
        guildQuestChallengesCompletedLeaderboard.QuestParticipationsMap = questParticipationsMap;
        await initialBuffer;
        foreach (string uuid in uuids) {
            Task buffer = Task.Delay(1000);
            Json? json = await HypixelMethods.RetrievePlayerAPI(uuid, uuid: true);
            if (json == null || json.GetBoolean("success") == false || json.GetValueKind("player") != JsonValueKind.Object)
                await HBData.Log.InfoAsync("Ignoring player " + uuid + " in leaderboards update");
            else {
                HBData.LeaderboardRankings.Add((json.GetString("player.displayname") ?? "?????").ToLower(), new LBRankingData(json));
                foreach (Leaderboard leaderboard in HBAssets.LeaderboardList) {
                    if (leaderboard is not AverageLeaderboardPositionLeaderboard)
                        leaderboard.EnterPlayer(json);
                }
            }
            await buffer;
        }
        guildQuestChallengesCompletedLeaderboard.QuestParticipationsMap = null;

        // Refreshing rankings cache:
        RefreshRankings();

        // Populating the leaderboard for Average Leaderboard Position:
        AverageLeaderboardPositionLeaderboard averageLeaderboardPositionLeaderboard = (AverageLeaderboardPositionLeaderboard)
            (HBAssets.LeaderboardList.Find(x => x is AverageLeaderboardPositionLeaderboard) ?? new AverageLeaderboardPositionLeaderboard());
        foreach (LBRankingData player in HBData.LeaderboardRankings.Values)
            averageLeaderboardPositionLeaderboard.EnterPlayer(player);
        RefreshRankings(averageLeaderboardPositionLeaderboard);
            

        // Awaiting the completion of leaderboards wiping:
        await channelWipeTask;

        // Post new leaderboards:
        foreach (Leaderboard leaderboard in HBAssets.LeaderboardList) {
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
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> pagesAsync = lbChannel.GetMessagesAsync(HBAssets.LeaderboardList.Count);
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
        IAsyncEnumerable<IReadOnlyCollection<IMessage>> pagesPinsAsync = lbChannel.GetMessagesAsync(HBAssets.LeaderboardList.Count);
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
        done.WithDescription("The leaderboards are finished updating.").WithColor(Color.Green).WithCurrentTimestamp();
        await lbChannel.SendMessageAsync(embed: done.Build());
    }

    /// <summary>Updates the rankings in <see cref="HBData.LeaderboardRankings"/> for all leaderboards.</summary>
    public static void RefreshRankings() {
        foreach (Leaderboard leaderboard in HBAssets.LeaderboardList)
            RefreshRankings(leaderboard);
    }

    /// <summary>Updates the rankings in <see cref="HBData.LeaderboardRankings"/> for the provided leaderboard.</summary>
    /// <param name="leaderboard">The leaderboard for which to update the rankings</param>
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

    /// <summary>Updates the rankings in <see cref="HBData.LeaderboardRankings"/> for all leaderboards based on the existing threads in the provided Discord text channel.</summary>
    /// <param name="channel">The leaderboards channel from which to retrieve the rankings</param>
    public static async Task RefreshRankingsFromChannelAsync(SocketTextChannel channel) {
        List<IMessage> messages = new List<IMessage>();
        List<IReadOnlyCollection<IMessage>> pages = await channel.GetMessagesAsync(HBAssets.LeaderboardList.Count + 5).ToListAsync();
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
            Leaderboard? leaderboard = HBAssets.LeaderboardList.Find(x => x.GameTitle.Equals(embed.Title) && x.GameStat.Equals(embed.Description ?? ""));
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

    /// <summary>Updates the rankings in <see cref="HBData.LeaderboardRankings"/> for the given leaderboard based on the existing messages in the provided Discord thread.</summary>
    /// <param name="leaderboard">The leaderboard for which to update the rankings</param>
    /// <param name="thread">The Discord thread from which the leaderboard state should be retrieved</param>
    /// <param name="initializePlayers">True if this task should initialize the players</param>
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

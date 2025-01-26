using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Ranks;
using HeinzBOTtle.Requirements;
using HeinzBOTtle.Statics;
using MySqlConnector;
using System.Reflection;
using System.Text.Json;
using ConnectionState = System.Data.ConnectionState;

namespace HeinzBOTtle.Database;

public static class DBMethods {

    /// <summary>Reconnects to the database if the connection is not currently open.</summary>
    /// <returns>Whether the connection is open after attempting to open it.</returns>
    public static async Task<bool> EnsureConnectionAsync() {
        try {
            await HBClients.DatabaseConnection.PingAsync();
        } catch { }
        if (HBClients.DatabaseConnection.State == ConnectionState.Closed || HBClients.DatabaseConnection.State == ConnectionState.Broken) {
            await HBClients.DatabaseConnection.OpenAsync();
            for (int i = 0; i < 5; i++) {
                await Task.Delay(100);
                try {
                    bool success = await HBClients.DatabaseConnection.PingAsync();
                    if (!success) {
                        await HBData.Log.InfoAsync($"Failure in database ping attempt #{i + 1}");
                        continue;
                    }
                    break;
                } catch (Exception ex) {
                    await HBData.Log.InfoAsync($"Error in database ping attempt #{i + 1}: {ex.Message}\n{ex.StackTrace}");
                    continue;
                }
            }
        }
        return HBClients.DatabaseConnection.State == ConnectionState.Open;
    }
    
    /// <param name="user">The user whose summary to generate</param>
    /// <param name="cachedMCUsername">The proper Minecraft username of the linked Minecraft account, to be provided if readily available</param>
    /// <returns>A text summary of the information stored for the provided user. If retrieval is unsuccessful, null.</returns>
    public static async Task<string?> GenerateUserSummaryAsync(DBUser user, string? cachedMCUsername = null) {
        (ulong enrollmentTimestamp, ulong? discordUserID, string? minecraftUUID, Color? signatureColor, ulong flags)? info = await user.GetAllAsync();
        if (info == null)
            return null;
        string minecraft = "Not Linked";
        if (info.Value.minecraftUUID != null) {
            if (cachedMCUsername == null) {
                Json json = await HypixelMethods.RetrievePlayerAPI(info.Value.minecraftUUID, uuid: true);
                if (!(json.GetBoolean("success") ?? false) || json.GetValueKind("player") != JsonValueKind.Object)
                    minecraft = $"API error, but UUID is: {HypixelMethods.ToDashedUUID(info.Value.minecraftUUID)}";
                else
                    minecraft = $"**{(json.GetString("player.displayname") ?? "?????").Replace("_", "\\_")}** ({HypixelMethods.ToDashedUUID(info.Value.minecraftUUID)})";
            } else
                minecraft = $"**{cachedMCUsername.Replace("_", "\\_")}** ({HypixelMethods.ToDashedUUID(info.Value.minecraftUUID)})";
        }
        ulong f = info.Value.flags;
        return  $"ID: {user.ID}" +
                $"\nDiscord: <@{(info.Value.discordUserID != null ? info.Value.discordUserID : "Not Linked")}>" +
                $"\nMinecraft: {minecraft}" +
                $"\nSignature Color: {(info.Value.signatureColor != null ? $"`#{info.Value.signatureColor.Value.RawValue:x6}`" : "Not Set")}" +
                $"\nHighest Recorded Non-Staff Rank: {(Rank)(f & 0b111)}" +
                $"\nTreehard Level: {((TreehardLevel)(f & 0b11000)).ToString().Replace("Plus", "+")}" +
                $"\nHonorary Quest Status: {((f & 1 << 5) == 1 << 5 ? "Granted" : "Not Granted")}";
    }

    /// <param name="uuid">The UUID of the user's corresponding Minecraft account</param>
    /// <returns>The signature color of the user linked to the Minecraft account identified by the UUID.
    /// If the account is unlinked or the user has no signature color, null.</returns>
    public static async Task<Color?> FindReplacementColorAsync(string uuid) {
        DBUser? user = await DBUser.FromMinecraftUUIDAsync(uuid);
        if (user == null)
            return null;
        return await user.Value.GetSignatureColorAsync();
    }

    /// <summary>Sends or modifies a message prompting for approval or denial of a Minecraft account link. Does not update the database.</summary>
    /// <param name="discordID">The Discord ID of the Discord account pending linkage to the Minecraft account</param>
    /// <param name="uuid">The UUID of the Minecraft account pending linkage to the Discord account</param>
    /// <param name="username">The Minecraft username of the Minecraft account</param>
    /// <param name="existingMessage">The ID of the existing prompting message to be modified, if any</param>
    /// <returns>The ID of the prompting message that was either created or modified.</returns>
    public static async Task<ulong> UpdateReviewQueue(ulong discordID, string uuid, string username, ulong? existingMessage = null) {
        SocketTextChannel channel = (SocketTextChannel)await HBClients.DiscordClient.GetChannelAsync(HBConfig.ReviewChannelID);
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithTitle("Link Request");
        embed.WithDescription($"Discord: <@{discordID}>\nMinecraft: **{username.Replace("_", "\\_")}** ({HypixelMethods.ToDashedUUID(uuid)})");
        embed.WithColor(Color.Blue);
        ulong messageID;
        if (existingMessage == null) {
            string compressedID = Convert.ToBase64String(BitConverter.GetBytes(discordID));
            ButtonBuilder approve = new ButtonBuilder().WithLabel("Approve").WithCustomId($"linkrequest/approve {compressedID}").WithStyle(ButtonStyle.Success);
            ButtonBuilder deny = new ButtonBuilder().WithLabel("Deny").WithCustomId($"linkrequest/deny {compressedID}").WithStyle(ButtonStyle.Danger);
            ComponentBuilder buttons = new ComponentBuilder().WithButton(approve).WithButton(deny);
            messageID = (await channel.SendMessageAsync(embed: embed.Build(), components: buttons.Build())).Id;
        } else
            messageID = (await channel.ModifyMessageAsync((ulong)existingMessage, delegate (MessageProperties p) {
                p.Embed = embed.Build();
            })).Id;
        return messageID;
    }

    /// <summary>Processes the click of the Approve/Deny button of a link request message, deleting and updating the message and the database as necessary.</summary>
    /// <param name="button">The event corresponding to the button being clicked</param>
    public static async Task HandleLinkButton(SocketMessageComponent button) {
        ulong clicker = button.User.Id;
        string[] id = button.Data.CustomId.Split(' ');
        ulong discordID = BitConverter.ToUInt64(Convert.FromBase64String(id[1]), 0);
        string uuid = "";
        ulong messageID = 0uL;
        using (MySqlCommand cmd = new MySqlCommand("SELECT MinecraftUUID, ReviewMessageID FROM LinkPending WHERE DiscordUserID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
                uuid = reader.GetString(0);
                messageID = reader.GetUInt64(1);
            }
        }
        SocketTextChannel channel = (SocketTextChannel)await HBClients.DiscordClient.GetChannelAsync(HBConfig.ReviewChannelID);
        IMessage message = await channel.GetMessageAsync(messageID);
        bool approved = false;
        string desc = "";
        switch (id[0]) {
            case "linkrequest/approve":
                DBUser? existingUser = await DBUser.FromDiscordIDAsync(discordID);
                Json json = await HypixelMethods.RetrievePlayerAPI(uuid, uuid: true);
                bool success = json.GetBoolean("success") ?? false;
                if (!success) {
                    await message.RemoveAllReactionsAsync();
                    await message.AddReactionAsync(new Emoji("⚠️"));
                    return;
                }
                string username = json.GetString("player.displayname") ?? "?????";
                if (existingUser == null) {
                    (DBUser? newUser, byte code) result = await DBUser.EnrollAsync(discordID, uuid, username);
                    if (result.code != 0) {
                        await message.RemoveAllReactionsAsync();
                        await message.AddReactionAsync(new Emoji("❌"));
                        return;
                    }
                } else {
                    byte code = await existingUser.Value.SetMinecraftUUIDAsync(uuid);
                    if (code != 0) {
                        await message.RemoveAllReactionsAsync();
                        await message.AddReactionAsync(new Emoji("❌"));
                        return;
                    }
                }
                desc = message.Embeds.First().Description;
                await message.DeleteAsync();
                approved = true;
                break;
            case "linkrequest/deny":
                desc = message.Embeds.First().Description;
                await message.DeleteAsync();
                break;
        }
        EmbedBuilder logEmbed = new EmbedBuilder();
        logEmbed.WithCurrentTimestamp();
        logEmbed.WithTitle("Link request handled");
        logEmbed.WithDescription($"{desc}\n\n{(approved ? "Approved" : "Denied")} by <@{clicker}>");
        logEmbed.WithColor(approved ? Color.Green : Color.Red);
        SocketTextChannel logsChannel = (SocketTextChannel)HBClients.DiscordClient.GetChannel(HBConfig.LogsChannelID);
        await logsChannel.SendMessageAsync(embed: logEmbed.Build());
        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM LinkPending WHERE DiscordUserID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>Updates information stored for the provided user based on any new information and
    /// assigns appropriate Discord roles if a Discord account is linked.</summary>
    /// <param name="user"></param>
    /// <returns>A code indicating an error, if any, otherwise 0.</returns>
    public static async Task<byte> UpdateUserAsync(DBUser user) {
        (ulong enrollmentTimestamp, ulong? discordUserID, string? minecraftUUID, Color? signatureColor, byte flags)? data = await user.GetAllAsync();
        if (data == null)
            return 1; // Code 1: Invalid user
        string uuid = data.Value.minecraftUUID ?? "";
        if (uuid == "")
            return 2; // Code 2: Minecraft not linked
        Json json = await HypixelMethods.RetrievePlayerAPI(uuid, uuid: true);

        Json? guild = await HypixelMethods.RetrieveGuildAPI(HBConfig.HypixelGuildID);
        Task initialBuffer = Task.Delay(1000);
        if (guild == null || guild.GetBoolean("success") == false) {
            await HBData.Log.InfoAsync("ERROR: Guild information retrieval unsuccessful :(");
            return 3; // Code 3: API problem with guild
        }
        List<JsonElement>? members = guild.GetArray("guild.members");
        if (members == null || members.Count == 0) {
            await HBData.Log.InfoAsync("ERROR: Guild member list retrieval unsuccessful :(");
            return 3; // Code 3: API problem with guild
        }

        bool isGuildMember = false;
        Rank rank = Rank.None;
        foreach (JsonElement member in members) {
            if (member.ValueKind != JsonValueKind.Object)
                continue;

            Dictionary<string, JsonElement>? memberDic;
            try {
                memberDic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(member);
            } catch {
                memberDic = null;
            }

            if (memberDic == null || !memberDic.ContainsKey("uuid") || !memberDic.ContainsKey("joined") || !memberDic.ContainsKey("rank"))
                continue;
            string? uuidMember = memberDic["uuid"].GetString();
            if (uuidMember == null)
                continue;

            if (uuid == uuidMember) {
                isGuildMember = true;
                Enum.TryParse(typeof(Rank), memberDic["rank"].GetString(), true, out object? foundRank);
                rank = (Rank?)foundRank ?? Rank.Member;
                TimeSpan? timeInGuild = RankMethods.TimeSinceTimestamp(memberDic["joined"].GetInt64(), DateTimeOffset.Now);
                rank = (Rank)byte.Max((byte)rank, (byte)RankMethods.FindBestEligibleRank(json, timeInGuild != null ? timeInGuild.Value : TimeSpan.Zero, (Rank)(data.Value.flags & 0b111)));
            }
        }
        rank = (Rank)byte.Max((byte)rank, (byte)(data.Value.flags & 0b111));
        
        SocketGuildUser? discord = data.Value.discordUserID != null ? HBClients.DiscordClient.GetGuild(HBConfig.DiscordGuildID).GetUser((ulong)data.Value.discordUserID) : null;

        TreehardLevel treehardLevel = (TreehardLevel)(data.Value.flags & 0b11000);
        bool hqStatus = (data.Value.flags & 1 << 5) != 0;
        bool discordUpdateSuccessful = true;
        if (discord != null) {
            int bestLeaderboardPosition = await HypixelMethods.BestLeaderboardPositionAsync(uuid);
            List<ulong> toAdd = new List<ulong>();
            List<ulong> toRemove = new List<ulong>();
            List<ulong> reqRolesPending = new List<ulong>();
            bool hasGuest = false;
            bool hasGuildMember = false;
            bool hasHonoraryQuest = false;
            bool hasTreehard = false;
            bool hasTreehardPlus = false;
            bool hasChallenger = false;
            bool hasLeaderboarder = false;
            List<ulong> reqRoles = new List<ulong>();
            foreach (Requirement req in HBAssets.RequirementList)
                reqRoles.Add(HBData.RoleMap[req.Title]);
            if (isGuildMember) {
                foreach (Requirement req in ReqMethods.GetRequirementsMet(json))
                    reqRolesPending.Add(HBData.RoleMap[req.Title]);
            }
            foreach (SocketRole role in discord.Roles) {
                if (!isGuildMember && reqRoles.Contains(role.Id))
                    toRemove.Add(role.Id);
                else if (isGuildMember && reqRolesPending.Contains(role.Id))
                    reqRolesPending.Remove(role.Id);
                else if (role.Id == HBConfig.GuestRoleID)
                    hasGuest = true;
                else if (role.Id == HBConfig.GuildMemberRoleID)
                    hasGuildMember = true;
                else if (role.Id == HBConfig.HonoraryQuestRoleID) {
                    hasHonoraryQuest = true;
                    hqStatus = true;
                } else if (role.Id == HBConfig.TreehardRoleID) {
                    hasTreehard = true;
                    treehardLevel = (TreehardLevel)byte.Max((byte)treehardLevel, (byte)TreehardLevel.Treehard);
                } else if (role.Id == HBConfig.TreehardPlusRoleID) {
                    hasTreehardPlus = true;
                    treehardLevel = (TreehardLevel)byte.Max((byte)treehardLevel, (byte)TreehardLevel.TreehardPlus);
                } else if (role.Id == HBConfig.ChallengerRoleID)
                    hasChallenger = true;
                else if (role.Id == HBConfig.LeaderboarderRoleID)
                    hasLeaderboarder = true;
            }
            foreach (ulong req in reqRolesPending)
                toAdd.Add(req);
            if (isGuildMember) {
                if (hasGuest)
                    toRemove.Add(HBConfig.GuestRoleID);
                if (!hasGuildMember)
                    toAdd.Add(HBConfig.GuildMemberRoleID);
                if (hasHonoraryQuest)
                    toRemove.Add(HBConfig.HonoraryQuestRoleID);
            } else {
                if (hasGuildMember)
                    toRemove.Add(HBConfig.GuildMemberRoleID);
                if (hqStatus) {
                    if (hasGuest)
                        toRemove.Add(HBConfig.GuestRoleID);
                    if (!hasHonoraryQuest)
                        toAdd.Add(HBConfig.HonoraryQuestRoleID);
                } else {
                    if (!hasGuest)
                        toAdd.Add(HBConfig.GuestRoleID);
                    if (hasHonoraryQuest)
                        toRemove.Add(HBConfig.HonoraryQuestRoleID);
                }
            }
            switch (treehardLevel) {
                case TreehardLevel.None:
                    if (hasTreehard)
                        toRemove.Add(HBConfig.TreehardRoleID);
                    if (hasTreehardPlus)
                        toRemove.Add(HBConfig.TreehardPlusRoleID);
                    break;
                case TreehardLevel.Treehard:
                    if (!hasTreehard)
                        toAdd.Add(HBConfig.TreehardRoleID);
                    if (hasTreehardPlus)
                        toRemove.Add(HBConfig.TreehardPlusRoleID);
                    break;
                case TreehardLevel.TreehardPlus:
                    if (hasTreehard)
                        toRemove.Add(HBConfig.TreehardRoleID);
                    if (!hasTreehardPlus)
                        toAdd.Add(HBConfig.TreehardPlusRoleID);
                    break;
            }
            if (bestLeaderboardPosition >= 1 && bestLeaderboardPosition <= 100 && !hasChallenger)
                toAdd.Add(HBConfig.ChallengerRoleID);
            if (bestLeaderboardPosition >= 1 && bestLeaderboardPosition <= 10 && !hasLeaderboarder)
                toAdd.Add(HBConfig.LeaderboarderRoleID);
            try {
                await discord.RemoveRolesAsync(toRemove);
                await discord.AddRolesAsync(toAdd);
            } catch {
                discordUpdateSuccessful = false;
            }
        }
        byte newFlags = (byte)((data.Value.flags & 0b11000000) | (byte)rank | (byte)treehardLevel | (hqStatus ? 1 << 5 : 0));
        if (data.Value.flags != newFlags)
            await user.SetFlagsAsync(newFlags);
        if (!discordUpdateSuccessful)
            return 4; // Code 4: Role update failed, flag update successful
        return 0; // Code 0: Success
    }

    /// <summary>Checks if the database structure should be modified in the event that a new version of the software requires it,
    /// modifying it appropriately.</summary>
    public static async Task UpdateDatabaseSchemaAsync() {
        using (MySqlCommand cmd0 = new MySqlCommand("CREATE TABLE IF NOT EXISTS GlobalData (" +
                                                       "\n    ID VARCHAR(32) PRIMARY KEY NOT NULL," +
                                                       "\n    CurrentValue BIGINT UNSIGNED" +
                                                       "\n)", HBClients.DatabaseConnection))
            await cmd0.ExecuteNonQueryAsync();
        ulong currentSchemaVersion = ulong.MaxValue;
        using (MySqlCommand cmd1 = new MySqlCommand("SELECT DISTINCT CurrentValue FROM GlobalData WHERE ID = \"SchemaVersion\"", HBClients.DatabaseConnection)) {
            using MySqlDataReader reader = await cmd1.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                currentSchemaVersion = reader.GetUInt64(0);
        }
        if (currentSchemaVersion == ulong.MaxValue) {
            using (MySqlCommand cmd2 = new MySqlCommand("INSERT INTO GlobalData VALUES (\"SchemaVersion\", 0)", HBClients.DatabaseConnection))
                await cmd2.ExecuteNonQueryAsync();
            currentSchemaVersion = 0;
        }
        string scriptText;
        using (Stream? scriptStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HeinzBOTtle.Database.SchemaUpdateScript.sql")) {
            if (scriptStream == null) {
                await HBData.Log.InfoAsync("The update script cannot be loaded; update failed.");
                return;
            }
            using (StreamReader file = new StreamReader(scriptStream))
                scriptText = await file.ReadToEndAsync();
        }
        string script = scriptText.Trim();
        ulong targetSchemaVersion = ulong.Parse(script[19..script.IndexOf('\n', 19)]);
        if (currentSchemaVersion == targetSchemaVersion)
            return;
        await HBData.Log.InfoAsync($"Updating database schema...\nCurrent Schema Version: {currentSchemaVersion}\nTarget Schema Version: {targetSchemaVersion}");
        for (ulong i = currentSchemaVersion + 1; i <= targetSchemaVersion; i++) {
            int start = script.IndexOf($"\n-- [{i}]\n");
            int end = script.IndexOf($"\n-- END", start);
            if (start == -1 || end == -1 || start >= end) {
                await HBData.Log.InfoAsync($"Unable to find update {i}; update aborted.");
                return;
            }
            await HBData.Log.InfoAsync($"Applying update {i}...");
            using (MySqlCommand cmd2 = new MySqlCommand(script[start..end].Trim(), HBClients.DatabaseConnection))
                await cmd2.ExecuteNonQueryAsync();
        }
        await HBData.Log.InfoAsync("Database schema update done!");
    }

}

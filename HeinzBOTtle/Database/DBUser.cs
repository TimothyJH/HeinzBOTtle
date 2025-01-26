using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Statics;
using MySqlConnector;

namespace HeinzBOTtle.Database;

public struct DBUser {

    public uint ID;

    public DBUser(uint ID) {
        this.ID = ID;
    }

    public async Task<bool> Exists() {
        using (MySqlCommand cmd = new MySqlCommand("SELECT EXISTS(SELECT ID FROM Users WHERE ID = @a)", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (reader.GetBoolean(0))
                    return true;
        }
        return false;
    }

    public async Task<ulong> GetEnrollmentTimestampAsync() {
        using (MySqlCommand cmd = new MySqlCommand("SELECT EnrollmentTimestamp FROM Users WHERE ID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return reader.GetUInt64(0);
        }
        return 0uL;
    }

    public async Task<byte> SetDiscordUserIDAsync(ulong? discordID, ulong? modifier = null) {
        using (MySqlCommand cmd = new MySqlCommand("SELECT EXISTS(SELECT ID FROM Users WHERE DiscordUserID = @a)", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (reader.GetBoolean(0))
                    return 1; // Code 1: Discord account already in use
        }
        ulong? oldDiscordID = await GetDiscordUserIDAsync();
        using (MySqlCommand cmd = new MySqlCommand("UPDATE Users SET DiscordUserID = @a WHERE ID = @b", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID == null ? DBNull.Value : discordID);
            cmd.Parameters.AddWithValue("b", ID);
            await cmd.ExecuteNonQueryAsync();
        }
        await SendModifyEmbed(ID, "Discord", oldDiscordID == null ? "None" : $"<@{oldDiscordID}>", discordID == null ? "None" : $"<@{discordID}>", modifier);
        return 0; // Code 0: Success
    }

    public async Task<ulong?> GetDiscordUserIDAsync() {
        using (MySqlCommand cmd = new MySqlCommand("SELECT DiscordUserID FROM Users WHERE ID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return reader.IsDBNull(0) ? null : reader.GetUInt64(0);
        }
        return null;
    }

    public async Task<byte> SetMinecraftUUIDAsync(string? minecraftUUID, ulong? modifier = null) {
        using (MySqlCommand cmd = new MySqlCommand("SELECT EXISTS(SELECT ID FROM Users WHERE MinecraftUUID = @a)", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", minecraftUUID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (reader.GetBoolean(0))
                    return 1; // Code 1: Minecraft account already in use
        }
        string? oldUUID = await GetMinecraftUUIDAsync();
        using (MySqlCommand cmd = new MySqlCommand("UPDATE Users SET MinecraftUUID = @a WHERE ID = @b", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", minecraftUUID == null ? DBNull.Value : minecraftUUID);
            cmd.Parameters.AddWithValue("b", ID);
            await cmd.ExecuteNonQueryAsync();
        }
        await SendModifyEmbed(ID, "Minecraft", oldUUID == null ? "None" : $"({HypixelMethods.ToDashedUUID(oldUUID)})",
            minecraftUUID == null ? "None" : $"({HypixelMethods.ToDashedUUID(minecraftUUID)})", modifier);
        return 0; // Code 0: Success
    }

    public async Task<string?> GetMinecraftUUIDAsync() {
        using (MySqlCommand cmd = new MySqlCommand("SELECT MinecraftUUID FROM Users WHERE ID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return reader.IsDBNull(0) ? null : reader.GetString(0);
        }
        return null;
    }

    public async Task<byte> SetSignatureColorAsync(uint color) {
        using (MySqlCommand cmd = new MySqlCommand("UPDATE Users SET SignatureColor = @a WHERE ID = @b", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", color);
            cmd.Parameters.AddWithValue("b", ID);
            await cmd.ExecuteNonQueryAsync();
        }
        return 0; // Code 0: Success
    }

    public async Task<uint?> GetSignatureColorAsync() {
        using (MySqlCommand cmd = new MySqlCommand("SELECT SignatureColor FROM Users WHERE ID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return reader.IsDBNull(0) ? null : reader.GetUInt32(0);
        }
        return null;
    }

    public async Task<byte> SetFlagsAsync(byte flags, ulong? modifier = null) {
        byte oldFlags = await GetFlagsAsync();
        using (MySqlCommand cmd = new MySqlCommand("UPDATE Users SET Flags = @a WHERE ID = @b", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", flags);
            cmd.Parameters.AddWithValue("b", ID);
            await cmd.ExecuteNonQueryAsync();
        }
        await SendModifyEmbed(ID, "Flags", $"0b{uint.Parse(Convert.ToString(oldFlags, 2)):D8}", $"0b{uint.Parse(Convert.ToString(flags, 2)):D8}", modifier);
        return 0; // Code 0: Success
    }

    public async Task<byte> GetFlagsAsync() {
        using (MySqlCommand cmd = new MySqlCommand("SELECT Flags FROM Users WHERE ID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return reader.GetByte(0);
        }
        return 0xFF;
    }

    public async Task<(ulong enrollmentTimestamp, ulong? discordUserID, string? minecraftUUID, Color? signatureColor, byte flags)?> GetAllAsync() {
        using (MySqlCommand cmd = new MySqlCommand("SELECT EnrollmentTimestamp, DiscordUserID, MinecraftUUID, SignatureColor, Flags FROM Users WHERE ID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return (reader.GetUInt64(0), reader.IsDBNull(1) ? null : reader.GetUInt64(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : new Color(reader.GetUInt32(3)), reader.GetByte(4));
        }
        return null;
    }

    public static async Task<DBUser?> FromDiscordIDAsync(ulong discordID) {
        using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM Users WHERE DiscordUserID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return new DBUser(reader.GetUInt32(0));
        }
        return null;
    }

    public static async Task<DBUser?> FromMinecraftUUIDAsync(string minecraftUUID) {
        using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM Users WHERE MinecraftUUID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", minecraftUUID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                return new DBUser(reader.GetUInt32(0));
        }
        return null;
    }

    public static async Task<(DBUser? newUser, byte code)> EnrollAsync(ulong discordID, string minecraftUUID, string cachedMCUsername) {
        // Making sure that the relevant arguments aren't already being used:
        byte errorCode = 0;
        using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM Users WHERE DiscordUserID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                errorCode |= 1; // Code 1: Discord account already in use
        }
        using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM Users WHERE MinecraftUUID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", minecraftUUID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                errorCode |= 2; // Code 2: Minecraft account already in use
        }
        if (errorCode != 0) // Potential code 3: Both Discord and Minecraft accounts already in use
            return (null, errorCode);

        // Attempting to enroll the new user:
        using (MySqlCommand cmd = new MySqlCommand("INSERT INTO Users(EnrollmentTimestamp, DiscordUserID, MinecraftUUID, Flags) VALUES (@a, @b, @c, @d)", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", DateTimeOffset.Now.ToUnixTimeSeconds());
            cmd.Parameters.AddWithValue("b", discordID);
            cmd.Parameters.AddWithValue("c", minecraftUUID);
            cmd.Parameters.AddWithValue("d", 0b0u);
            await cmd.ExecuteNonQueryAsync();
        }
        uint newID = uint.MaxValue;
        using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM Users WHERE DiscordUserID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                newID = reader.GetUInt32(0);
        }
        if (newID == uint.MaxValue)
            return (null, 4); // Code 4: Not found in database after entering
        DateTimeOffset ts = DateTimeOffset.Now;

        // Communicating the success:
        SocketTextChannel logsChannel = (SocketTextChannel)HBClients.DiscordClient.GetChannel(HBConfig.LogsChannelID);
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithTitle("User enrolled");
        embed.WithDescription($"ID: {newID}\nDiscord: <@{discordID}>\nMinecraft: **{cachedMCUsername.Replace("_", "\\_")}** ({HypixelMethods.ToDashedUUID(minecraftUUID)})");
        embed.WithColor(Color.Green);
        embed.WithTimestamp(ts);
        await logsChannel.SendMessageAsync(embed: embed.Build());
        DBUser user = new DBUser(newID);
        byte updateCode = await DBMethods.UpdateUserAsync(user);
        if (updateCode == 4)
            return (user, 5); // Code 5: Role update failed, flag update successful
        if (updateCode != 0)
            return (user, 6); // Code 6: Other update failure
        return (user, 0); // Code 0: Success
    }

    public async Task<byte> DeleteAsync(ulong? modifier = null) {
        using (MySqlCommand cmd = new MySqlCommand("SELECT EXISTS(SELECT ID FROM Users WHERE ID = @a)", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (!reader.GetBoolean(0))
                    return 1; // Code 1: User nonexistent
        }
        string? summary = await DBMethods.GenerateUserSummaryAsync(this);
        if (summary == null)
            return 2; // Code 2: Error in data retrieval
        ulong enrollmentTimestamp = await GetEnrollmentTimestampAsync();
        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM Users WHERE ID = @a", HBClients.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", ID);
            await cmd.ExecuteNonQueryAsync();
        }
        DateTimeOffset ts = DateTimeOffset.Now;

        SocketTextChannel logsChannel = (SocketTextChannel)HBClients.DiscordClient.GetChannel(HBConfig.LogsChannelID);
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithTitle("User deleted");
        embed.WithDescription($"Created: <t:{enrollmentTimestamp}:F>\n{summary}{(modifier != null ? $"\n\nDeleted by <@{modifier}>" : "")}");
        embed.WithColor(Color.Red);
        embed.WithTimestamp(ts);
        await logsChannel.SendMessageAsync(embed: embed.Build());
        return 0; // Code 0: Success
    }

    private static async Task SendModifyEmbed(uint id, string field, string oldValue, string newValue, ulong? modifier) {
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithCurrentTimestamp();
        embed.WithTitle("User modified");
        embed.WithDescription($"ID: {id}\n\nOld {field}: {oldValue}\nNew {field}: {newValue}{(modifier != null ? $"\n\nModified by <@{modifier}>" : "")}");
        embed.WithColor(Color.Blue);
        embed.Build();
        SocketTextChannel logsChannel = (SocketTextChannel)HBClients.DiscordClient.GetChannel(HBConfig.LogsChannelID);
        await logsChannel.SendMessageAsync(embed: embed.Build());
    }

}

/* FLAG BIT ASSIGNMENT REFERENCE:
 * 0-2: Highest achieved in-game Rank (based on values for Rank enum) 
 * 3-4: Highest Treehard rank achieved (based on values for TreehardLevel enum)
 * 5: Whether Honorary Quest status is granted
 */

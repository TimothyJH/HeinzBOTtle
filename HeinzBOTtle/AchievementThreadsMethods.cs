using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Database;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Statics;
using System.Text.Json;

namespace HeinzBOTtle;

public static class AchievementThreadsMethods {

    /// <summary>Processes messages in the achievements channel after being received by the handler for thread creation.
    /// Will attempt to use the username of the linked player (if any) in the thread title.</summary>
    /// <param name="receivedMessage">The message received in the achievements channel</param>
    public static async Task ProcessAchievementsChannelMessage(SocketUserMessage receivedMessage) {
        Task<bool> connectionTask = DBMethods.EnsureConnectionAsync();
        await Task.Delay(500);
        ITextChannel channel = (ITextChannel)(await HBClients.DiscordClient.GetChannelAsync(HBConfig.AchievementsChannelID));
        IMessage message = await channel.GetMessageAsync(receivedMessage.Id);
        if (message is not IUserMessage userMessage || message.Thread != null || userMessage.Content.Contains("[NoThread]", StringComparison.OrdinalIgnoreCase))
            return;

        string name = message.Author.Username;
        await connectionTask;

        try {
            if (connectionTask.Result) {
                DBUser? existingUser = await DBUser.FromDiscordIDAsync(userMessage.Author.Id);
                string? uuid;
                if (existingUser != null && (uuid = await existingUser.Value.GetMinecraftUUIDAsync()) != null) {
                    Json json = await HypixelMethods.RetrievePlayerAPI(uuid, uuid: true);
                    if ((json.GetBoolean("success") ?? false) && json.GetValueKind("player") == JsonValueKind.Object)
                        name = json.GetString("player.displayname") ?? message.Author.Username;
                }
            } else
                await HBData.Log.InfoAsync("Database connection error during achievement thread creation, skipping...");
        } catch {
            await HBData.Log.InfoAsync("Database or API error during achievement thread creation, skipping...");
        }

        try {
            await channel.CreateThreadAsync($"{name}'s Achievement", message: message);
            await HBData.Log.InfoAsync($"Created achievement thread for user: {message.Author.Username}");
        } catch {
            await HBData.Log.InfoAsync($"Unable to create achievement thread for user: {message.Author.Username}");
        }
    }

}

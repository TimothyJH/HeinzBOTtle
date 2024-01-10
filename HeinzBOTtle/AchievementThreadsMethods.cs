using Discord;
using Discord.WebSocket;

namespace HeinzBOTtle;

public static class AchievementThreadsMethods {

    /// <summary>Processes messages in the achievements channel after being received by the handler for thread creation.</summary>
    /// <param name="receivedMessage">The message received in the achievements channel</param>
    public static async Task ProcessAchievementsChannelMessage(SocketUserMessage receivedMessage) {
        await Task.Delay(500);
        ITextChannel channel = (ITextChannel)(await HBData.DiscordClient.GetChannelAsync(HBData.AchievementsChannelID));
        IMessage message = await channel.GetMessageAsync(receivedMessage.Id);
        if (message is not IUserMessage userMessage || message.Thread != null || userMessage.Content.Contains("[NoThread]", StringComparison.OrdinalIgnoreCase))
            return;
        try {
            await channel.CreateThreadAsync($"{message.Author.Username}'s Achievement", message: message);
            Console.WriteLine($"Created achievement thread for user: {message.Author.Username}");
        } catch {
            Console.WriteLine($"Unable to create achievement thread for user: {message.Author.Username}");
        }
    }

}

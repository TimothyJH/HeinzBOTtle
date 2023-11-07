using Discord;
using Discord.WebSocket;

namespace HeinzBOTtle;

public static class AchievementThreadsMethods {

    public static async Task ProcessAchievementChannelMessage(SocketUserMessage receivedMessage) {
        await Task.Delay(500);
        ITextChannel channel = (ITextChannel)(await HBData.DiscordClient.GetChannelAsync(HBData.AchievementsChannelID));
        IMessage message = await channel.GetMessageAsync(receivedMessage.Id);
        if (!(message.Attachments.Count > 0 || message.Embeds.Count > 0) || message.Thread != null)
            return;
        try {
            await channel.CreateThreadAsync($"{message.Author.Username}'s Achievement", message: message);
            Console.WriteLine($"Created achievement thread for user: {message.Author.Username}");
        } catch {
            Console.WriteLine($"Unable to create achievement thread for user: {message.Author.Username}");
        }
    }

}

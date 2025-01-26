using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Commands;
using HeinzBOTtle.Database;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Statics;
using MySqlConnector;

namespace HeinzBOTtle;

public class EventHandlers {

    // A method name prefix of "M" indicates that the event relates to the database.
    // A method name prefix of "D" indicates that the event relates to Discord.

    public void MInfoMessageEvent(object sender, MySqlInfoMessageEventArgs args) {
        if (args.Errors != null)
            foreach (var error in args.Errors)
                HBData.Log.WriteLineToLog($"{error.Level}/{error.ErrorCode}: {error.Message}", "Database", DateTime.Now);
    }

    public void MStateChangeEvent(object sender, System.Data.StateChangeEventArgs e) {
        HBData.Log.Info($"New database connection state: {e.CurrentState}", reduced: false);
    }

    public Task DLogEvent(LogMessage msg) {
        HBData.Log.WriteLineToLog(msg.Message, "Discord.Net", DateTime.Now);
        if (msg.Exception != null)
            HBData.Log.WriteLineToLog(msg.Exception.ToString(), "Discord.Net", DateTime.Now);
        return Task.CompletedTask;
    }

    public Task DReadyEvent(Semaphore readySignal) {
        HBClients.DiscordClient.SlashCommandExecuted += DSlashCommandExecutedEventAsync;
        HBClients.DiscordClient.MessageReceived += DMessageReceivedEvent;
        HBClients.DiscordClient.ButtonExecuted += DButtonExecutedEventAsync;
        readySignal.Release();
        return Task.CompletedTask;
    }

    public Task DDisconnectedEvent(Exception exception) {
        HypixelMethods.CleanCache();
        return Task.CompletedTask;
    }

    public async Task DSlashCommandExecutedEventAsync(SocketSlashCommand command) {
        string logMessage = $"Command executed: /{command.Data.Name} by {command.User.Username} in #{command.Channel.Name}";
        foreach (SocketSlashCommandDataOption arg in command.Data.Options) {
            logMessage += $"\n   {arg.Name} ({arg.Type}): {arg.Value}";
            if (arg.Type == ApplicationCommandOptionType.SubCommand)
                foreach (SocketSlashCommandDataOption subarg in arg.Options)
                    logMessage += $"\n      {subarg.Name} ({subarg.Type}): {subarg.Value}";
        }
        await HBData.Log.InfoAsync(logMessage);
        HBCommand? hbCommand = HBAssets.HBCommandList.Find(x => command.Data.Name.Equals(x.Name));
        if (hbCommand == null) {
            await command.RespondAsync(embed: (new EmbedBuilder()).WithDescription("?????").Build());
            return;
        }
        if (DateTime.Now.Ticks - hbCommand.LastExecution < 5L * 10000000L) {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription("This command is currently on cooldown; try again in a few seconds.");
            embed.WithColor(Color.Gold);
            await command.RespondAsync(embed: embed.Build());
            return;
        }
        hbCommand.LastExecution = DateTime.Now.Ticks;
        _ = hbCommand.ExecuteCommandSafelyAsync(command);
    }

    public Task DButtonExecutedEventAsync(SocketMessageComponent button) {
        if (button.Data.CustomId.StartsWith("linkrequest/"))
            _ = DBMethods.HandleLinkButton(button);
        return Task.CompletedTask;
    }

    public Task DMessageReceivedEvent(SocketMessage message) {
        if (message.Channel.Id == HBConfig.AchievementsChannelID && message is SocketUserMessage userMessage)
            _ = AchievementThreadsMethods.ProcessAchievementsChannelMessage(userMessage);
        return Task.CompletedTask;
    }

}

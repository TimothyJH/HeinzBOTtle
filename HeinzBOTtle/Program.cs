using Discord;
using Discord.Net;
using Discord.WebSocket;
using HeinzBOTtle.Commands;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Leaderboards;
using HeinzBOTtle.Requirements;

namespace HeinzBOTtle;

internal class Program {

    private Semaphore ReadySignal { get; set; } = new Semaphore(0, 1);

    private static Task Main() => new Program().MainAsync();

    private static bool LoadFileVariables() {
        Json json;
        try {
            json = new Json(File.ReadAllText("variables.json"));
        } catch (Exception e) {
            HBData.Log.Info("Unable to properly access variables file: " + e.Message);
            return false;
        }
        if (json.IsEmpty()) {
            HBData.Log.Info("Invalid variables.json file!");
            return false;
        }

        string? rawHypixelKey = json.GetString("HypixelKey");
        string? rawDiscordToken = json.GetString("DiscordToken");
        string? rawHypixelGuildID = json.GetString("HypixelGuildID");
        string? rawDiscordGuildID = json.GetString("DiscordGuildID");
        string? rawLeaderboardsChannelID = json.GetString("LeaderboardsChannelID");
        string? rawAchievementsChannelID = json.GetString("AchievementsChannelID");
        string? rawLogDestinationPath = json.GetString("LogDestinationPath");
        if (rawHypixelKey == null || rawDiscordToken == null || rawHypixelGuildID == null || rawDiscordGuildID == null
            || rawLeaderboardsChannelID == null || rawAchievementsChannelID == null || rawLogDestinationPath == null) {
            HBData.Log.Info("Invalid variables.json file!");
            return false;
        }
        HBData.HypixelKey = rawHypixelKey;
        HBData.DiscordToken = rawDiscordToken;
        HBData.HypixelGuildID = rawHypixelGuildID;
        HBData.DiscordGuildID = ulong.Parse(rawDiscordGuildID);
        HBData.LeaderboardsChannelID = ulong.Parse(rawLeaderboardsChannelID);
        HBData.AchievementsChannelID = ulong.Parse(rawAchievementsChannelID);
        HBData.LogDestinationPath = rawLogDestinationPath;
        return true;
    }

    private async Task MainAsync() {
        // Verifying log status:
        if (!HBData.Log.SuccessfulSetup) {
            Console.WriteLine("There was an issue when trying to create the log file; program will terminate.");
            return;
        }

        // Loading config:
        await HBData.Log.InfoAsync("Running in: " + Directory.GetCurrentDirectory().ToString());
        if (!LoadFileVariables())
            return;

        // Setting up the Discord client:
        HBData.DiscordClient.Log += DLogEvent;
        HBData.DiscordClient.Ready += DReadyEvent;
        HBData.DiscordClient.Disconnected += DDisconnectedEvent;
        await HBData.DiscordClient.LoginAsync(TokenType.Bot, HBData.DiscordToken);
        await HBData.DiscordClient.StartAsync();

        // Waiting for the Discord client to be ready:
        while (true) {
            if (ReadySignal.WaitOne(30000))
                break;
            await HBData.Log.InfoAsync("The Discord client is taking quite a long time to get ready; perhaps something is wrong?");
        }
        HBData.DiscordClient.Ready -= DReadyEvent;
        ReadySignal.Release();

        // Getting role data from Discord:
        ReqMethods.RefreshRequirementRoles();

        // Running the console client:
        await ConsoleClientAsync();

        // Stopping the program:
        await HBData.DiscordClient.StopAsync();
        HBData.Log.Dispose();
        HBData.Log.Retire(new DirectoryInfo(HBData.LogDestinationPath));
    }

    private Task DLogEvent(LogMessage msg) {
        HBData.Log.WriteLineToLog(msg.Message, "Discord.Net", DateTime.Now);
        if (msg.Exception != null)
            HBData.Log.WriteLineToLog(msg.Exception.ToString(), "Discord.Net", DateTime.Now);
        return Task.CompletedTask;
    }

    private Task DReadyEvent() {
        HBData.DiscordClient.SlashCommandExecuted += DSlashCommandExecutedEventAsync;
        HBData.DiscordClient.MessageReceived += DMessageReceivedEvent;
        ReadySignal.Release();
        return Task.CompletedTask;
    }

    private Task DDisconnectedEvent(Exception exception) {
        HypixelMethods.CleanCache();
        return Task.CompletedTask;
    }

    private async Task DSlashCommandExecutedEventAsync(SocketSlashCommand command) {
        string logMessage = $"Command executed: /{command.Data.Name} by {command.User.Username} in #{command.Channel.Name}";
        foreach (SocketSlashCommandDataOption arg in command.Data.Options) {
            logMessage += $"\n   {arg.Name} ({arg.Type}): {arg.Value}";
        }
        await HBData.Log.InfoAsync(logMessage);
        HBCommand? hbCommand = HBData.HBCommandList.Find(x => command.Data.Name.Equals(x.Name));
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
        _ = hbCommand.ExecuteCommandAsync(command);
    }

    private Task DMessageReceivedEvent(SocketMessage message) {
        if (message.Channel.Id == HBData.AchievementsChannelID && message is SocketUserMessage userMessage)
            _ = AchievementThreadsMethods.ProcessAchievementsChannelMessage(userMessage);
        return Task.CompletedTask;
    }

    private static async Task ConsoleClientAsync() {
        await HBData.Log.InfoAsync("HeinzBOTtle\nType \"help\" for a list of console commands.");
        while (true) {
            string? command = Console.ReadLine();
            if (command == null || command.Equals(""))
                continue;
            await HBData.Log.InfoAsync($"Console command entered: {command}");
            switch (command) {
                case "help":
                    await HBData.Log.InfoAsync("=> shutdown\n=> update-commands\n=> display-cache\n=> clean-cache\n=> get-rankings\n=> release-logs");
                    break;
                case "shutdown":
                    return;
                case "update-commands":
                    await UpdateCommandsAsync();
                    break;
                case "display-cache":
                    DisplayCache();
                    break;
                case "clean-cache":
                    HypixelMethods.CleanCache();
                    break;
                case "get-rankings":
                    await HBData.Log.InfoAsync("Starting rankings retrieval...");
                    HBData.LeaderboardRankings.Clear();
                    await LBMethods.RefreshRankingsFromChannelAsync((SocketTextChannel)HBData.DiscordClient.GetChannel(HBData.LeaderboardsChannelID));
                    await HBData.Log.InfoAsync("Done!");
                    break;
                case "release-logs":
                    await HBData.Log.ReleaseLogsAsync();
                    break;
                default:
                    await HBData.Log.InfoAsync("???");
                    break;
            }
        }
    }

    private static async Task UpdateCommandsAsync() {
        await HBData.Log.InfoAsync("Getting guild info...");
        SocketGuild guild = HBData.DiscordClient.GetGuild(HBData.DiscordGuildID);
        IReadOnlyCollection<SocketApplicationCommand> existingCommands = await guild.GetApplicationCommandsAsync();
        Dictionary<string, SocketApplicationCommand> existingCommandsMap = new Dictionary<string, SocketApplicationCommand>();
        foreach (SocketApplicationCommand existingCommand in existingCommands) {
            if (existingCommand.Type == ApplicationCommandType.Slash)
                existingCommandsMap.Add(existingCommand.Name, existingCommand);
        }
        await HBData.Log.InfoAsync("Performing command updates...");
        foreach (HBCommand command in HBData.HBCommandList) {
            await Task.Delay(1000);
            if (existingCommandsMap.ContainsKey(command.Name)) {
                await existingCommandsMap[command.Name].ModifyAsync(delegate (ApplicationCommandProperties p) {
                    SlashCommandProperties oldProperties = (SlashCommandProperties)p;
                    SlashCommandProperties newProperties = command.GenerateCommandProperties();
                    oldProperties.Name = newProperties.Name;
                    oldProperties.Description = newProperties.Description;
                    oldProperties.IsDefaultPermission = newProperties.IsDefaultPermission;
                    oldProperties.Options = newProperties.Options;
                });
            } else {
                try {
                    await guild.CreateApplicationCommandAsync(command.GenerateCommandProperties());
                } catch (HttpException exception) {
                    await HBData.Log.InfoAsync($"Creation of \"{command.Name}\" command failed: " + exception.Message);
                }
            }

        }
        await HBData.Log.InfoAsync("Done!");
    }

    private static void DisplayCache() {
        HBData.Log.Info("Current Timestamp: " + DateTime.Now.Ticks);
        if (HBData.APICache.Count == 0) {
            HBData.Log.Info("The cache is empty.");
            return;
        }
        string message = "";
        foreach (string username in HBData.APICache.Keys)
            message += $"{username} - {HBData.APICache[username].Timestamp}\n";
        HBData.Log.Info(message.TrimEnd());
    }

}

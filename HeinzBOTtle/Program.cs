using Discord;
using Discord.Net;
using Discord.WebSocket;
using HeinzBOTtle.Commands;
using HeinzBOTtle.Database;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Leaderboards;
using MySqlConnector;

namespace HeinzBOTtle;

internal class Program {

    private Semaphore ReadySignal { get; set; } = new Semaphore(0, 1);

    private static Task Main(string[] args) => new Program().MainAsync(args);

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
        ulong? rawDiscordGuildID = json.GetUInt64("DiscordGuildID");
        ulong? rawLeaderboardsChannelID = json.GetUInt64("LeaderboardsChannelID");
        ulong? rawAchievementsChannelID = json.GetUInt64("AchievementsChannelID");
        ulong? rawLogsChannelID = json.GetUInt64("LogsChannelID");
        string? rawLogDestinationPath = json.GetString("LogDestinationPath");
        string? rawDatabaseLogin = json.GetString("DatabaseLogin");
        ulong? rawReviewChannelID = json.GetUInt64("ReviewChannelID");
        if (rawHypixelKey == null || rawDiscordToken == null || rawHypixelGuildID == null || rawDiscordGuildID == null
                || rawLeaderboardsChannelID == null || rawAchievementsChannelID == null || rawLogsChannelID == null
                || rawLogDestinationPath == null || rawDatabaseLogin == null || rawReviewChannelID == null) {
            HBData.Log.Info("Invalid variables.json file!");
            return false;
        }
        HBData.HypixelKey = rawHypixelKey;
        HBData.DiscordToken = rawDiscordToken;
        HBData.HypixelGuildID = rawHypixelGuildID;
        HBData.DiscordGuildID = (ulong)rawDiscordGuildID;
        HBData.LeaderboardsChannelID = (ulong)rawLeaderboardsChannelID;
        HBData.AchievementsChannelID = (ulong)rawAchievementsChannelID;
        HBData.LogsChannelID = (ulong)rawLogsChannelID;
        HBData.LogDestinationPath = rawLogDestinationPath;
        HBData.DatabaseLogin = rawDatabaseLogin;
        HBData.ReviewChannelID = (ulong)rawReviewChannelID;

        List<string> missingRoles = new List<string>();
        foreach (string roleTitle in HBData.NecessaryRoles) {
            ulong? value = json.GetUInt64($"Roles.{roleTitle}");
            if (value != null)
                HBData.RoleMap[roleTitle] = (ulong)value;
            else
                missingRoles.Add(roleTitle);
        }
        if (missingRoles.Count != 0) {
            string message = "Invalid variables.json file! The following roles are missing from the configuration:";
            foreach (string missing in missingRoles)
                message += $"\n- {missing}";
            HBData.Log.Info(message);
            return false;
        }

        return true;
    }

    private async Task MainAsync(string[] args) {
        // Handling potential arguments:
        if (args.Contains("-r")) {
            await ProgramArguments.R();
            Console.WriteLine("Program is terminating.");
            return;
        }

        // Verifying log status:
        if (!HBData.Log.Start()) {
            Console.WriteLine("There was an issue when trying to create the log file; program will terminate.");
            return;
        }

        // Loading config:
        await HBData.Log.InfoAsync("Running in: " + Directory.GetCurrentDirectory().ToString());
        if (!LoadFileVariables())
            return;

        // Initializing database connection:
        HBData.DatabaseConnection.InfoMessage += MInfoMessageEvent;
        HBData.DatabaseConnection.StateChange += MStateChangeEvent;
        string[] loginParts = HBData.DatabaseLogin.Split(' ');
        if (loginParts.Length != 4) {
            await HBData.Log.InfoAsync("The provided database login information is not properly formatted; program will terminate.");
            HBData.Log.Dispose();
            HBData.Log.Retire(new DirectoryInfo(HBData.LogDestinationPath));
            return;
        }
        string[] serverParts = loginParts[0].Split(':');
        string connectionString = $"Server={serverParts[0]};Database={loginParts[1]};User ID={loginParts[2]};Password={loginParts[3]}";
        if (serverParts.Length == 2)
            connectionString += $";Port={serverParts[1]}";
        HBData.DatabaseConnection.ConnectionString = connectionString;
        await HBData.DatabaseConnection.OpenAsync();
        if (HBData.DatabaseConnection.State != System.Data.ConnectionState.Open) {
            await HBData.Log.InfoAsync("There was a problem when trying to connect to the database; program will terminate.");
            HBData.Log.Dispose();
            HBData.Log.Retire(new DirectoryInfo(HBData.LogDestinationPath));
            return;
        }
        await DBMethods.UpdateDatabaseSchemaAsync();

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

        // Initializing the role convenience variables:
        HBData.GuestRoleID = HBData.RoleMap["Guest"];
        HBData.GuildMemberRoleID = HBData.RoleMap["Guild Member"];
        HBData.HonoraryQuestRoleID = HBData.RoleMap["Honorary Quest"];
        HBData.TreehardRoleID = HBData.RoleMap["Treehard"];
        HBData.TreehardPlusRoleID = HBData.RoleMap["Treehard+"];

        // Running the console client:
        await ConsoleClientAsync();

        // Stopping the program:
        await HBData.DiscordClient.StopAsync();
        await HBData.DatabaseConnection.CloseAsync();
        HBData.Log.Dispose();
        HBData.Log.Retire(new DirectoryInfo(HBData.LogDestinationPath));
    }

    private void MInfoMessageEvent(object sender, MySqlInfoMessageEventArgs args) {
        if (args.Errors != null)
            foreach (var error in args.Errors)
                HBData.Log.WriteLineToLog($"{error.Level}/{error.ErrorCode}: {error.Message}", "Database", DateTime.Now);
    }

    private void MStateChangeEvent(object sender, System.Data.StateChangeEventArgs e) {
        HBData.Log.Info($"New database connection state: {e.CurrentState}", reduced: false);
        /*if (e.CurrentState == System.Data.ConnectionState.Broken || e.CurrentState == System.Data.ConnectionState.Closed) {
            HBData.Log.Info("Database connection closed", reduced: false);
            _ = DBMethods.EnsureConnectionAsync();
        }
        else if (e.CurrentState == System.Data.ConnectionState.Open)
            HBData.Log.Info("Database connection opened", reduced: false);*/
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
        HBData.DiscordClient.ButtonExecuted += DButtonExecutedEventAsync;
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
            if (arg.Type == ApplicationCommandOptionType.SubCommand)
                foreach (SocketSlashCommandDataOption subarg in arg.Options)
                    logMessage += $"\n      {subarg.Name} ({subarg.Type}): {subarg.Value}";
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
        _ = ExecuteCommandSafelyAsync(hbCommand, command);
    }

    private async Task ExecuteCommandSafelyAsync(HBCommand hbCommand, SocketSlashCommand slashCommand) {
        await slashCommand.DeferAsync();
        if (!await DBMethods.EnsureConnectionAsync()) {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription("Oopsies, something went wrong!");
            embed.WithColor(Color.Gold);
            await slashCommand.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = embed.Build();
            });
            return;
        }
        if (hbCommand.ModifiesDatabase) {
            bool clear = HBData.ModifyingDatabase.WaitOne(120000);
            if (!clear) {
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithDescription("Timeout on database modification semaphore acquisition. :(\nTry again later.");
                embed.WithColor(Color.Gold);
                await slashCommand.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = embed.Build();
                });
                return;
            }
        }
        try {
            await hbCommand.ExecuteCommandAsync(slashCommand);
        } catch (Exception e) {
            await HBData.Log.InfoAsync($"An unhandled exception occurred during command execution:\n{e.GetType()}: {e.Message}\n{e.StackTrace}");
        } finally {
            if (hbCommand.ModifiesDatabase)
                HBData.ModifyingDatabase.Release();
        }
    }

    private Task DButtonExecutedEventAsync(SocketMessageComponent button) {
        if (button.Data.CustomId.StartsWith("linkrequest/"))
            _ = DBMethods.HandleLinkButton(button);
        return Task.CompletedTask;
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
                    oldProperties.DefaultMemberPermissions = newProperties.DefaultMemberPermissions;
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

using Discord;
using Discord.Net;
using Discord.WebSocket;
using HeinzBOTtle.Commands;
using HeinzBOTtle.Leaderboards;

namespace HeinzBOTtle {

    class Program {

        private bool IsReady { get; set; } = false;

        private static Task Main() => new Program().MainAsync();

        private static bool LoadFileVariables() {
            Json json;
            try {
                json = new Json(File.ReadAllText("variables.json"));
            } catch (Exception e) {
                Console.WriteLine("Unable to properly access variables file: " + e.Message);
                return false;
            }
            if (json.IsEmpty()) {
                Console.WriteLine("Invalid variables.json file!");
                return false;
            }

            string? rawHypixelKey = json.GetString("HypixelKey");
            string? rawDiscordToken = json.GetString("DiscordToken");
            string? rawHypixelGuildID = json.GetString("HypixelGuildID");
            string? rawDiscordGuildID = json.GetString("DiscordGuildID");
            string? rawLeaderboardsChannelID = json.GetString("LeaderboardsChannelID");
            string? rawAchievementsChannelID = json.GetString("AchievementsChannelID");
            if (rawHypixelKey == null || rawDiscordToken == null || rawHypixelGuildID == null || rawDiscordGuildID == null
                || rawLeaderboardsChannelID == null || rawAchievementsChannelID == null) {
                Console.WriteLine("Invalid variables.json file!");
                return false;
            }
            HBData.HypixelKey = rawHypixelKey;
            HBData.DiscordToken = rawDiscordToken;
            HBData.HypixelGuildID = rawHypixelGuildID;
            HBData.DiscordGuildID = ulong.Parse(rawDiscordGuildID);
            HBData.LeaderboardsChannelID = ulong.Parse(rawLeaderboardsChannelID);
            HBData.AchievementsChannelID = ulong.Parse(rawAchievementsChannelID);
            return true;
        }

        private async Task MainAsync() {
            // Loading config:
            Console.WriteLine("Running in: " + Directory.GetCurrentDirectory().ToString());
            if (!LoadFileVariables())
                return;

            // Setting up the Discord client:
            HBData.DiscordClient.Log += DLogEvent;
            HBData.DiscordClient.Ready += DReadyEvent;
            await HBData.DiscordClient.LoginAsync(TokenType.Bot, HBData.DiscordToken);
            await HBData.DiscordClient.StartAsync();

            // Waiting for the Discord client to be ready:
            while (true) {
                await Task.Delay(250);
                if (IsReady)
                    break;
            }

            // Running the console client:
            await ConsoleClientAsync();

            // Stopping the program:
            await HBData.DiscordClient.StopAsync();
        }

        private Task DLogEvent(LogMessage msg) {
            Console.WriteLine(msg.ToString());
            if (msg.ToString().Trim().EndsWith("Disconnecting"))
                HypixelMethods.CleanPlayerCache();
            return Task.CompletedTask;
        }

        private Task DReadyEvent() {
            HBData.DiscordClient.SlashCommandExecuted += DSlashCommandExecutedEventAsync;
            HBData.DiscordClient.MessageReceived += DMessageReceivedEventAsync;
            IsReady = true;
            return Task.CompletedTask;
        }

        private async Task DSlashCommandExecutedEventAsync(SocketSlashCommand command) {
            Console.WriteLine($"Command executed: /{command.Data.Name} by {command.User.Username} in #{command.Channel.Name}");
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

        private Task DMessageReceivedEventAsync(SocketMessage message) {
            if (message.Channel.Id == HBData.AchievementsChannelID && message is SocketUserMessage)
                _ = AchievementThreadsMethods.ProcessAchievementChannelMessage((SocketUserMessage)message);
            return Task.CompletedTask;
        }

        private static async Task ConsoleClientAsync() {
            Console.WriteLine("HeinzBOTtle\nType \"help\" for a list of console commands.");
            while (true) {
                string? command = Console.ReadLine();
                if (command == null || command.Equals(""))
                    continue;
                switch (command) {
                    case "help":
                        Console.WriteLine("=> shutdown\n=> update-commands\n=> display-player-cache\n=> clean-player-cache\n=> get-rankings");
                        break;
                    case "shutdown":
                        return;
                    case "update-commands":
                        await UpdateCommandsAsync();
                        break;
                    case "display-player-cache":
                        DisplayPlayerCache();
                        break;
                    case "clean-player-cache":
                        HypixelMethods.CleanPlayerCache();
                        break;
                    case "get-rankings":
                        Console.WriteLine("Starting rankings retrieval...");
                        HBData.LeaderboardRankings.Clear();
                        await LBMethods.RefreshRankingsFromChannelAsync((SocketTextChannel)HBData.DiscordClient.GetChannel(HBData.LeaderboardsChannelID));
                        Console.WriteLine("Done!");
                        break;
                    default:
                        Console.WriteLine("???");
                        break;
                }
            }
        }

        private static async Task UpdateCommandsAsync() {
            Console.WriteLine("Getting guild info...");
            SocketGuild guild = HBData.DiscordClient.GetGuild(HBData.DiscordGuildID);
            IReadOnlyCollection<SocketApplicationCommand> existingCommands = await guild.GetApplicationCommandsAsync();
            Dictionary<string, SocketApplicationCommand> existingCommandsMap = new Dictionary<string, SocketApplicationCommand>();
            foreach (SocketApplicationCommand existingCommand in existingCommands) {
                if (existingCommand.Type == ApplicationCommandType.Slash)
                    existingCommandsMap.Add(existingCommand.Name, existingCommand);
            }
            Console.WriteLine("Performing command updates...");
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
                        Console.WriteLine($"Creation of \"{command.Name}\" command failed: " + exception.Message);
                    }
                }

            }
            Console.WriteLine("Done!");
        }

        private static void DisplayPlayerCache() {
            Console.WriteLine("Current Timestamp: " + DateTime.Now.Ticks);
            if (HBData.PlayerCache.Count == 0) {
                Console.WriteLine("The cache is empty.");
                return;
            }
            foreach (string username in HBData.PlayerCache.Keys)
                Console.WriteLine(username + " - " + HBData.PlayerCache[username].Timestamp);
        }

    }

}

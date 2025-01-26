using Discord;
using Discord.Net;
using Discord.WebSocket;
using HeinzBOTtle.Commands;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Leaderboards;
using HeinzBOTtle.Statics;
using System.Text.Json;

namespace HeinzBOTtle;

public static class ConsoleMethods {

    /// <summary>Provides a interface for the program operator to input console-specific commands via standard input.</summary>
    public static async Task ConsoleClientAsync() {
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
                    await LBMethods.RefreshRankingsFromChannelAsync((SocketTextChannel)HBClients.DiscordClient.GetChannel(HBConfig.LeaderboardsChannelID));
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

    /// <summary>Registers slash command configurations with the Discord guild.</summary>
    private static async Task UpdateCommandsAsync() {
        await HBData.Log.InfoAsync("Getting guild info...");
        SocketGuild guild = HBClients.DiscordClient.GetGuild(HBConfig.DiscordGuildID);
        IReadOnlyCollection<SocketApplicationCommand> existingCommands = await guild.GetApplicationCommandsAsync();
        Dictionary<string, SocketApplicationCommand> existingCommandsMap = new Dictionary<string, SocketApplicationCommand>();
        foreach (SocketApplicationCommand existingCommand in existingCommands) {
            if (existingCommand.Type == ApplicationCommandType.Slash)
                existingCommandsMap.Add(existingCommand.Name, existingCommand);
        }
        await HBData.Log.InfoAsync("Performing command updates...");
        foreach (HBCommand command in HBAssets.HBCommandList) {
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

    /// <summary>Displays each cached API request (endpoint and argument(s)) and its time of retrieval (in "ticks").</summary>
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

    /// <summary>Handles the operation of the program with the -r flag, inidcating that the program should collect role IDs from the server and then terminate.</summary>
    public static async Task UseRFlag() {
        // Getting necessary config:
        Json json;
        try {
            json = new Json(File.ReadAllText("config.json"));
        } catch (Exception e) {
            Console.WriteLine("Unable to properly access configuration file: " + e.Message);
            return;
        }
        if (json.IsEmpty()) {
            Console.WriteLine("Values for DiscordToken and DiscordGuildID must be present in the config.json file to use the -r flag.");
            return;
        }
        string? rawDiscordToken = json.GetString("DiscordToken");
        ulong? rawDiscordGuildID = json.GetUInt64("DiscordGuildID");
        if (rawDiscordToken == null || rawDiscordGuildID == null) {
            Console.WriteLine("Values for DiscordToken and DiscordGuildID must be present in the config.json file to use the -r flag.");
            return;
        }

        // Setting up the Discord client:
        DiscordSocketClient discordClient = new DiscordSocketClient();
        await discordClient.LoginAsync(TokenType.Bot, rawDiscordToken);
        await discordClient.StartAsync();
        Semaphore wait = new Semaphore(0, 1);
        discordClient.Ready += delegate {
            wait.Release();
            return Task.CompletedTask;
        };
        while (true) {
            if (wait.WaitOne(30000))
                break;
            Console.WriteLine("The Discord client is taking quite a long time to get ready; perhaps something is wrong?");
        }
        SocketGuild guild = discordClient.GetGuild((ulong)rawDiscordGuildID);

        // Mapping the roles
        List<string> remaining = new List<string>(HBAssets.NecessaryRoles);
        Dictionary<string, ulong> roleMap = new Dictionary<string, ulong>();
        foreach (SocketRole role in guild.Roles) {
            if (HBAssets.NecessaryRoles.Contains(role.Name)) {
                if (roleMap.ContainsKey(role.Name)) {
                    Console.WriteLine($"WARNING: \"{role.Name}\" found multiple times; only the first instance will be used.");
                    continue;
                }
                roleMap[role.Name] = role.Id;
                remaining.Remove(role.Name);
            }
        }
        if (remaining.Count != 0) {
            Console.WriteLine("WARNING: Not all roles were found, so the output JSON will not be valid. These are the missing roles:");
            foreach (string missing in remaining)
                Console.WriteLine($"- {missing}");
        }
        Console.WriteLine();

        // Displaying results:
        string outputJSON = JsonSerializer.Serialize(roleMap, new JsonSerializerOptions {
            WriteIndented = true,
        });
        Console.WriteLine($"{outputJSON}\n");

        await discordClient.StopAsync();
    }

}

using Discord;
using Discord.Net;
using Discord.WebSocket;
using HeinzBOTtle;
using System.Text.Json;

class Program {

    public static DiscordSocketClient discordClient = new DiscordSocketClient();
    public static HttpClient httpClient = new HttpClient();
    public static string hypixelKey = "";
    public static string botToken = "";
    public static ulong discordGuildID = 0;

    static Task Main() => new Program().MainAsync();

    async Task MainAsync() {
        if (!LoadFileVariables())
            return;

        DiscordHandling.CommandCooldowns["reqs"] = DateTime.Now.Ticks;

        discordClient.Log += DLogEvent;
        discordClient.Ready += DReadyEvent;
        await discordClient.LoginAsync(TokenType.Bot, botToken);
        await discordClient.StartAsync();
        await Task.Delay(3000);

        Console.WriteLine("Running in: " + Directory.GetCurrentDirectory().ToString());
        await ConsoleClient();

        await discordClient.StopAsync();
    }

    Task DLogEvent(LogMessage msg) {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    Task DReadyEvent() {
        discordClient.SlashCommandExecuted += DiscordHandling.SlashCommandHandler;
        return Task.CompletedTask;
    }

    static async Task ConsoleClient() {
        while (true) {
            string? command = Console.ReadLine();
            if (command == null || command == string.Empty)
                continue;
            switch (command) {
                case "help":
                    Console.WriteLine("=> shutdown\n=> update-commands\n=> display-player-cache");
                    break;
                case "shutdown":
                    return;
                case "update-commands":
                    await UpdateCommands();
                    break;
                case "display-player-cache":
                    DisplayPlayerCache();
                    break;
                default:
                    Console.WriteLine("???");
                    break;
            }
        }
    }

    static bool LoadFileVariables() {
        string json;
        try {
            json = File.ReadAllText("variables.json");
        } catch (Exception e) {
            Console.WriteLine("Unable to access variables file: " + e.Message);
            return false;
        }
        Dictionary<string, JsonElement> info = JsonMethods.DeserializeJsonObject(json);
        if (info == null) {
            Console.WriteLine("Invalid variables.json file!");
            return false;
        }

        string? hypixelKeyRaw = info["hypixelKey"].GetString();
        string? botTokenRaw = info["botToken"].GetString();
        string? discordGuildIDRaw = info["discordGuildID"].GetString();
        if (hypixelKeyRaw == null || botTokenRaw == null || discordGuildIDRaw == null) {
            Console.WriteLine("Invalid variables.json file!");
            return false;
        }
        
        hypixelKey = hypixelKeyRaw;
        botToken = botTokenRaw;
        discordGuildID = ulong.Parse(discordGuildIDRaw);
        return true;
    }

    static async Task UpdateCommands() {
        SocketGuild guild = discordClient.GetGuild(discordGuildID);

        SlashCommandBuilder reqsCommand = new SlashCommandBuilder();
        reqsCommand.IsDefaultPermission = true;
        reqsCommand.WithName("reqs");
        reqsCommand.WithDescription("[EXPERIMENTAL] This checks the guild requirements met by the provided player.");
        reqsCommand.AddOption("username", ApplicationCommandOptionType.String, "The username of the player to check", isRequired: true);
        try {
            await guild.CreateApplicationCommandAsync(reqsCommand.Build());
        } catch (HttpException exception) {
            Console.WriteLine("Creation of \"reqs\" command failed: " + exception.Message);
        }
        
    }

    static void DisplayPlayerCache() {
        Console.WriteLine("Current Timestamp: "  + DateTime.Now.Ticks);
        if (HypixelMethods.PlayerCache.Count == 0) {
            Console.WriteLine("The cache is empty.");
            return;
        }
        foreach (string username in HypixelMethods.PlayerCache.Keys)
            Console.WriteLine(username + " - " + HypixelMethods.PlayerCache[username].Timestamp);
    }

}
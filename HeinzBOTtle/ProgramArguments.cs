using Discord;
using Discord.WebSocket;
using System.Text.Json;

namespace HeinzBOTtle;

public static class ProgramArguments {

    /// <summary>Handles the operation of the program with the -r flag, inidcating that the program should collect role IDs from the server and then terminate.</summary>
    public static async Task R() {
        // Getting necessary config:
        Json json;
        try {
            json = new Json(File.ReadAllText("variables.json"));
        } catch (Exception e) {
            Console.WriteLine("Unable to properly access variables file: " + e.Message);
            return;
        }
        if (json.IsEmpty()) {
            Console.WriteLine("Invalid variables.json file!");
            return;
        }
        string? rawDiscordToken = json.GetString("DiscordToken");
        ulong? rawDiscordGuildID = json.GetUInt64("DiscordGuildID");
        if (rawDiscordToken == null || rawDiscordGuildID == null) {
            Console.WriteLine("Values for DiscordToken and DiscordGuildID must be present in the variables.json file to use the -r flag.");
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
        List<string> remaining = new List<string>(HBData.NecessaryRoles);
        Dictionary<string, ulong> roleMap = new Dictionary<string, ulong>();
        foreach (SocketRole role in guild.Roles) {
            if (HBData.NecessaryRoles.Contains(role.Name)) {
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

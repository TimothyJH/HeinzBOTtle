using Discord;
using HeinzBOTtle.Database;
using HeinzBOTtle.Statics;

namespace HeinzBOTtle;

internal class Program {

    private Semaphore ReadySignal { get; } = new Semaphore(0, 1);
    private readonly EventHandlers events = new EventHandlers();

    private static Task Main(string[] args) => new Program().MainAsync(args);

    private async Task MainAsync(string[] args) {
        // Handling potential arguments:
        if (args.Contains("-r")) {
            await ConsoleMethods.UseRFlag();
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
        HBConfigSingleton config = HBConfigSingleton.GetSingleton();
        if (!config.IsCorrect) {
            HBData.Log.Dispose();
            HBData.Log.Retire(new DirectoryInfo(config.LogDestinationPath));
            return;
        }

        // Initializing database connection:
        HBClients.DatabaseConnection.InfoMessage += events.MInfoMessageEvent;
        HBClients.DatabaseConnection.StateChange += events.MStateChangeEvent;
        string[] loginParts = HBConfig.DatabaseLogin.Split(' ');
        if (loginParts.Length != 4) {
            await HBData.Log.InfoAsync("The provided database login information is not properly formatted; program will terminate.");
            HBData.Log.Dispose();
            HBData.Log.Retire(new DirectoryInfo(HBConfig.LogDestinationPath));
            return;
        }
        string[] serverParts = loginParts[0].Split(':');
        string connectionString = $"Server={serverParts[0]};Database={loginParts[1]};User ID={loginParts[2]};Password={loginParts[3]}";
        if (serverParts.Length == 2)
            connectionString += $";Port={serverParts[1]}";
        HBClients.DatabaseConnection.ConnectionString = connectionString;
        await HBClients.DatabaseConnection.OpenAsync();
        if (HBClients.DatabaseConnection.State != System.Data.ConnectionState.Open) {
            await HBData.Log.InfoAsync("There was a problem when trying to connect to the database; program will terminate.");
            HBData.Log.Dispose();
            HBData.Log.Retire(new DirectoryInfo(HBConfig.LogDestinationPath));
            return;
        }
        await DBMethods.UpdateDatabaseSchemaAsync();
        HBClients.DiscordClient.Log += events.DLogEvent;
        Task readyEvent() { return events.DReadyEvent(ReadySignal); }
        HBClients.DiscordClient.Ready += readyEvent;
        HBClients.DiscordClient.Disconnected += events.DDisconnectedEvent;
        await HBClients.DiscordClient.LoginAsync(TokenType.Bot, HBConfig.DiscordToken);
        await HBClients.DiscordClient.StartAsync();

        // Waiting for the Discord client to be ready:
        while (true) {
            if (ReadySignal.WaitOne(30000))
                break;
            await HBData.Log.InfoAsync("The Discord client is taking quite a long time to get ready; perhaps something is wrong?");
        }
        HBClients.DiscordClient.Ready -= readyEvent;
        ReadySignal.Release();

        // Running the console client:
        await ConsoleMethods.ConsoleClientAsync();

        // Stopping the program:
        await HBClients.DiscordClient.StopAsync();
        await HBClients.DatabaseConnection.CloseAsync();
        HBData.Log.Dispose();
        HBData.Log.Retire(new DirectoryInfo(HBConfig.LogDestinationPath));
    }

}

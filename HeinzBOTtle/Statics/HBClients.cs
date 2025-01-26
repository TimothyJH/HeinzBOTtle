using Discord.WebSocket;
using MySqlConnector;

namespace HeinzBOTtle.Statics;

public static class HBClients {

    /// <summary>The client dealing with interactions between this process and Discord.</summary>
    public static DiscordSocketClient DiscordClient { get; } = new DiscordSocketClient();
    /// <summary>The client dealing with interactions between this process and the Hypixel API.</summary>
    public static HttpClient HttpClient { get; } = new HttpClient();
    /// <summary>The client dealing with interactions between this process and the database.</summary>
    public static MySqlConnection DatabaseConnection { get; } = new MySqlConnection();

}

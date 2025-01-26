using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Database;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Statics;
using System.Text.Json;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents the implementation of a Discord slash command.
/// </summary>
public abstract class HBCommand {

    /// <summary>The literal name of the command as called in Discord without the initial slash.</summary>
    public string Name { get; }
    /// <summary>Whether this command should acquire the database modification semaphore for its execution.</summary>
    public bool ModifiesDatabase { get; }
    /// <summary>The timestamp of the most recent instance of the command's execution.</summary>
    public long LastExecution { get; set; }

    public HBCommand(string name, bool modifiesDatabase = false) {
        Name = name;
        ModifiesDatabase = modifiesDatabase;
        LastExecution = 0L;
    }

    /// <summary>Runs the logic to run a command safely, including generic error handling and database semaphore acquisition.</summary>
    /// <param name="slashCommand">The executed command received by the handler</param>
    public async Task ExecuteCommandSafelyAsync(SocketSlashCommand slashCommand) {
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
        if (ModifiesDatabase) {
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
            await ExecuteCommandAsync(slashCommand);
        } catch (Exception e) {
            await HBData.Log.InfoAsync($"An unhandled exception occurred during command execution:\n{e.GetType()}: {e.Message}\n{e.StackTrace}");
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription("Oopsies, something went wrong!");
            embed.WithColor(Color.Gold);
            await slashCommand.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = embed.Build();
            });
            return;
        } finally {
            if (ModifiesDatabase)
                HBData.ModifyingDatabase.Release();
        }
    }

    /// <summary>Runs the actual logic for this command's execution after its initial handling.</summary>
    /// <param name="slashCommand">The executed command to process</param>
    protected virtual async Task ExecuteCommandAsync(SocketSlashCommand slashCommand) {
        await slashCommand.RespondAsync(embed: (new EmbedBuilder()).WithDescription("?????").Build());
    }

    /// <returns>The command properties to be applied to this command.</returns>
    public abstract SlashCommandProperties GenerateCommandProperties();

    /// <summary>Processes a player API call within a command, responding with an appropriate error message is something went wrong with the input validation
    /// or the returned API data.</summary>
    /// <param name="key">The username or UUID of the player whose API information is to be retrieved (depending on the value of the next argument)</param>
    /// <param name="uuid">Whether the key is a UUID (true) or a username (false)</param>
    /// <returns>JSON representing the player if the retrieval was successful or null if there was an issue and subsequent error response.</returns>
    protected static async Task<Json?> HandlePlayerAPIRetrievalAsync(string key, bool uuid, SocketSlashCommand slashCommand) {
        key = key.Trim();
        Json json;
        if (!uuid) {
            if (!HypixelMethods.IsValidUsername(key)) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("That is not a valid username.");
                fail.WithColor(Color.Gold);
                await slashCommand.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return null;
            }
            json = await HypixelMethods.RetrievePlayerAPI(key);
        } else {
            if (key.Length != 32) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("That is not a valid UUID.");
                fail.WithColor(Color.Gold);
                await slashCommand.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return null;
            }
            json = await HypixelMethods.RetrievePlayerAPI(key, uuid: true);
        }

        bool success = json.GetBoolean("success") ?? false;
        if (!success) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("Oopsies, something went wrong!");
            fail.WithColor(Color.Gold);
            await slashCommand.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return null;
        }
        if (json.GetValueKind("player") != JsonValueKind.Object) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("Hypixel doesn't seem to have any information about this player. This player probably changed usernames, never logged into Hypixel, or doesn't exist.");
            fail.WithColor(Color.Gold);
            await slashCommand.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return null;
        }
        return json;
    }

}

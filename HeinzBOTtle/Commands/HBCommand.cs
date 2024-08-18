using Discord;
using Discord.WebSocket;

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

    /// <summary>Runs the logic for this command's execution after its initial handling.</summary>
    /// <param name="command">The executed command received by the handler</param>
    public virtual async Task ExecuteCommandAsync(SocketSlashCommand command) {
        await command.RespondAsync(embed: (new EmbedBuilder()).WithDescription("?????").Build());
    }

    /// <returns>The command properties to be applied to this command.</returns>
    public abstract SlashCommandProperties GenerateCommandProperties();

}

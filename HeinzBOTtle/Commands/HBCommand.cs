using Discord;
using Discord.WebSocket;

namespace HeinzBOTtle.Commands;

public abstract class HBCommand {

    public string Name { get; }
    public long LastExecution { get; set; }

    public HBCommand(string name) {
        Name = name;
        LastExecution = 0L;
    }

    public virtual async Task ExecuteCommandAsync(SocketSlashCommand command) {
        await command.RespondAsync(embed: (new EmbedBuilder()).WithDescription("?????").Build());
    }

    public abstract SlashCommandProperties GenerateCommandProperties();

}

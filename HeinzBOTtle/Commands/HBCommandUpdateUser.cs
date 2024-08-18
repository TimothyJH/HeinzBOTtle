using Discord.WebSocket;
using Discord;
using HeinzBOTtle.Database;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /update-user.
/// </summary>
public class HBCommandUpdateUser : HBCommand {

    public HBCommandUpdateUser() : base("update-user", modifiesDatabase: true) { }

    public override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        uint id = (uint)(long)command.Data.Options.First().Value;
        DBUser user = new DBUser(id);
        if (!await user.Exists()) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription($"A user with ID {id} has not been enrolled.");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        byte code = await DBMethods.UpdateUserAsync(user);
        if (code == 2) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("This user does not have a linked Minecraft account, so the update cannot proceed.");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        } else if (code == 4) {
            EmbedBuilder halfFail = new EmbedBuilder();
            halfFail.WithDescription("The flag update was successful, but roles could not be modified successfully!");
            halfFail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = halfFail.Build();
            });
            return;
        } else if (code != 0) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("Oopsies, something went wrong!");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        uint? color = await user.GetSignatureColorAsync();
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithDescription($"User update complete!");
        embed.WithColor(color ?? Color.Green);
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = embed.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = GuildPermission.Administrator;
        command.WithName(Name);
        command.WithDescription("Updates Discord roles and data based on a user's Hypixel information.");
        command.AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue);
        return command.Build();
    }

}

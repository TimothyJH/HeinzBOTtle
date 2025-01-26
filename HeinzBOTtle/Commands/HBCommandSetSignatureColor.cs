using Discord.WebSocket;
using Discord;
using HeinzBOTtle.Database;
using System.Text.RegularExpressions;
using System.Globalization;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /set-signature-color.
/// </summary>
public class HBCommandSetSignatureColor : HBCommand {

    public HBCommandSetSignatureColor() : base("set-signature-color", modifiesDatabase: true) { }

    protected override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        DBUser? user = await DBUser.FromDiscordIDAsync(command.User.Id);
        if (user == null) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("You are not currently enrolled in the database; use `/link-minecraft`!");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }
        string input = (string)command.Data.Options.First();

        Match match = Regex.Match(input, "^(#|0x)?([0123456789AaBbCcDdEeFf]{1,6})$");
        if (!match.Success) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("The color was not provided in 8-bit hexadecimal format.");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        uint color = uint.Parse(match.Groups[2].Value, NumberStyles.HexNumber);
        await user.Value.SetSignatureColorAsync(color);

        EmbedBuilder embed = new EmbedBuilder();
        embed.WithDescription($"Your signature color has been updated to `#{color:x6}`!");
        embed.WithColor(color);
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = embed.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = null;
        command.WithName(Name);
        command.WithDescription("This shows the info associated with a user in the database.");
        command.AddOption("color", ApplicationCommandOptionType.String, "The new 8-bit color in hexadecimal (ex: 1177ff)", isRequired: true);
        return command.Build();
    }

}

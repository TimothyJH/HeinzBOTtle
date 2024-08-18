using Discord.WebSocket;
using Discord;
using HeinzBOTtle.Database;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /update.
/// </summary>
public class HBCommandUpdate : HBCommand {

    public HBCommandUpdate() : base("update", modifiesDatabase: true) { }

    public override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        DBUser user;
        DBUser? self = await DBUser.FromDiscordIDAsync(command.User.Id);
        if (self == null) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("You are not currently enrolled in the database; use `/link-minecraft`!");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }
        user = self.Value;

        byte code = await DBMethods.UpdateUserAsync(user);
        if (code == 2) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("You do not have a linked Minecraft account, so the update cannot proceed.");
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
        command.DefaultMemberPermissions = null;
        command.WithName(Name);
        command.WithDescription("Updates your Discord roles and user data based on your Hypixel information.");
        return command.Build();
    }

}

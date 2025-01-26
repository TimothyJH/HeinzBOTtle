using Discord.WebSocket;
using Discord;
using HeinzBOTtle.Database;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /link-new-user.
/// </summary>
public class HBCommandLinkNewUser : HBCommand {

    public HBCommandLinkNewUser() : base("link-new-user", modifiesDatabase: true) { }

    protected override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        ulong discordID = 0;
        string username = "";
        foreach (SocketSlashCommandDataOption option in command.Data.Options) {
            switch (option.Name) {
                case "discord-user":
                    discordID = ((SocketGuildUser)option.Value).Id;
                    break;
                case "minecraft-username":
                    username = ((string)option).Trim();
                    break;
            }
        }

        Json json;
        Json? retrievalAttempt = await HandlePlayerAPIRetrievalAsync(username, false, command);
        if (retrievalAttempt == null)
            return;
        else
            json = retrievalAttempt;

        username = json.GetString("player.displayname") ?? "?????";
        string uuid = json.GetString("player.uuid") ?? "?????";
        (DBUser? user, byte code) = await DBUser.EnrollAsync(discordID, uuid, username);
        if (code != 0 && code != 4) {
            EmbedBuilder fail = new EmbedBuilder();
            string description = "";
            if ((code & 1) == 1)
                description += $"<@{discordID}> has already been enrolled.\n";
            if ((code & 2) == 2)
                description += $"**{username.Replace("_", "\\_")}** is already associated with a user.";
            fail.WithDescription(description.Trim());
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        } else if (code == 4) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("Oopsies, something went wrong!");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        EmbedBuilder embed = new EmbedBuilder();
        embed.WithDescription($"Link established between <@{discordID}> and **{username.Replace("_", "\\_")}**!");
        embed.WithColor(Color.Green);
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = embed.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = GuildPermission.Administrator;
        command.WithName(Name);
        command.WithDescription("This adds a new user to the database.");
        command.AddOption("discord-user", ApplicationCommandOptionType.User, "The user's Discord account", isRequired: true);
        command.AddOption("minecraft-username", ApplicationCommandOptionType.String, "The user's Minecraft username", isRequired: true);
        return command.Build();
    }

}

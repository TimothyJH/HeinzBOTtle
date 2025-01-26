using Discord.WebSocket;
using Discord;
using HeinzBOTtle.Database;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /user-info.
/// </summary>
public class HBCommandUserInfo : HBCommand {

    public HBCommandUserInfo() : base("user-info") { }

    protected override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        DBUser? user = null;
        string? cachedMCUsername = null;
        switch (command.Data.Options.First().Name) {
            case "me":
                user = await DBUser.FromDiscordIDAsync(command.User.Id);
                break;
            case "from-discord-user":
                ulong newDiscordID = ((SocketGuildUser)command.Data.Options.First().Options.First().Value).Id;
                user = await DBUser.FromDiscordIDAsync(newDiscordID);
                break;
            case "from-minecraft-username":
                string username = (string)command.Data.Options.First().Options.First();

                Json json;
                Json? retrievalAttempt = await HandlePlayerAPIRetrievalAsync(username, false, command);
                if (retrievalAttempt == null)
                    return;
                else
                    json = retrievalAttempt;

                cachedMCUsername = json.GetString("player.displayname") ?? "?????";
                user = await DBUser.FromMinecraftUUIDAsync(json.GetString("player.uuid") ?? "?????");
                break;
            case "from-id":
                uint id = (uint)(long)command.Data.Options.First().Options.First().Value;
                user = new DBUser(id);
                if (!await user.Value.Exists())
                    user = null;
                break;
        }

        if (user == null) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("A matching user has not been found. :(");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }
        string? info = await DBMethods.GenerateUserSummaryAsync(user.Value, cachedMCUsername);
        if (info == null) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("There was an error when getting info for that user. :(");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        EmbedBuilder embed = new EmbedBuilder();
        embed.WithTitle("User Info");
        embed.WithDescription(info);
        embed.WithColor(await user.Value.GetSignatureColorAsync() ?? 0x0u);
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = embed.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = null;
        command.WithName(Name);
        command.WithDescription("This shows the info associated with a user in the database.");
        SlashCommandOptionBuilder sc0 = new SlashCommandOptionBuilder().WithName("me").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Searches for you");
        SlashCommandOptionBuilder sc1 = new SlashCommandOptionBuilder().WithName("from-discord-user").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Searches by Discord user")
            .AddOption("discord-user", ApplicationCommandOptionType.User, "The user's Discord account", isRequired: true);
        SlashCommandOptionBuilder sc2 = new SlashCommandOptionBuilder().WithName("from-minecraft-username").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Searches by Minecraft player")
            .AddOption("minecraft-username", ApplicationCommandOptionType.String, "The user's Minecraft username", isRequired: true);
        SlashCommandOptionBuilder sc3 = new SlashCommandOptionBuilder().WithName("from-id").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Searches by database ID")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue);

        command.AddOption(sc0).AddOption(sc1).AddOption(sc2).AddOption(sc3);
        return command.Build();
    }

}

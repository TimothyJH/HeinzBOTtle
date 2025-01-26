using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Database;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Requirements;
using HeinzBOTtle.Statics;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /reqs.
/// </summary>
public class HBCommandReqs : HBCommand {

    public HBCommandReqs() : base("reqs") { }

    protected override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        Json json;
        string username;
        if (command.Data.Options.Count == 0) {
            DBUser? user = await DBUser.FromDiscordIDAsync(command.User.Id);
            string? uuid = null;
            if (user != null)
                uuid = await user.Value.GetMinecraftUUIDAsync();
            if (user == null || uuid == null) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("A linked Minecraft account is required to use this command without an argument. You can link your Minecraft account with `/link-minecraft`.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }
            Json? retrievalAttempt = await HandlePlayerAPIRetrievalAsync(uuid, true, command);
            if (retrievalAttempt == null)
                return;
            else
                json = retrievalAttempt;
        } else {
            username = ((string)command.Data.Options.First<SocketSlashCommandDataOption>()).Trim();
            Json? retrievalAttempt = await HandlePlayerAPIRetrievalAsync(username, false, command);
            if (retrievalAttempt == null)
                return;
            else
                json = retrievalAttempt;
        }

        int level = (int)HypixelMethods.GetNetworkLevelFromXP(json.GetDouble("player.networkExp") ?? 0.0);
        username = json.GetString("player.displayname") ?? "?????";

        List<Requirement> met = ReqMethods.GetRequirementsMet(json);
        EmbedBuilder embed = new EmbedBuilder();

        embed.WithTitle(username.Replace("_", "\\_"));
        string output = "\n";
        if (met.Count == 0)
            output += "is network level " + level + " and meets 0 game requirements.";
        else if (met.Count == 1)
            output += "is network level " + level + " and meets 1 game requirement:\n";
        else
            output += "is network level " + level + " and meets " + met.Count + " game requirements:\n";
        foreach (Requirement req in met) {
            if (HBData.RoleMap.TryGetValue(req.Title, out ulong roleID))
                output += $"\n<@&{roleID}> - {req.GameTitle}";
            else
                output += $"\n{req.Title} - {req.GameTitle}";
        }

        embed.WithDescription(output);
        Color? signatureColor = await DBMethods.FindReplacementColorAsync(json.GetString("player.uuid") ?? "?????");
        if (signatureColor != null)
            embed.WithColor(signatureColor.Value);
        else if (level >= 85 && met.Count >= 1)
            embed.WithColor(Color.Green);
        else
            embed.WithColor(Color.Red);
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = embed.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = null;
        command.WithName(Name);
        command.WithDescription("This checks the guild requirements met by the provided player.");
        command.AddOption("username", ApplicationCommandOptionType.String, "The username of the player to check", isRequired: false);
        return command.Build();
    }

}

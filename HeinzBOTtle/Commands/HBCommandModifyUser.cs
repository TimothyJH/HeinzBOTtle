using Discord.WebSocket;
using Discord;
using HeinzBOTtle.Database;
using HeinzBOTtle.Ranks;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /modify-user.
/// </summary>
public class HBCommandModifyUser : HBCommand {

    public HBCommandModifyUser() : base("modify-user", modifiesDatabase: true) { }

    protected override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        ulong executor = command.User.Id;
        uint id = (uint)(long)command.Data.Options.First().Options.First().Value;

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

        switch (command.Data.Options.First().Name) {
            case "change-discord-user":
                ulong? discordID = await user.GetDiscordUserIDAsync();
                ulong? newDiscordID = ((SocketGuildUser)command.Data.Options.First().Options.ElementAt(1).Value).Id;
                if (newDiscordID == 0)
                    newDiscordID = null;
                else {
                    DBUser? existingUserA = await DBUser.FromDiscordIDAsync(newDiscordID.Value);
                    if (existingUserA != null) {
                        EmbedBuilder fail = new EmbedBuilder();
                        ulong? e = await existingUserA.Value.GetDiscordUserIDAsync();
                        fail.WithDescription(discordID == e ? $"No change." : $"<@{newDiscordID}> is already associated with a separate user.");
                        fail.WithColor(Color.Gold);
                        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                            p.Embed = fail.Build();
                        });
                        return;
                    }
                }

                byte codeA = await user.SetDiscordUserIDAsync(newDiscordID, modifier: executor);
                if (codeA != 0)
                    throw new Exception("Database update that should not have failed failed.");

                EmbedBuilder embedA = new EmbedBuilder();
                embedA.WithDescription(newDiscordID == null ? "The Discord account has been removed!" : $"The Discord account has been changed to <@{newDiscordID}>!");
                embedA.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = embedA.Build();
                });
                return;
            case "change-minecraft-username":
                string? username = (string)command.Data.Options.First().Options.ElementAt(1);
                string? uuid;
                if (username == "-") {
                    uuid = null;
                } else {
                    Json json;
                    Json? retrievalAttempt = await HandlePlayerAPIRetrievalAsync(username, false, command);
                    if (retrievalAttempt == null)
                        return;
                    else
                        json = retrievalAttempt;

                    username = json.GetString("player.displayname") ?? "?????";
                    uuid = json.GetString("player.uuid") ?? "?????";

                    DBUser? existingUserB = await DBUser.FromMinecraftUUIDAsync(uuid);
                    if (existingUserB != null) {
                        EmbedBuilder fail = new EmbedBuilder();
                        uint e = existingUserB.Value.ID;
                        fail.WithDescription(id == e ? $"No change."
                            : $"**{username.Replace("_", "\\_")}** is already associated with {(await existingUserB.Value.GetDiscordUserIDAsync() == null ? "another user" : $"<@{e}>")}.");
                        fail.WithColor(Color.Gold);
                        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                            p.Embed = fail.Build();
                        });
                        return;
                    }
                }

                byte codeB = await user.SetMinecraftUUIDAsync(uuid, modifier: executor);
                if (codeB != 0)
                    throw new Exception("Database update that should not have failed failed.");

                EmbedBuilder embedB = new EmbedBuilder();
                embedB.WithDescription(uuid == null ? "The associated player has been removed!" : $"The associated player has been changed to **{username.Replace("_", "\\_")}**!");
                embedB.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = embedB.Build();
                });
                return;
            case "delete":
                byte codeC = await user.DeleteAsync(modifier: executor);
                if (codeC != 0)
                    throw new Exception("Database update that should not have failed failed.");
                EmbedBuilder embedC = new EmbedBuilder();
                embedC.WithDescription("The user has been deleted!");
                embedC.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = embedC.Build();
                });
                return;
            case "highest-rank":
                string rawRank = (string)command.Data.Options.First().Options.ElementAt(1);
                Rank? rank = null;
                foreach (Rank test in Enum.GetValues(typeof(Rank)))
                    if (test.ToString().StartsWith(rawRank, StringComparison.OrdinalIgnoreCase)) {
                        rank = test;
                        break;
                    }
                if (rank == null) {
                    EmbedBuilder fail = new EmbedBuilder();
                    string desc = "Unrecognized rank. Please make sure that the rank you type matches one of the following:";
                    foreach (Rank r in Enum.GetValues(typeof(Rank)))
                        desc += $"\n- {r}";
                    desc += "\nSpecifically, what you type must match the beginning of one of these options.";
                    fail.WithDescription(desc);
                    fail.WithColor(Color.Gold);
                    await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                        p.Embed = fail.Build();
                    });
                    return;
                }
                byte oldFlagsD = await user.GetFlagsAsync();
                byte newFlagsD = (byte)((oldFlagsD & 0b11111000) | (byte)rank.Value);
                if (oldFlagsD == newFlagsD) {
                    EmbedBuilder fail = new EmbedBuilder();
                    fail.WithDescription("No change.");
                    fail.WithColor(Color.Green);
                    await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                        p.Embed = fail.Build();
                    });
                    return;
                }
                byte codeD = await user.SetFlagsAsync(newFlagsD, modifier: executor);
                if (codeD != 0)
                    throw new Exception("Database update that should not have failed failed.");
                EmbedBuilder embedD = new EmbedBuilder();
                embedD.WithDescription($"The highest achieved rank has been updated to {rank}!");
                embedD.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = embedD.Build();
                });
                return;
            case "treehard-level":
                string rawTreehard = ((string)command.Data.Options.First().Options.ElementAt(1)).Replace("+", "Plus");
                TreehardLevel? treehard = null;
                foreach (TreehardLevel test in Enum.GetValues(typeof(TreehardLevel)))
                    if (test.ToString().Equals(rawTreehard, StringComparison.OrdinalIgnoreCase)) {
                        treehard = test;
                        break;
                    }
                if (treehard == null) {
                    EmbedBuilder fail = new EmbedBuilder();
                    string desc = "Unrecognized Treehard level. Please make sure that the rank you type matches one of the following:";
                    foreach (TreehardLevel tl in Enum.GetValues(typeof(TreehardLevel)))
                        desc += $"\n- {tl.ToString().Replace("Plus", "+")}";
                    fail.WithDescription(desc);
                    fail.WithColor(Color.Gold);
                    await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                        p.Embed = fail.Build();
                    });
                    return;
                }
                byte oldFlagsE = await user.GetFlagsAsync();
                byte newFlagsE = (byte)((oldFlagsE & 0b11100111) | (byte)treehard.Value);
                if (oldFlagsE == newFlagsE) {
                    EmbedBuilder fail = new EmbedBuilder();
                    fail.WithDescription("No change.");
                    fail.WithColor(Color.Green);
                    await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                        p.Embed = fail.Build();
                    });
                    return;
                }
                byte codeE = await user.SetFlagsAsync(newFlagsE, modifier: executor);
                if (codeE != 0)
                    throw new Exception("Database update that should not have failed failed.");
                EmbedBuilder embedE = new EmbedBuilder();
                embedE.WithDescription($"The Treehard level has been updated to {treehard.ToString()!.Replace("Plus", "+")}!");
                embedE.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = embedE.Build();
                });
                return;
            case "hq-status":
                bool granted = (bool)command.Data.Options.First().Options.ElementAt(1);
                byte oldFlagsF = await user.GetFlagsAsync();
                byte newFlagsF = (byte)((oldFlagsF & 0b11011111) | (granted ? 1 << 5 : 0));
                if (oldFlagsF == newFlagsF) {
                    EmbedBuilder fail = new EmbedBuilder();
                    fail.WithDescription("No change.");
                    fail.WithColor(Color.Green);
                    await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                        p.Embed = fail.Build();
                    });
                    return;
                }
                byte codeF = await user.SetFlagsAsync(newFlagsF, modifier: executor);
                if (codeF != 0)
                    throw new Exception("Database update that should not have failed failed.");
                EmbedBuilder embedF = new EmbedBuilder();
                embedF.WithDescription($"The user's Honorary Quest status has been {(granted ? "granted" : "revoked")}!");
                embedF.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = embedF.Build();
                });
                return;
        }
        
        EmbedBuilder defaultEmbed = new EmbedBuilder();
        defaultEmbed.WithDescription("?????");
        defaultEmbed.WithColor(Color.Gold);
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = defaultEmbed.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = GuildPermission.Administrator;
        command.WithName(Name);
        command.WithDescription("This modifies a user in the database.");
        SlashCommandOptionBuilder sc0 = new SlashCommandOptionBuilder().WithName("change-discord-user").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Changes the associated Discord account")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue)
            .AddOption("new-discord-user", ApplicationCommandOptionType.User, "The user's new Discord account", isRequired: true);
        SlashCommandOptionBuilder sc1 = new SlashCommandOptionBuilder().WithName("change-minecraft-username").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Changes the associated Minecraft player")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue)
            .AddOption("new-minecraft-username", ApplicationCommandOptionType.String, "The new Minecraft player to associate with the user", isRequired: true);
        SlashCommandOptionBuilder sc2 = new SlashCommandOptionBuilder().WithName("delete").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Deletes the user from the database")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue);
        SlashCommandOptionBuilder sc3 = new SlashCommandOptionBuilder().WithName("highest-rank").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Sets the highest guild rank achieved")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue)
            .AddOption("rank", ApplicationCommandOptionType.String, "The new highest guild rank achieved by the user", isRequired: true);
        SlashCommandOptionBuilder sc4 = new SlashCommandOptionBuilder().WithName("treehard-level").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Sets the highest Treehard rank achieved")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue)
            .AddOption("treehard-level", ApplicationCommandOptionType.String, "The new Treehard rank achieved by the user", isRequired: true);
        SlashCommandOptionBuilder sc5 = new SlashCommandOptionBuilder().WithName("hq-status").WithType(ApplicationCommandOptionType.SubCommand)
            .WithDescription("Sets whether the user is granted Honorary Quest status")
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user's database ID", isRequired: true, minValue: 0, maxValue: uint.MaxValue)
            .AddOption("granted", ApplicationCommandOptionType.Boolean, "Whether Honorary Quest status is granted", isRequired: true);

        command.AddOption(sc0).AddOption(sc1).AddOption(sc2).AddOption(sc3).AddOption(sc4).AddOption(sc5);
        return command.Build();
    }

}

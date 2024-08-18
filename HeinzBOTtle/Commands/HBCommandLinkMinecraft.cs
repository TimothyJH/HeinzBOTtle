using Discord.WebSocket;
using Discord;
using HeinzBOTtle.Hypixel;
using System.Text.Json;
using HeinzBOTtle.Database;
using MySqlConnector;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /link-minecraft.
/// </summary>
public class HBCommandLinkMinecraft : HBCommand {

    public HBCommandLinkMinecraft() : base("link-minecraft", modifiesDatabase: true) { }

    public override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        ulong discordID = command.User.Id;
        string username = ((string)command.Data.Options.First()).Trim();
        if (!HypixelMethods.IsValidUsername(username)) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("That is not a valid username.");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        Json json = await HypixelMethods.RetrievePlayerAPI(username);

        bool success = json.GetBoolean("success") ?? false;
        if (!success) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("Oopsies, something went wrong!");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        if (json.GetValueKind("player") != JsonValueKind.Object) {
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription("Hypixel doesn't seem to have any information about the provided player. This player probably changed usernames, never logged into Hypixel, or doesn't exist.");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        username = json.GetString("player.displayname") ?? "?????";
        string uuid = json.GetString("player.uuid") ?? "?????";
        string? apiDiscordUsername = json.GetString("player.socialMedia.links.DISCORD");

        DBUser? existingUser = await DBUser.FromMinecraftUUIDAsync(uuid);
        if (existingUser != null) {
            ulong? currentHolder = await existingUser.Value.GetDiscordUserIDAsync();
            string currentHolderName;
            if (currentHolder == null)
                currentHolderName = "someone";
            else if (currentHolder == discordID)
                currentHolderName = "you";
            else
                currentHolderName = $"<@{currentHolder}>";
            EmbedBuilder fail = new EmbedBuilder();
            fail.WithDescription($"**{username.Replace("_", "\\_")}** is already linked to {currentHolderName}!");
            fail.WithColor(Color.Gold);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = fail.Build();
            });
            return;
        }

        bool isAlreadyLinking = false;
        using (MySqlCommand cmd = new MySqlCommand("SELECT EXISTS(SELECT DiscordUserID FROM LinkPending WHERE DiscordUserID = @a)", HBData.DatabaseConnection)) {
            cmd.Parameters.AddWithValue("a", discordID);
            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                isAlreadyLinking = reader.GetBoolean(0);
        }

        if (isAlreadyLinking) {
            string previousUUID = "";
            ulong existingMessageID = 0uL;
            using (MySqlCommand cmd = new MySqlCommand("SELECT MinecraftUUID, ReviewMessageID FROM LinkPending WHERE DiscordUserID = @a", HBData.DatabaseConnection)) {
                cmd.Parameters.AddWithValue("a", discordID);
                using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) {
                    previousUUID = reader.GetString(0);
                    existingMessageID = reader.GetUInt64(1);
                }
            }
            if (command.User.Username.Equals(apiDiscordUsername, StringComparison.OrdinalIgnoreCase)) {
                SocketTextChannel channel = (SocketTextChannel)await HBData.DiscordClient.GetChannelAsync(HBData.ReviewChannelID);
                IMessage message = await channel.GetMessageAsync(existingMessageID);

                DBUser? eU = await DBUser.FromDiscordIDAsync(discordID);
                if (eU != null) {
                    byte code = await eU.Value.SetMinecraftUUIDAsync(uuid);
                    if (code != 0) {
                        EmbedBuilder fail = new EmbedBuilder();
                        fail.WithDescription("Oopsies, something went wrong!");
                        fail.WithColor(Color.Gold);
                        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                            p.Embed = fail.Build();
                        });
                        return;
                    }
                } else {
                    (DBUser? newUser, byte code) result = await DBUser.EnrollAsync(discordID, uuid, username);
                    if (result.code != 0) {
                        EmbedBuilder fail = new EmbedBuilder();
                        fail.WithDescription("Oopsies, something went wrong!");
                        fail.WithColor(Color.Gold);
                        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                            p.Embed = fail.Build();
                        });
                        return;
                    }
                }
                await message.DeleteAsync();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM LinkPending WHERE DiscordUserID = @a", HBData.DatabaseConnection)) {
                    cmd.Parameters.AddWithValue("a", discordID);
                    await cmd.ExecuteNonQueryAsync();
                }
                EmbedBuilder autoEmbed = new EmbedBuilder();
                autoEmbed.WithDescription($"You have been linked to **{username.Replace("_", "\\_")}**!");
                autoEmbed.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = autoEmbed.Build();
                });
                return;
            }
            if (uuid == previousUUID) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription($"Your link to **{username.Replace("_", "\\_")}** is currently pending manual review by an officer.\n\nTo bypass this, " +
                    $"you can update your Discord link in Hypixel to \"{command.User.Username}\", wait 10 minutes, and then run this command again.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }
            using (MySqlCommand cmd = new MySqlCommand("UPDATE LinkPending SET MinecraftUUID = @a WHERE DiscordUserID = @b", HBData.DatabaseConnection)) {
                cmd.Parameters.AddWithValue("a", uuid);
                cmd.Parameters.AddWithValue("b", discordID);
                await cmd.ExecuteNonQueryAsync();
            }
            using (MySqlCommand cmd = new MySqlCommand("SELECT ReviewMessageID FROM LinkPending WHERE DiscordUserID = @a", HBData.DatabaseConnection)) {
                cmd.Parameters.AddWithValue("a", discordID);
                using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    existingMessageID = reader.GetUInt64(0); // Repurposed
            }
            await DBMethods.UpdateReviewQueue(discordID, uuid, username, existingMessage: existingMessageID);
        } else {
            if (command.User.Username.Equals(apiDiscordUsername, StringComparison.OrdinalIgnoreCase)) {
                DBUser? eU = await DBUser.FromDiscordIDAsync(discordID);
                if (eU != null) {
                    byte code = await eU.Value.SetMinecraftUUIDAsync(uuid);
                    if (code != 0) {
                        EmbedBuilder fail = new EmbedBuilder();
                        fail.WithDescription("Oopsies, something went wrong!");
                        fail.WithColor(Color.Gold);
                        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                            p.Embed = fail.Build();
                        });
                        return;
                    }
                } else {
                    (DBUser? newUser, byte code) result = await DBUser.EnrollAsync(discordID, uuid, username);
                    if (result.code != 0) {
                        EmbedBuilder fail = new EmbedBuilder();
                        fail.WithDescription("Oopsies, something went wrong!");
                        fail.WithColor(Color.Gold);
                        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                            p.Embed = fail.Build();
                        });
                        return;
                    }
                }
                EmbedBuilder autoEmbed = new EmbedBuilder();
                autoEmbed.WithDescription($"You have been linked to **{username.Replace("_", "\\_")}**!");
                autoEmbed.WithColor(Color.Green);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = autoEmbed.Build();
                });
                return;
            }
            ulong messageID = await DBMethods.UpdateReviewQueue(discordID, uuid, username);
            using (MySqlCommand cmd = new MySqlCommand("INSERT INTO LinkPending(DiscordUserID, MinecraftUUID, ReviewMessageID) VALUES (@a, @b, @c)", HBData.DatabaseConnection)) {
                cmd.Parameters.AddWithValue("a", discordID);
                cmd.Parameters.AddWithValue("b", uuid);
                cmd.Parameters.AddWithValue("c", messageID);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        EmbedBuilder embed = new EmbedBuilder();
        embed.WithDescription($"Your link to **{username.Replace("_", "\\_")}** is now pending manual review by an officer.\n\nTo bypass this, " +
                $"you can update your Discord link in Hypixel to \"{command.User.Username}\", wait 10 minutes, and then run this command again.");
        embed.WithColor(Color.Green);
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = embed.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = null;
        command.WithName(Name);
        command.WithDescription("Links your Discord account to your Minecraft account, enrolling you in the database.");
        command.AddOption("username", ApplicationCommandOptionType.String, "Your Minecraft username", isRequired: true);
        return command.Build();
    }

}

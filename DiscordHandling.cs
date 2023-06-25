using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Requirements;
using System.Text.Json;

namespace HeinzBOTtle {

    class DiscordHandling {

        public static Dictionary<string, long> CommandCooldowns = new Dictionary<string, long>();

        public static async Task SlashCommandHandler(SocketSlashCommand command) {
            switch (command.Data.Name) {
                case "reqs":
                    if (DateTime.Now.Ticks - CommandCooldowns["reqs"] < 5L * 10000000L) {
                        await command.RespondAsync(embed: GenerateCooldownEmbed());
                        break;
                    }
                    CommandCooldowns["reqs"] = DateTime.Now.Ticks;
                    _ = ExecuteRequirementsCommand(command);
                    break;
                default:
                    _ = command.RespondAsync(embed: (new EmbedBuilder()).WithDescription("?????").Build());
                    break;
            }
        }

        public static Embed GenerateCooldownEmbed() {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription("This command is currently on cooldown; try again in a few seconds.");
            embed.WithColor(Color.Gold);
            return embed.Build();
        }

        public static async Task ExecuteRequirementsCommand(SocketSlashCommand command) {
            await command.DeferAsync();
            string username = (string) command.Data.Options.First<SocketSlashCommandDataOption>();
            username = username.Trim();
            if (!HypixelMethods.IsValidUsername(username)) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("That is not a valid username.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }

            string json = await HypixelMethods.RetrievePlayerAPI(username);
            bool success = JsonMethods.DeserializeJsonObject(json)["success"].GetBoolean();
            if (!success) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("Oopsies, something went wrong!");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }

            JsonElement playerElement = JsonMethods.GetNodeValue(json, "player");
            if (playerElement.ValueKind == JsonValueKind.Null) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("Hypixel doesn't seem to have any information about this player. This player probably changed usernames, never logged into Hypixel, or doesn't exist.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }

            int level;
            JsonElement xpElement = JsonMethods.GetNodeValue(json, "player.networkExp");
            if (xpElement.ValueKind == JsonValueKind.Undefined)
                level = 0;
            else
                level = (int) HypixelMethods.GetNetworkLevelFromXP(xpElement.GetDouble());
            JsonElement usernameElement = JsonMethods.GetNodeValue(json, "player.displayname");
            
            string? properUsername;
            if (usernameElement.ValueKind == JsonValueKind.Undefined)
                properUsername = "?????";
            else
                properUsername = usernameElement.GetString();
            if (properUsername != null)
                username = properUsername;

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
                output += "\n" + req.Title + " - " + req.GameTitle;
            }
            
            embed.WithDescription(output);
            if (level >= 85 && met.Count >= 1)
                embed.WithColor(Color.Green);
            else
                embed.WithColor(Color.Red);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = embed.Build();
            });
        }

    }

}

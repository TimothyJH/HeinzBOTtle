using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Requirements;
using System.Text.Json;

namespace HeinzBOTtle.Commands {

    class HBCommandReqs : HBCommand {

        public HBCommandReqs() : base("reqs") { }

        public override async Task ExecuteCommandAsync(SocketSlashCommand command) {
            await command.DeferAsync();
            string username = (string)command.Data.Options.First<SocketSlashCommandDataOption>();
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
                fail.WithDescription("Hypixel doesn't seem to have any information about this player. This player probably changed usernames, never logged into Hypixel, or doesn't exist.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
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

        public override SlashCommandProperties GenerateCommandProperties() {
            SlashCommandBuilder command = new SlashCommandBuilder();
            command.IsDefaultPermission = true;
            command.WithName(Name);
            command.WithDescription("This checks the guild requirements met by the provided player.");
            command.AddOption("username", ApplicationCommandOptionType.String, "The username of the player to check", isRequired: true);
            return command.Build();
        }

    }

}

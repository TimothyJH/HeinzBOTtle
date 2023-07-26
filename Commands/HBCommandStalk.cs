using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Leaderboards;

namespace HeinzBOTtle.Commands {

    class HBCommandStalk : HBCommand {

        public HBCommandStalk() : base("stalk") { }

        public override async Task ExecuteCommandAsync(SocketSlashCommand command) {
            await command.DeferAsync();
            if (HBData.LeaderboardsUpdating) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("This command cannot be used while the leaderboards are updating.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }
            if (HBData.LeaderboardRankings.Count == 0) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("The leaderboards have not been updated since the bot was last restarted, so the rankings are not currently stored in memory. " +
                    "Update the leaderboards with `/update-leaderboards` and try again after the update is complete.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }

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
            if (!HBData.LeaderboardRankings.ContainsKey(username.ToLower())) {
                EmbedBuilder fail = new EmbedBuilder();
                fail.WithDescription("That player was not in the guild at the time of the last leaderboards update.");
                fail.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = fail.Build();
                });
                return;
            }

            LBRankingData data = HBData.LeaderboardRankings[username.ToLower()];
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(data.ProperUsername);
            if (data.Rankings.Count == 0)
                embed.WithColor(Color.Red).WithDescription("does not have any guild leaderboard rankings.");
            else {
                string description = "has the following guild leaderboard ranking" + (data.Rankings.Count == 1 ? "" : "s") + ":\n";
                foreach (LBRanking ranking in data.Rankings)
                    description += "\n" + ranking.ToString();
                embed.WithColor(Color.Purple).WithDescription(description);
            }

            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = embed.Build();
            });
        }

        public override SlashCommandProperties GenerateCommandProperties() {
            SlashCommandBuilder command = new SlashCommandBuilder();
            command.IsDefaultPermission = true;
            command.WithName(Name);
            command.WithDescription("This checks the guild leaderboard rankings of the provided player.");
            command.AddOption("username", ApplicationCommandOptionType.String, "The username of the player to check", isRequired: true);
            return command.Build();
        }

    }

}

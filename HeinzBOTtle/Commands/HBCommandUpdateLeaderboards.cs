using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Leaderboards;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /update-leaderboards.
/// </summary>
public class HBCommandUpdateLeaderboards : HBCommand {

    public HBCommandUpdateLeaderboards() : base("update-leaderboards") { }

    public override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        if (!HBData.LeaderboardsUpdating.WaitOne(100)) {
            EmbedBuilder updating = new EmbedBuilder();
            updating.WithDescription("The leaderboards are already in the process of updating!");
            updating.WithColor(Color.Red);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = updating.Build();
            });
            return;
        }
        try {
            if (DateTime.Now.Ticks - HBData.LeaderboardsLastUpdated < 120L * 60L * 10000000L) {
                EmbedBuilder cooldown = new EmbedBuilder();
                cooldown.WithDescription("The leaderboards can only be updated once every 2 hours. Try again in " +
                    ((120L) - ((DateTime.Now.Ticks - HBData.LeaderboardsLastUpdated) / (60L * 10000000L)) + 1L) + " minute(s).");
                cooldown.WithColor(Color.Gold);
                await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                    p.Embed = cooldown.Build();
                });
                return;
            }

            HBData.LeaderboardsLastUpdated = DateTime.Now.Ticks;
            EmbedBuilder success = new EmbedBuilder();
            success.WithDescription("The leaderboards are now updating! The process should be complete in a few minutes.");
            success.WithColor(Color.Green);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = success.Build();
            });
        
            try {
                await LBMethods.UpdateLeaderboards();
            } catch (Exception e) {
                await HBData.Log.InfoAsync("There was an exception during the process of updating the leaderboards: " + e.Message);
                await HBData.Log.InfoAsync(e.StackTrace);
            }
        } finally {
            HBData.LeaderboardsUpdating.Release();
        }
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.DefaultMemberPermissions = GuildPermission.Administrator;
        command.WithName(Name);
        command.WithDescription("This updates the guild leaderboards and can be used once every 2 hours.");
        return command.Build();
    }

}

using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Ranks;
using System.Text.Json;

namespace HeinzBOTtle.Commands;

/// <summary>
/// Represents /promotions.
/// </summary>
public class HBCommandPromotions : HBCommand {

    public HBCommandPromotions() : base("promotions") { }

    public override async Task ExecuteCommandAsync(SocketSlashCommand command) {
        await command.DeferAsync();
        Json? guild = await HypixelMethods.RetrieveGuildAPI(HBData.HypixelGuildID);
        Task initialBuffer = Task.Delay(1000);
        if (guild == null || guild.GetBoolean("success") == false) {
            EmbedBuilder failInformation = new EmbedBuilder();
            failInformation.WithDescription("ERROR: Guild information retrieval unsuccessful :(");
            failInformation.WithColor(Color.Red);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = failInformation.Build();
            });
            return;
        }
        List<JsonElement>? members = guild.GetArray("guild.members");
        if (members == null || members.Count == 0) {
            EmbedBuilder failMemberList = new EmbedBuilder();
            failMemberList.WithDescription("ERROR: Guild member list retrieval unsuccessful :(");
            failMemberList.WithColor(Color.Red);
            await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
                p.Embed = failMemberList.Build();
            });
            Console.WriteLine("ERROR: Guild member list retrieval unsuccessful :(");
            return;
        }

        // Determining which players to investigate:
        List<(string, Rank, long)> candidates = new List<(string, Rank, long)>(); // string --> uuid, Rank --> rank, long --> joined
        foreach (JsonElement member in members) {
            if (member.ValueKind != JsonValueKind.Object)
                continue;

            Dictionary<string, JsonElement>? memberDic;
            try {
                memberDic = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(member);
            } catch {
                memberDic = null;
            }
            if (memberDic == null || !memberDic.ContainsKey("uuid") || !memberDic.ContainsKey("rank") || !memberDic.ContainsKey("joined"))
                continue;
            string? rankRaw = memberDic["rank"].GetString();
            if (rankRaw == null)
                continue;
            Rank? rank = RankMethods.StringToRank(rankRaw);
            if (rank == null || rank == Rank.Veteran)
                continue; // This continue does not imply an error like the other ones; it just doesn't make sense to check these players for promotions.
            string? uuid = memberDic["uuid"].GetString();
            long? joined = memberDic["joined"].GetInt64();
            if (uuid == null || joined == null)
                continue;
            candidates.Add((uuid, (Rank)rank, (long)joined));
        }

        // Evaluating each player:
        await initialBuffer;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<(string, Rank)> promoteNow = new List<(string, Rank)>(); // string --> username, Rank --> rank to which the player should be promoted right now
        List<(string, Rank, double)> promoteSoon = new List<(string, Rank, double)>(); // string --> username, Rank --> rank to which the player should be promoted later
                                                                                       // double --> how much later (in days)
        foreach ((string, Rank, long) playerInfo in candidates) { // string --> uuid, Rank --> rank, long --> joined
            Task buffer = Task.Delay(1000);
            Json? json = await HypixelMethods.RetrievePlayerAPI(playerInfo.Item1, uuid: true);
            if (json == null || json.GetBoolean("success") == false || json.GetValueKind("player") != JsonValueKind.Object)
                Console.WriteLine("Ignoring player " + playerInfo.Item1 + " in promotions evaluation");
            else {
                (double, Rank)? distance = RankMethods.CalculateDaysUntilPromotion(json, RankMethods.DaysSinceTimestamp(playerInfo.Item3, now), playerInfo.Item2);
                if (distance == null) {
                    await buffer;
                    continue;
                }
                if (distance.Value.Item1 <= 0.0) {
                    promoteNow.Add(((json.GetString("player.displayname") ?? "?????").Replace("_", "\\_"), distance.Value.Item2));
                    // This performs an additional future check in case this player is extremely overdue for the current promotion and will qualify for the next rank soon.
                    distance = RankMethods.CalculateDaysUntilPromotion(json, RankMethods.DaysSinceTimestamp(playerInfo.Item3, now), distance.Value.Item2); 
                }
                if (distance != null && distance.Value.Item1 <= 30.0)
                    promoteSoon.Add((json.GetString("player.displayname") ?? "?????", distance.Value.Item2, distance.Value.Item1));
            }
            await buffer;
        }

        // Displaying the results:
        EmbedBuilder results = new EmbedBuilder();
        results.WithTitle("Promotions Evaluation");
        results.WithTimestamp(now);
        if (promoteNow.Count == 0 && promoteSoon.Count == 0) {
            results.WithDescription("There are no current promotions or guaranteed upcoming promotions scheduled within the next 30 days.");
            results.WithColor(Color.Green);
        } else {
            string description = "";
            foreach ((string, Rank) result in promoteNow)
                description += $":star: **{result.Item1}** can be promoted to **{result.Item2}**!\n\n";
            foreach ((string, Rank, double) result in promoteSoon)
                description += $":warning: **{result.Item1}** will qualify for **{result.Item2}** in " +
                    $"{HypixelMethods.PadOneDecimalPlace(double.Round(result.Item3, 1, MidpointRounding.ToPositiveInfinity).ToString())} days.\n\n";
            results.WithDescription(description.TrimEnd());
            if (promoteNow.Count != 0)
                results.WithColor(Color.Purple);
            else
                results.WithColor(Color.Orange);
        }
        await command.ModifyOriginalResponseAsync(delegate (MessageProperties p) {
            p.Embed = results.Build();
        });
    }

    public override SlashCommandProperties GenerateCommandProperties() {
        SlashCommandBuilder command = new SlashCommandBuilder();
        command.IsDefaultPermission = false;
        command.WithName(Name);
        command.WithDescription("(May take multiple minutes to finish!) Reports whether any guild members should be promoted.");
        return command.Build();
    }

}

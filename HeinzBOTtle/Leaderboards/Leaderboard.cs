using Discord;
using HeinzBOTtle.Hypixel;

namespace HeinzBOTtle.Leaderboards;

public abstract class Leaderboard {

    protected List<PlayerEntry> board;
    
    public string GameTitle { get; }
    public string GameStat { get; }
    public Color Color { get; }
    public bool IsReversed { get; }

    public Leaderboard(string gameTitle, string gameStat, Color color, bool isReversed) {
        GameTitle = gameTitle;
        GameStat = gameStat;
        Color = color;
        IsReversed = isReversed;
        board = new List<PlayerEntry>();
    }

    public void Reset() {
        board.Clear();
    }

    public Embed GenerateHeaderEmbed() {
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithColor(Color).WithTitle(GameTitle);
        if (!GameStat.Equals(""))
            embed.WithDescription(GameStat);
        return embed.Build();
    }

    public List<Embed> GenerateScoresEmbeds() {
        if (board.Count == 0) {
            EmbedBuilder empty = new EmbedBuilder();
            empty.WithDescription("This leaderboard is empty. :(");
            return new List<Embed> { empty.Build() };
        }
        List<Embed> embeds = new List<Embed>();
        EmbedBuilder? current = null;
        string currentDescription = "";
        int tiebreakerValue = -1;
        int tiebreakerIndex = 0;
        for (int i = 0; i < board.Count; i++) {
            if (current == null) {
                current = new EmbedBuilder();
                current.WithColor(Color);
                currentDescription = "";
            }

            PlayerEntry entry = board[i];
            if (entry.Stat != tiebreakerValue) {
                currentDescription += "`#" + LBMethods.FormatPosition(i + 1) + ":` " + entry.Username.Replace("_", "\\_") + " (" + GenerateDisplayStat(entry) + ")\n";
                tiebreakerValue = entry.Stat;
                tiebreakerIndex = i;
            } else
                currentDescription += "`#" + LBMethods.FormatPosition(tiebreakerIndex + 1) + ":` " + entry.Username.Replace("_", "\\_") + " (" + GenerateDisplayStat(entry) + ")\n";
            
            if ((i + 1) % 25 == 0) {
                current.WithDescription(currentDescription.TrimEnd());
                embeds.Add(current.Build());
                current = null;
            }
        }
        if (current != null) {
            current.WithDescription(currentDescription.TrimEnd());
            embeds.Add(current.Build());
        }
        return embeds;
    }

    public void EnterPlayer(Json player) {
        PlayerEntry? entry = CalculatePlayer(player);
        if (entry == null)
            return;
        for (int i = board.Count - 1; i >= -1; i--) {
            if (i == -1) {
                board.Insert(0, entry);
                break;
            }
            if (IsReversed ? (entry >= board[i]) : (entry <= board[i])) {
                board.Insert(i + 1, entry);
                break;
            }
        }
    }

    public Dictionary<string, LBRanking> GenerateRankings() {
        Dictionary<string, LBRanking> rankings = new Dictionary<string, LBRanking>();
        int tiebreakerValue = -1;
        int tiebreakerIndex = 0;
        for (int i = 0; i < board.Count; i++) {
            PlayerEntry entry = board[i];
            if (entry.Stat != tiebreakerValue) {
                rankings.Add(entry.Username.ToLower(), new LBRanking(i + 1, GameTitle, GameStat));
                tiebreakerValue = entry.Stat;
                tiebreakerIndex = i;
            } else
                rankings.Add(entry.Username.ToLower(), new LBRanking(tiebreakerIndex + 1, GameTitle, GameStat));
        }
        return rankings;
    }

    public async Task<Dictionary<string, LBRanking>> GenerateRankingsFromThreadAsync(IThreadChannel thread, bool initializePlayers) {
        Dictionary<string, LBRanking> rankings = new Dictionary<string, LBRanking>();
        List<IMessage> messages = new List<IMessage>();
        List<IReadOnlyCollection<IMessage>> pages = await thread.GetMessagesAsync(5).ToListAsync();
        foreach (IReadOnlyCollection<IMessage> page in pages) {
            foreach (IMessage message in page)
                messages.Add(message);
        }
        foreach (IMessage message in messages) {
            if (message.Embeds.Count == 0)
                continue;
            string description = message.Embeds.First().Description;
            if (!description.StartsWith('`'))
                continue;
            string[] entries = description.Split('\n');
            foreach (string entry in entries) {
                string[] components = entry.Split(' ', 3); // 0 is position prefix, 1 is username
                if (initializePlayers)
                    HBData.LeaderboardRankings.Add(components[1].ToLower(), new LBRankingData(components[1]));
                rankings.Add(components[1].ToLower(), new LBRanking(int.Parse(components[0][2..5]), GameTitle, GameStat));
            }
        }
        return rankings;
    }

    public virtual string GenerateDisplayStat(PlayerEntry entry) {
        return HypixelMethods.WithCommas(entry.Stat);
    }

    protected abstract PlayerEntry? CalculatePlayer(Json player);

}

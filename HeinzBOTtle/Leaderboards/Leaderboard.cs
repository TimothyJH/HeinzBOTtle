using Discord;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Statics;

namespace HeinzBOTtle.Leaderboards;

/// <summary>
/// Represents a leaderboard. Entries on leaderboards are dynamic, but the game and statistic associated with a leaderboard are fixed.
/// </summary>
public abstract class Leaderboard {

    /// <summary>The clean name of the Hypixel minigame represented by this leaderboard.</summary>
    public string GameTitle { get; }
    /// <summary>The clean name of the statistic represented by this leaderboard.</summary>
    public string GameStat { get; }
    /// <summary>The color to use for the Discord text embeds in the leaderboard messages.</summary>
    public Color Color { get; }
    /// <summary>True if higher statistics are associated with inferior ranks.</summary>
    public bool IsReversed { get; }

    /// <summary>The ordered list defining the contents of the leaderboard.</summary>
    protected List<PlayerEntry> board;

    public Leaderboard(string gameTitle, string gameStat, Color color, bool isReversed) {
        GameTitle = gameTitle;
        GameStat = gameStat;
        Color = color;
        IsReversed = isReversed;
        board = new List<PlayerEntry>();
    }

    /// <summary>Removes all entries in the leaderboard, reverting it to its original state.</summary>
    public void Reset() {
        board.Clear();
    }

    /// <returns>The embed to be used as the header of this leaderboard.</returns>
    public Embed GenerateHeaderEmbed() {
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithColor(Color).WithTitle(GameTitle);
        if (!GameStat.Equals(""))
            embed.WithDescription(GameStat);
        return embed.Build();
    }

    /// <returns>The ordered list of embeds to display in the leaderboard's thread.</returns>
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
                currentDescription += $"`#{(i + 1):D3}:` {entry.Username.Replace("_", "\\_")} ({GenerateDisplayStat(entry)})\n";
                tiebreakerValue = entry.Stat;
                tiebreakerIndex = i;
            } else
                currentDescription += $"`#{(tiebreakerIndex + 1):D3}:` {entry.Username.Replace("_", "\\_")} ({GenerateDisplayStat(entry)})\n";            
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

    /// <summary>Inserts the player represented by the provided JSON into the leaderboard at the appropriate position.</summary>
    /// <param name="player">The JSON representing the player to insert</param>
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

    /// <summary>The result of this method is to be merged into <see cref="HBData.LeaderboardRankings"/>.</summary>
    /// <returns>A dictionary mapping players' normalized usernames to their rankings on this leaderboard.</returns>
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

    /// <summary>The result of this method is to be merged into <see cref="HBData.LeaderboardRankings"/>.</summary>
    /// <param name="thread">The thread to use to generate the rankings</param>
    /// <param name="initializePlayers">True only if this method should create the initial entries in <see cref="HBData.LeaderboardRankings"/>.</param>
    /// <returns>A dictionary mapping players' normalized usernames to their rankings based on the provided leaderboard thread.</returns>
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

    /// <param name="entry">The JSON representing the player whose statistic to format</param>
    /// <returns>The properly formatted statistic to be displayed corresponding to the player represented by the provided JSON.</returns>
    public virtual string GenerateDisplayStat(PlayerEntry entry) {
        return HypixelMethods.WithCommas(entry.Stat);
    }

    /// <summary>This should only ever be called by <see cref="Leaderboard.EnterPlayer(Json)"/>.</summary>
    protected abstract PlayerEntry? CalculatePlayer(Json player);

}

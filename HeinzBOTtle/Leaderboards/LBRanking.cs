namespace HeinzBOTtle.Leaderboards;

public class LBRanking {

    public int Position { get; }
    public string GameTitle { get; }
    public string GameStat { get; }

    public LBRanking(int position, string gameTitle, string gameStat) {
        Position = position;
        GameTitle = gameTitle;
        GameStat = gameStat;
    }

    public override string ToString() {
        return "`#" + LBMethods.FormatPosition(Position) + $"` in {GameTitle}" + (GameStat.Equals("") ? "" : $" ({GameStat})");
    }

    public static bool operator <(LBRanking a, LBRanking b) => a.Position < b.Position;
    public static bool operator >(LBRanking a, LBRanking b) => a.Position > b.Position;
    public static bool operator <=(LBRanking a, LBRanking b) => a.Position <= b.Position;
    public static bool operator >=(LBRanking a, LBRanking b) => a.Position >= b.Position;

}

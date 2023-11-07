namespace HeinzBOTtle.Leaderboards;

public class PlayerEntry {

    public string Username { get; }
    public int Stat { get; }

    public PlayerEntry(string username, int stat) {
        Username = username;
        Stat = stat;
    }

    public static bool operator <(PlayerEntry a, PlayerEntry b) => a.Stat < b.Stat;
    public static bool operator >(PlayerEntry a, PlayerEntry b) => a.Stat > b.Stat;
    public static bool operator <=(PlayerEntry a, PlayerEntry b) => a.Stat <= b.Stat;
    public static bool operator >=(PlayerEntry a, PlayerEntry b) => a.Stat >= b.Stat;

}

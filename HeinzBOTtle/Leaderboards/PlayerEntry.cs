namespace HeinzBOTtle.Leaderboards;

public class PlayerEntry {

    /// <summary>The clean username of the player in this entry.</summary>
    public string Username { get; }
    public int Stat { get; }

    /// <summary>The stat associated with the player in this entry.</summary>
    public PlayerEntry(string username, int stat) {
        Username = username;
        Stat = stat;
    }

    public static bool operator <(PlayerEntry a, PlayerEntry b) => a.Stat < b.Stat;
    public static bool operator >(PlayerEntry a, PlayerEntry b) => a.Stat > b.Stat;
    public static bool operator <=(PlayerEntry a, PlayerEntry b) => a.Stat <= b.Stat;
    public static bool operator >=(PlayerEntry a, PlayerEntry b) => a.Stat >= b.Stat;

}

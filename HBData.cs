using Discord;
using Discord.WebSocket;
using HeinzBOTtle.Commands;
using HeinzBOTtle.Leaderboards;
using HeinzBOTtle.Requirements;

namespace HeinzBOTtle {

    static class HBData {

        // Clients
        public static DiscordSocketClient DiscordClient { get; } = new DiscordSocketClient();
        public static HttpClient HttpClient { get; } = new HttpClient();
        
        // Config
        public static string HypixelKey { get; set; } = "";
        public static string DiscordToken { get; set; } = "";
        public static string HypixelGuildID { get; set; } = "";
        public static ulong DiscordGuildID { get; set; } = 0;
        public static ulong LeaderboardsChannelID { get; set; } = 0;

        // Assets
        public static List<HBCommand> HBCommandList { get; } = GenerateHBCommandList();
        public static List<Requirement> RequirementList { get; } = GenerateRequirementList();
        public static List<Leaderboard> LeaderboardList { get; } = GenerateLeaderboardList();

        // Other
        public static Dictionary<string, CachedPlayerInfo> PlayerCache { get; } = new Dictionary<string, CachedPlayerInfo>();
        public static Dictionary<string, LBRankingData> LeaderboardRankings { get; } = new Dictionary<string, LBRankingData>();
        public static Dictionary<string, int> QuestParticipationsLeaderboardMap { get; } = new Dictionary<string, int>();
        public static bool LeaderboardsUpdating { get; set; } = false;
        public static long LeaderboardsLastUpdated { get; set; } = 0;

        // Asset Generators
        private static List<HBCommand> GenerateHBCommandList() {
            List<HBCommand> list = new List<HBCommand>() {
                new HBCommandReqs(),
                new HBCommandStalk(),
                new HBCommandUpdateLeaderboards()
            };
            return list;
        }

        private static List<Requirement> GenerateRequirementList() {
            List<Requirement> list = new List<Requirement> {
                new SimpleRequirement("8-bit", "Arcade", "player.achievements.arcade_arcade_winner", 500),
                new SimpleRequirement("Cookie Clicker", "The Pit", "player.achievements.pit_prestiges", 8),
                new SimpleRequirement("Rush B", "Cops and Crims", "player.stats.MCGO.game_wins", 500),
                new SimpleRequirement("Red is Sus", "Murder Mystery", "player.stats.MurderMystery.wins", 750),
                new SimpleRequirement("MMA", "Arena Brawl", "player.stats.Arena.wins", 300),
                new SimpleRequirement("Blue shell", "Turbo Kart Racers", "player.achievements.gingerbread_winner", 300),
                new SimpleRequirement("Sonic", "Speed UHC", "player.stats.SpeedUHC.wins", 250),
                new AdditiveRequirement("Mockingjay", "Blitz Survival Games", "player.achievements.blitz_wins", "player.achievements.blitz_wins_teams", 300),
                new SimpleRequirement("Short fuse", "TNT Games", "player.stats.TNTGames.wins", 300),
                new SimpleRequirement("Dreamer", "Bed Wars", "player.achievements.bedwars_wins", 1000),
                new SimpleRequirement("Icarus", "SkyWars", "player.stats.SkyWars.wins", 1000),
                new SimpleRequirement("Trap card", "Duels", "player.achievements.duels_duels_winner", 4000),
                new AdditiveRequirement("Van Helsing", "VampireZ", "player.stats.VampireZ.human_wins", "player.stats.VampireZ.vampire_wins", 150),
                new CompoundRequirement("Sniper", "Quakecraft", "player.achievements.quake_wins", 150, "player.achievements.quake_kills", 10000),
                new SimpleRequirement("Time traveler", "Crazy Walls", "player.stats.TrueCombat.wins", 100),
                new SimpleRequirement("John Smith", "SkyClash", "player.achievements.skyclash_wins", 100),
                new CompoundRequirement("Champion", "UHC", "player.stats.UHC.wins", 5, "player.stats.UHC.kills", 50),
                new SimpleRequirement("Ares", "The Walls", "player.achievements.walls_wins", 100),
                new SimpleRequirement("Pacifist", "Build Battle", "player.achievements.buildbattle_build_battle_score", 7500),
                new SimpleRequirement("Warrior", "Warlords", "player.stats.Battleground.wins", 150),
                new SimpleRequirement("Final destination", "Smash Heroes", "player.stats.SuperSmash.smashLevel", 150),
                new SimpleRequirement("Hades", "Mega Walls", "player.achievements.walls3_wins", 50),
                new CompoundRequirement("Snow baller", "Paintball Warfare", "player.achievements.paintball_wins", 300, "player.achievements.paintball_kills", 10000),
                new SimpleRequirement("Yggdrasil", "SkyBlock", "player.achievements.skyblock_sb_levels", 120),
                new SimpleRequirement("Shepherd", "Wool Wars", "player.stats.WoolGames.wool_wars.stats.wins", 300)
            };
            return list;
        }

        private static List<Leaderboard> GenerateLeaderboardList() {
            return new List<Leaderboard>() {
                new SimpleLeaderboard("Achievement Points", "", Color.DarkPurple, "player.achievementPoints"),
                new SimpleLeaderboard("Arcade", "Wins", Color.Magenta, "player.achievements.arcade_arcade_winner"),
                new SimpleLeaderboard("Arena Brawl", "Wins", Color.Orange, "player.stats.Arena.wins"),
                new AverageLeaderboardPositionLeaderboard(),
                new BedWarsLevelLeaderboard(),
                new SimpleLeaderboard("Blitz Survival Games", "Kills", Color.Red, "player.achievements.blitz_kills"),
                new SimpleLeaderboard("Build Battle", "Score", Color.DarkGreen, "player.achievements.buildbattle_build_battle_score"),
                new SimpleLeaderboard("Cops and Crims", "Kills", Color.Gold, "player.stats.MCGO.kills"),
                new SimpleLeaderboard("Duels", "Wins", Color.DarkRed, "player.achievements.duels_duels_winner"),
                new GuildQuestChallengesCompletedLeaderboard(),
                new HeinzReqsLeaderboard(),
                new HypixelLevelLeaderboard(),
                new SimpleLeaderboard("Karma", "", Color.DarkPurple, "player.karma"),
                new SimpleLeaderboard("Mega Walls", "Wins", Color.Green, "player.achievements.walls3_wins"),
                new SimpleLeaderboard("Murder Mystery", "Wins", Color.DarkMagenta, "player.stats.MurderMystery.wins"),
                new SimpleLeaderboard("Paintball Warfare", "Kills", Color.Teal, "player.achievements.paintball_kills"),
                new SimpleLeaderboard("The Pit", "Total Experience", Color.Gold, "player.stats.Pit.profile.xp"),
                new SimpleLeaderboard("Quakecraft", "Kills", Color.LightOrange, "player.achievements.quake_kills"),
                new SimpleLeaderboard("Quests Completed", "", Color.DarkPurple, "player.achievements.general_quest_master"),
                new SkyWarsLevelLeaderboard(),
                new SimpleLeaderboard("SkyWars", "Lucky Block Wins", Color.Teal, "player.stats.SkyWars.lab_win_lucky_blocks_lab"),
                new SimpleLeaderboard("SkyBlock", "Level", Color.Green, "player.achievements.skyblock_sb_levels"),
                new SimpleLeaderboard("Smash Heroes", "Level", Color.DarkGreen, "player.stats.SuperSmash.smashLevel"),
                new SimpleLeaderboard("Smash Heroes", "Wins", Color.DarkGreen, "player.stats.SuperSmash.wins"),
                new SimpleLeaderboard("Speed UHC", "Score", Color.Orange, "player.stats.SpeedUHC.score"),
                new SimpleLeaderboard("Turbo Kart Racers", "Trophies", Color.Blue, "player.achievements.gingerbread_winner"),
                new SimpleLeaderboard("TNT Games", "Wins", Color.Red, "player.stats.TNTGames.wins"),
                new SimpleLeaderboard("UHC", "Score", Color.DarkOrange, "player.stats.UHC.score"),
                new SimpleLeaderboard("VampireZ", "Human Wins", Color.DarkMagenta, "player.achievements.vampirez_survivor_wins"),
                new SimpleLeaderboard("The Walls", "Wins", Color.Gold, "player.achievements.walls_wins"),
                new SimpleLeaderboard("Warlords", "Wins", Color.Purple, "player.stats.Battleground.wins"),
                new WoolWarsLevelLeaderboard()
            };
        }

    }

}

namespace HeinzBOTtle.Requirements {

    class ReqMethods {

        public static List<Requirement> GenerateRequirementList() {
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

        public static List<Requirement> GetRequirementsMet(string playerJson) {
            List<Requirement> met = new List<Requirement>();
            foreach (Requirement req in Requirement.RequirementList) {
                if (req.MeetsRequirement(playerJson))
                    met.Add(req);
            }
            return met;
        }

        public static string FormatRequirementsList(List<Requirement> list) {
            if (list.Count == 0)
                return "";
            string formatted = "";
            foreach (Requirement req in list)
                formatted += req.GameTitle + ", ";
            return formatted.Substring(0, formatted.Length - 2);
        }

    }

}

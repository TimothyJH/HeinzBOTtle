using System.Collections.Generic;

namespace HeinzBOTtle.Requirements
{

    class ReqMethods
    {

        public static List<Requirement> GenerateRequirementList()
        {
            List<Requirement> list = new List<Requirement>();
            list.Add(new SimpleRequirement("8-bit", "Arcade", "player.achievements.arcade_arcade_winner", 500));
            list.Add(new PitRequirement());
            list.Add(new SimpleRequirement("Rush B", "Cops and Crims", "player.stats.MCGO.game_wins", 500));
            list.Add(new SimpleRequirement("Red is Sus", "Murder Mystery", "player.stats.MurderMystery.wins", 750));
            list.Add(new SimpleRequirement("MMA", "Arena Brawl", "player.stats.Arena.wins", 300));
            list.Add(new SimpleRequirement("Blue shell", "Turbo Kart Racers", "player.stats.GingerBread.wins", 300));
            list.Add(new SimpleRequirement("Sonic", "Speed UHC", "player.stats.SpeedUHC.wins", 250));
            list.Add(new SimpleRequirement("Mockingjay", "Blitz Survival Games", "player.stats.HungerGames.wins", 300));
            list.Add(new SimpleRequirement("Short fuse", "TNT Games", "player.stats.TNTGames.wins", 300));
            list.Add(new SimpleRequirement("Dreamer", "Bed Wars", "player.stats.Bedwars.wins_bedwars", 1000));
            list.Add(new SimpleRequirement("Icarus", "SkyWars", "player.stats.SkyWars.wins", 1000));
            list.Add(new SimpleRequirement("Trap card", "Duels", "player.stats.Duels.wins", 4000));
            list.Add(new VZRequirement());
            list.Add(new CompoundRequirement("Sniper", "Quakecraft", "player.stats.Quake.wins", 150, "player.stats.Quake.kills", 10000));
            list.Add(new SimpleRequirement("Time traveler", "Crazy Walls", "player.stats.TrueCombat.wins", 100));
            list.Add(new SimpleRequirement("John Smith", "SkyClash", "player.stats.SkyClash.wins", 100));
            list.Add(new CompoundRequirement("Champion", "UHC", "player.stats.UHC.wins", 5, "player.stats.UHC.kills", 50));
            list.Add(new SimpleRequirement("Ares", "The Walls", "player.stats.Walls.wins", 100));
            list.Add(new SimpleRequirement("Pacifist", "Build Battle", "player.stats.BuildBattle.score", 7500));
            list.Add(new SimpleRequirement("Warrior", "Warlords", "player.stats.Battleground.wins", 150));
            list.Add(new SimpleRequirement("Final destination", "Smash Heroes", "player.stats.SuperSmash.smashLevel", 150));
            list.Add(new SimpleRequirement("Hades", "Mega Walls", "player.stats.Walls3.wins", 50));
            list.Add(new CompoundRequirement("Snow baller", "Paintball Warfare", "player.stats.Paintball.wins", 300, "player.stats.Paintball.kills", 10000));
            // SkyBlock Requirement (lol)
            list.Add(new SimpleRequirement("Shepherd", "Wool Wars", "player.stats.WoolGames.wool_wars.stats.wins", 300));
            return list;
        }

        public static List<Requirement> GetRequirementsMet(string playerJSON)
        {
            List<Requirement> met = new List<Requirement>();
            foreach (Requirement req in Requirement.RequirementList)
            {
                if (req.MeetsRequirement(playerJSON))
                    met.Add(req);
            }
            return met;
        }

    }

}

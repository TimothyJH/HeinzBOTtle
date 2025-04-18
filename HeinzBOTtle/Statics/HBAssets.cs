﻿using Discord;
using HeinzBOTtle.Commands;
using HeinzBOTtle.Hypixel;
using HeinzBOTtle.Leaderboards;
using HeinzBOTtle.Leaderboards.Special;
using HeinzBOTtle.Requirements;
using System.Collections.Immutable;

namespace HeinzBOTtle.Statics;

public static class HBAssets {

    /// <summary>The list of active Discord slash command implementers.</summary>
    public static ImmutableList<HBCommand> HBCommandList { get; } = GenerateHBCommandList();
    /// <summary>The list of active guild requirements.</summary>
    public static ImmutableList<Requirement> RequirementList { get; } = GenerateRequirementList();
    /// <summary>The list of active guild leaderboards.</summary>
    public static ImmutableList<Leaderboard> LeaderboardList { get; } = GenerateLeaderboardList();
    public static ImmutableList<string> NecessaryRoles { get; } = GenerateNecessaryRolesList();

    private static ImmutableList<HBCommand> GenerateHBCommandList() {
        return [
            new HBCommandLinkMinecraft(),
            new HBCommandLinkNewUser(),
            new HBCommandModifyUser(),
            new HBCommandPromotions(),
            new HBCommandReqs(),
            new HBCommandSetSignatureColor(),
            new HBCommandStalk(),
            new HBCommandUpdate(),
            new HBCommandUpdateLeaderboards(),
            new HBCommandUpdateUser(),
            new HBCommandUserInfo()
        ];
    }

    private static ImmutableList<Leaderboard> GenerateLeaderboardList() {
        return [
            new SimpleLeaderboard("Achievement Points", "", Color.DarkPurple, "player.achievementPoints"),
            new SimpleLeaderboard("Arcade", "Wins", Color.Magenta, "player.achievements.arcade_arcade_winner"),
            new SimpleLeaderboard("Arena Brawl", "Wins", Color.Orange, "player.achievements.arena_gladiator"),
            new AverageLeaderboardPositionLeaderboard(),
            new BedWarsLevelLeaderboard(),
            new SimpleLeaderboard("Blitz Survival Games", "Kills", Color.Red, "player.achievements.blitz_kills"),
            new SimpleLeaderboard("Build Battle", "Score", Color.DarkGreen, "player.achievements.buildbattle_build_battle_score"),
            new SimpleLeaderboard("Cops and Crims", "Kills", Color.Gold, "player.achievements.copsandcrims_serial_killer"),
            new SimpleLeaderboard("Duels", "Wins", Color.DarkRed, "player.achievements.duels_duels_winner"),
            new GuildQuestChallengesCompletedLeaderboard(),
            new HeinzRequirementsLeaderboard(),
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
            new SimpleLeaderboard("Smash Heroes", "Wins", Color.DarkGreen, "player.achievements.supersmash_smash_winner"),
            new SimpleLeaderboard("Speed UHC", "Score", Color.Orange, "player.stats.SpeedUHC.score"),
            new SimpleLeaderboard("Turbo Kart Racers", "Trophies", Color.Blue, "player.achievements.gingerbread_winner"),
            new SimpleLeaderboard("TNT Games", "Wins", Color.Red, "player.stats.TNTGames.wins"),
            new SimpleLeaderboard("UHC", "Score", Color.DarkOrange, "player.stats.UHC.score"),
            new SimpleLeaderboard("VampireZ", "Human Wins", Color.DarkMagenta, "player.achievements.vampirez_survivor_wins"),
            new SimpleLeaderboard("The Walls", "Wins", Color.Gold, "player.achievements.walls_wins"),
            new SimpleLeaderboard("Warlords", "Wins", Color.Purple, "player.stats.Battleground.wins"),
            new WoolGamesCombinedWinsLeaderboard(),
            new WoolGamesLevelLeaderboard()
        ];
    }

    private static ImmutableList<string> GenerateNecessaryRolesList() {
        List<string> list = new List<string>() {
            "Guest", "Guild Member", "Honorary Quest", "Treehard", "Treehard+", "Challenger", "Leaderboarder"
        };
        foreach (Requirement req in RequirementList)
            list.Add(req.Title);
        return [.. list];
    }

    private static ImmutableList<Requirement> GenerateRequirementList() {
        return [
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
            new PredicatedRequirement("Shepherd", "Wool Games", player => HypixelMethods.GetTotalWoolGamesWins(player) >= 600)
        ];
    }

}

# HeinzBOTtle

Hello! HeinzBOTtle is the software that drives a Discord bot (technically "application" now) of the same name that provides services for the **Gremlins Of Heinz** Discord guild ("server"), a community originating as a Hypixel guild. These services include member requirement validation, the provision of updatable member leaderboards, time calculations for member promotions, and account linking for automatic role application. If you are a member of this server and/or the in-game Gremlins Of Heinz guild on Hypixel, you are probably aware of this bot. If you are a member but are not aware of it, I wonder how you found out about this repository! If you are here for some other reason, welcome!

Despite the project being named "HeinzBOTtle", it is not affiliated with Kraft Heinz. The name is a pun, indicating that it is a *BOT* (a Discord application) for *Heinz* (the guild), but it is styled in such a way that one might be reminded of a Heinz (brand) product (such as "a *Heinz bottle* of ketchup").

## Running HeinzBOTtle on Your Own

If you'd like, you can run your own copy of HeinzBOTtle! You'll need a few things first:

1. An executable. This is a .NET 8 console project. The actual project directory is the one labeled "HeinzBOTtle", which is in the same directory as this README file. Upon cloning this repository, you should be able to create an executable using a tool such as `dotnet publish` (to be executed in `./HeinzBOTtle`) or Visual Studio.
2. A [Discord application token](https://discord.com/developers/applications). The program uses this to control the Discord bot.
3. A [Hypixel API key](https://developer.hypixel.net/). The program uses this to make requests to the Hypixel API. Note that HeinzBOTtle may qualify as "third-party software", so I would recommend making sure that you are compliant with the developer terms.
4. Access to a user on a MySqlConnector-compatible SQL server (such as MySQL or MariaDB) with all privileges to a particular database granted. Technically, not all privileges are necessary, but the ones that are required for proper functionality may change over time.
5. A Discord guild (the "server" kind, not the group kind) with the token-associated bot joined to it. The bot will require certain permissions for certain channels, but that is discussed later in the setup.

After acquiring the appropriate assets, put the executable in a directory by itself. Then, create a file named `config.json` in the same directory. This will hold information that the program needs to operate. In that file, create two properties `DiscordToken` (string) and `DiscordGuildID` (number) with their appropriate values.

For this example, I will pretend that the Discord application token is `abc123` and that the Discord guild's ID is `321`. I will also make up other values for the complete example later.

```json
{
  "DiscordToken": "abc123",
  "DiscordGuildID": 321
}
```

Now, the IDs for the roles that are managed by the bot have to be provided in this same configuration file. If the names of the roles are the same as how they are represented in the program, the IDs can be automatically extracted by running the program with the `-r` flag, which will provide text that can be directly pasted into the config file. If not, the role IDs should be manually provided. If there are any roles missing from the config, the program will tell you which ones are missing and then exit, as they are all required for a proper startup.

`HypixelKey` should be assigned to the Hypixel API key. `LogDestinationPath` should be assigned to a filesystem path to a directory, to which logs will be moved when the program exits. `DatabaseLogin` should be assigned the credentials for logging into the SQL server in the form `IP[:PORT] DATABASENAME USERNAME PASSWORD`, where the port is optional (defaulting to 3306).

The various channel IDs should be provided in the config file as well, and the bot should have the following minimum permissions for those channels:

| Member                  | Channel Purpose           | Minimum Permissions                                                                                                                              | Example ID |
| ----------------------- | ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ---------- |
| `LeaderboardsChannelID` | Reserved for leaderboards | View Channel, Send Messages, Send Messages in Threads, Create Public Threads, Embed Links, Manage Messages, Manage Threads, Read Message History | 1          |
| `AchievementsChannelID` | Thread creation on posts  | View Channel, Create Public Threads, Read Message History                                                                                        | 2          |
| `LogsChannelID`         | Logging database changes  | View Channel, Send Messages, Embed Links                                                                                                         | 3          |
| `ReviewChannelID`       | Presenting link requests  | View Channel, Send Messages, Embed Links, Add Reactions, Manage Messages, Read Message History                                                   | 4          |

An example of a complete configuration file is provided here.

```json
{
  "HypixelKey": "xyz",
  "DiscordToken": "abc123",
  "HypixelGuildID": "xyzabc",
  "DiscordGuildID": 321,
  "LeaderboardsChannelID": 1,
  "AchievementsChannelID": 2,
  "LogsChannelID": 3,
  "LogDestinationPath": "/home/heinzbottlefan/logs",
  "DatabaseLogin": "192.168.12.34 databasename username password",
  "ReviewChannelID": 4,
  "Roles": {
    "John Smith": 100,
    "Snow baller": 101,
    "Icarus": 102,
    "Shepherd": 103,
    "Warrior": 104,
    "Blue shell": 105,
    "Champion": 106,
    "Time traveler": 107,
    "Ares": 108,
    "Dreamer": 109,
    "Short fuse": 110,
    "Treehard\u002B": 111,
    "Treehard": 112,
    "Pacifist": 113,
    "Red is Sus": 114,
    "MMA": 115,
    "8-bit": 116,
    "Trap card": 117,
    "Sonic": 118,
    "Rush B": 119,
    "Van Helsing": 120,
    "Cookie Clicker": 121,
    "Hades": 122,
    "Mockingjay": 123,
    "Final destination": 124,
    "Challenger": 125,
    "Leaderboarder": 126,
    "Sniper": 127,
    "Yggdrasil": 128,
    "Guild Member": 129,
    "Guest": 130,
    "Honorary Quest": 131
  }
}
```

Note that the program will complain and let you know what is missing if something is missing. This should be helpful if a future update requires more information.

At this point, running the program without any arguments should cause it to start normally! To add the bot's commands to the server, use the `update-commands` command in the console. Other console commands can be displayed with `help`.

Using the `shutdown` command causes a graceful shutdown, which should be preferred over killing the process in other ways because using `shutdown` moves the log files to the proper location.

## Design Overview

This section will discuss how certain parts of the software work at a high level, taking design into account. I will refer to symbols by their name qualified by their namespace. For example, I would refer to the `UpdateLeaderboards` method in the `LBMethods` class as `HeinzBOTtle.Leaderboards.LBMethods.UpdateLeaderboards`. Note that classes are in files that share their name and that their paths relative to this README file accurately reflect their namespaces. As such, you would be able to find this `UpdateLeaderboards` method somewhere in the file `./HeinzBOTtle/Leaderboards/LBMethods.cs`. The goal is that this should (hopefully) make it easier to find what I'm talking about in the code.

HeinzBOTtle primarily functions by reacting to Discord-related C# events, which are provided by the Discord.NET library. All of the relevant event subscribers can be found in the `HeinzBOTtle.EventHandlers` class, so you should start there if you would like to trace the steps a particular interaction with the bot. The subscriber methods call other methods that can be found throughout this project, but the subscribers themselves are not written outside of `HeinzBOTtle.EventHandlers` (except for one in `HeinzBOTtle.Program` due to having to pass an argument). The subscribers are registered to the Discord client during startup in `HeinzBOTtle.Program`.

### Startup

The program's entry point is `HeinzBOTtle.Program.Main`. Since (pretty much) everything in this program is asynchronous, the `Program` is immediately instantiated, and its `HeinzBOTtle.Program.MainAsync` method is called. This effectively hands off the execution of the program to an asynchronous method, meaning that other asynchronous methods can be called from it without any extra effort.

At this point, if the `-r` flag was passed as an argument to the program, the program abandons normal execution in order to collect the role IDs in the target Discord guild, then terminates. This is special functionality that can be used to speed up the setup process for a new Discord guild. If that flag has not been passed, the program resumes with the following major setup steps:

1. Starting the logger
2. Loading config variables to be accessible via `HeinzBOTtle.Statics.HBConfig`, which can then be accessed anywhere in the program as needed
3. Initializing a connection to the database, updating its schema if the program has a new structure for it.
4. Starting up the client that interfaces with Discord, including the attachment of event handlers
5. Starting the console

I would like to point out that the static accessibility of data in the `HeinzBOTtle.Statics` namespace is definitely a less-than-ideal and questionable solution, but it seemed to be the most promising for the scope of this project. It is definitely more maintainable than painfully coupled spaghetti code, but it is not that modular, which, for the time being, feels acceptable.

After the console starts, the program simply idles until it receives either an event from Discord, an event from the database, or a command from the console.

### Discord Command Processing

The `HeinzBOTtle.Statics.HBAssets.HBCommandList` list holds all of the command handlers, with each element being an implementation of the abstract `HeinzBOTtle.Commands.HBCommand` class. Each slash command has its own corresponding `HBCommand`, which contains methods both to setup and run the command as well as a guild-wide cooldown value.

When someone runs a slash command to be processed by HeinzBOTtle, the command is initially received at `HeinzBOTtle.EventHandlers.DSlashCommandExecutedEventAsync`. From there, a search is done to find out which `HBCommand` should handle it. If the command is not on cooldown, the `HBCommand` runs its `ExecuteCommandSafelyAsync` method to process the command in a way such that the following safety measures are fulfilled:

1. If the command needs to write to the database, a semaphore is acquired so that multiple database-writing commands don't execute at the same time.
2. If the command encounters an unhandled exception, the exception is logged, and a generic error message is shown instead of the command eventually timing out after 15 minutes.

### Interaction with the Hypixel API

When some part of the program needs to access the Hypixel API, it calls `RetrievePlayerAPI`, `RetrieveGuildAPI`, or `RetrieveLeaderboardsAPI` from `HeinzBOTtle.Hypixel.HypixelMethods`, depending on whether it is looking for player, guild, or in-game leaderboards information. Players can be queried by username or by UUID, and guilds can be queried by their internal database ID. There are no query options for in-game leaderboards. If a particular request with the same parameters as a new one was already made within the last 10 minutes, the API is not used at all, and the cache (`HeinzBOTtle.Statics.HBData.APICache`) is used instead.

The cache is a simple `Dictionary` that maps part of the query URL (consisting of the endpoint and the parameter with its argument, if any) to a pair consisting of an API query timestamp and the actual API response in a custom JSON wrapper (`HeinzBOTtle.Json`). Separate caches for each API endpoint or parameter are unnecessary due to the keys containing the source endpoints and parameters with their arguments. The API query timestamp is not updated to the current timestamp when the cached JSON is retrieved from an entry; this is only done when fresh JSON is fetched from the API. This means that any API data used by the program will only ever be at most 10 minutes out of date while also preventing the API from giving empty responses due to consecutive identical queries.

However, this model on its own would eventually result in a very large cache of outdated JSON, taking up a lot of space in memory. To deal with this, every time the Discord client disconnects (handled by `HeinzBOTtle.EventHandlers.DDisconnectedEvent`) from Discord (which happens regularly due to reconnect requests), all entries in the cache that are older than 10 minutes are erased.

### Interaction with the Database

HeinzBOTtle uses its own relational database as a reference for information that is not provided by the Hypixel API. It is able to update the schema on its own if an update to the program requires a different structure, which is done in `HeinzBOTtle.Database.DBMethods.UpdateDatabaseSchemaAsync` as part of the setup logic.

I would consider the most important relation to be `Users`, which keeps track of information about those who have opted into this system, which can be anyone in the Discord guild, but the feature is intended for those who have been in the Hypixel guild at some point. Specifically, it links Minecraft accounts to Discord accounts, stores Honorary Quest status, stores highest Treehard rank, and stores highest in-game guild rank. This relation has a wrapper struct, `HeinzBOTtle.Database.DBUser`, which is used to interface with the `Users` relation without client code having to construct SQL queries. It exposes methods for reading and writing database information relating the target user. This struct consists only of the primary key for the target user represented in the database, and that ID is used in queries it makes.

As of now, the program doesn't cache anything from the database because the program and database are intended to run together on the same machine. There isn't anything preventing the two from being on different machines; I just don't see a worthwhile reason to implement a cache for that particular case.

## Development

Currently, HeinzBOTtle uses a "[cathedral](https://en.wikipedia.org/wiki/The_Cathedral_and_the_Bazaar#Central_thesis)-like" development model. Between deployments, commits in other branches are squashed and merged into `main` with the original development branches remaining local. As such, most visible commits should be seen as updates or hotfixes, not necessarily as small chunks of related work. I do not create pull requests for my changes, though they might be created in the future for any contributions made by others.

Furthermore, HeinzBOTtle is not code reviewed and does not have automated tests as of now. So far, the project has been developed by only one person (TimothyJH) since 2023. The project is currently not accepting external code contributions, however, bug reports and suggestions are welcome, including suggestions on how to clean up code that might be seen as poorly-written.

Thanks for reading! :)
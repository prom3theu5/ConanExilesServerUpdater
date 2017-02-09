# Conan Exiles Dedicated Server Updater / Service

What can this do?:

 * Clean shutdown your current server (Using AHK until we get RCON)
 * Update Check Interval (in Minutes)
 * Announce new update to a Discord and Twitch channel (x) Minutes before an update will take place
 * Update the Server
 * Auto Restart the Server after an Update
 * Auto Restart Server on Crash
 * Auto Restart Server After X Hours


*The updater needs write access to your steamcmd folder, so setup windows permissions for this. This is because appcache has to be removed prior to an update check to ensure the correct LIVE build version is returned from steam.*

Configuration is handled in the config.json file:

```json
{
  "Twitch": {
    "OAuth": "#twitchoauthhere#",
    "Username": "#botname#",
    "Channel": "#twitchchannel#"
  },
  "Discord": {
    "DiscordToken": "#discord-bot-token#",
    "ChannelId": 276147691820417024
  },
  "Conan": {
    "FolderPath": "c:\\conanserver\\",
    "Executable": "ConanSandboxServer.exe",
    "StartupParameters": "ConanSandbox?Multihome=#ip#?GameServerPort=27015?GameServerQueryPort=27016?MaxPlayers=70?listen?AdminPassword=#adminpassword#?ServerPassword=#serverpassword#"
  },
  "Update": {
    "ShouldInstallSteamCmdIfMissing": true,
    "ShouldInstallConanServerIfMissing": true,
    "SteamCmdPath": "c:\\steamcmd\\",
    "AnnounceDiscord": false,
    "AnnounceTwitch": false,
    "AnnounceMinutesBefore": 5,
    "UpdateCheckInterval": 5,
    "InstalledBuild": 1612541
  },
  "General": {
    "ShouldRestartConanOnNotRunning": true,
    "RestartServerAfterHours": 0
  }
}
```

### To Install / Uninstall as a Service
Open a cmd window in the directory. 
Then run:
```
ConanExilesUpdater.exe install
```
or
```
ConanExilesUpdater.exe uninstall
```

The Executable can control the service too. You can start and stop it with
```
ConanExilesUpdater.exe start
```
or
```
ConanExilesUpdater.exe stop
```

### Donations
This work is all free, but if you wish to buy me a beer as a thank you you can donate: [Here](https://streamtip.com/t/prom3theu5) =)


### Stuff used to make this:

 * [Serilog](https://github.com/serilog/serilog) Simple .NET logging with fully-structured events.
 * [Serilog.Sinks.Literate](https://github.com/serilog/serilog-sinks-literate) Readable console window for Serilog.
 * [Serilog.Sinks.RollingFile](https://github.com/serilog/serilog-sinks-rollingfile) Rolling log file output for Serilog.
 * [TopShelf](https://github.com/Topshelf/Topshelf) Allows the application to be installed as a windows service.
 * [Discord.Net](https://github.com/RogueException/Discord.Net) Discord Client used for discord announcements.
 * [TwitchLib](https://github.com/swiftyspiffy/TwitchLib) Twitch Client used for twitch announcements.
 * [AutoHotKey.Interop](https://github.com/amazing-andrew/AutoHotkey.Interop) Used to cleanly shutdown the server with Control+C until we get RCON support.
 * [Costura](https://github.com/Fody/Costura) Used to Embed all these libraries as resources in the final executable.

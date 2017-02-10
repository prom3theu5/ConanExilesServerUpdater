# Conan Exiles Dedicated Server Updater / Service

What can this do?:

 * Clean shutdown your current server (Using AHK until we get RCON)
 * Update Check Interval (in Minutes)
 * Announce new update to a Discord and Twitch channel (x) Minutes before an update will take place
 * Update the Server
 * Auto Restart the Server after an Update
 * Auto Restart Server on Crash
 * Auto Restart Server After X Hours
 * Raiding Protection (Enable Hours that Raiding is allowed)
 * Avatar Protection (Enable Hours that Summoning Avatars is allowed)
 * Auto Install SteamCMD if not Found
 * Auto Install Conan Server if not found


*The updater needs write access to your steamcmd folder, so setup windows permissions for this. This is because appcache has to be removed prior to an update check to ensure the correct LIVE build version is returned from steam.*

Configuration is handled in the config.json file:

```json
{
  "Twitch": {
    "OAuth": "",
    "Username": "",
    "Channel": ""
  },
  "Discord": {
    "DiscordToken": "",
    "ChannelId": 0
  },
  "Conan": {
    "FolderPath": "c:\\conanserver\\",
    "Executable": "ConanSandboxServer.exe",
    "StartupParameters": "ConanSandBox?Multihome=%IP%?GameServerPort=%GamePort%?GameServerQueryPort=%QueryPort%?MaxPlayers=%MaxPlayers%?listen?AdminPassword=%AdminPass%",
    "RaidingProtectionHoursEnabled": true,
    "RaidingStartHour": 18,
    "RaidingLengthInHours": 5,
	"AvatarsEnabledCertainHours": false,
    "AvatarActivationHour": 0,
    "AvatarsActiveLengthInHours": 0
  },
  "Update": {
    "ShouldInstallSteamCmdIfMissing": false,
    "ShouldInstallConanServerIfMissing": false,
    "SteamCmdPath": "c:\\steamcmd\\",
    "AnnounceDiscord": false,
    "AnnounceTwitch": false,
    "AnnounceMinutesBefore": 5,
    "UpdateCheckInterval": 5,
    "InstalledBuild": 0,
	"UpdateOnLaunch": false
  },
  "General": {
    "ShouldRestartConanOnNotRunning": true,
    "RestartServerAfterHours": 0
  }
}
```

Messages are handled in the file messages.json

```json
{
  "Discord": {
    "DiscordUpdateMessage": "@Everyone New Conan Server Version Detected, Build: @version. The server will restart in @announcebefore.",
    "DiscordServerUptimeMessage": "The Conan server has been running for @uptime. Restarts are scheduled every @restartinterval. The server will restart in @countdownminutes.",
    "DiscordServerRestartingMessage": "@Everyone The Conan server will restart in @countdownminutes. It will show online in a few minutes.",
    "DiscordServerNotRunning": "@Everyone The Conan server was not detected as running. It will start now, and be online within 2-3 Minutes"
  },
  "Twitch": {
    "TwitchUpdateMessage": "New Conan Server Version Detected, Build: @version. The server will restart in @announcebefore.",
    "TwitchServerUptimeMessage": "The Conan server has been running for @uptime. Restarts are scheduled every @restartinterval. The server will restart in @countdownminutes.",
    "TwitchServerRestartingMessage": "The Conan Server is now restarting. It will show online in a few minutes",
    "TwitchServerNotRunning": "The Conan server was not detected as running. It will start now, and be online within 2-3 Minutes"
  },
  "AnnounceIntervalInMinutes": 0
}
```

Here you have a couple of parameters that will be replaced then the messages send.
You don't have to edit these messages, but if you'd like to you can, and must understand what these parameters are going to be replaced with.

```
@announcebefore -> This will be replaced with config.json value for announceperiod.
@restartinterval -> The config.json Server Restart Interval
@countdownminutes -> Countdown calculated from time now to expected shutdown.
@uptime -> Calculated from when the server was launched to the current time.
@version -> The new steam version detected.
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
 * [Octokit.net](https://github.com/octokit/octokit.net) Github Api wrapper to check you have the latest release :)
 * [Costura](https://github.com/Fody/Costura) Used to Embed all these libraries as resources in the final executable.
  
 
 ### Testers / Debuggers
 
 Aoxmodeus

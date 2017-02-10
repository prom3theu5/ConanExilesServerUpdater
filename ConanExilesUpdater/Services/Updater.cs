using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ConanExilesUpdater.Models;
using ConanExilesUpdater.Models.Messages;
using Newtonsoft.Json;
using Octokit;
using Serilog;

namespace ConanExilesUpdater.Services
{
    public class Updater
    {
        #region Private Properties

        private ManualResetEvent _quitEvent;
        private Settings _settings;
        private Messages _messages;
        private TwitchService _twitchClient;
        private DiscordService _discordClient;
        private GeneralServices _general;
        private bool _runUpdates = true;
        private const double _version = 1.92;
        #endregion

        #region Constructor

        public Updater(string StartupPath)
        {
            #region Load Settings & Messages

            if (File.Exists(Path.Combine(StartupPath, "config.json")))
            {
                _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(StartupPath, "config.json")));
                Log.Information("Loaded settings from file: {settings}", "config.json");
            }
            else
            {
                Utils.SaveSettings(StartupPath, new Settings());
                Log.Information("No settings existed. Created new settings file: {settings}", "config.json");
            }

            if (File.Exists(Path.Combine(StartupPath, "messages.json")))
            {
                _messages = JsonConvert.DeserializeObject<Messages>(File.ReadAllText(Path.Combine(StartupPath, "messages.json")));
                Log.Information("Loaded Messages from file: {settings}", "messages.json");
            }
            else
            {
                Utils.SaveMessages(StartupPath, new Messages());
                Log.Information("No messages existed. Created new messages file: {settings}", "messages.json");
            }

            #endregion
        }

        #endregion

        #region Startup Method (Service Startup)

        public async Task<bool> StartUpdater()
        {
            _quitEvent = new ManualResetEvent(false);
            await Task.Run(() => {
                Log.Information("ConanExilesUpdater Started Running {DateAndTime}", DateTime.UtcNow);

                CheckForUpdate();

                if (_settings.Update.ShouldInstallSteamCmdIfMissing)
                {
                    InstallSteamCmd();
                }

                if (_settings.Update.ShouldInstallConanServerIfMissing)
                {
                    InstallConanServer();
                }

                if (_settings.Update.UpdateOnLaunch)
                {
                    var process = Process.GetProcesses().Where(c => c.ProcessName.Contains("ConanSandboxServer")).FirstOrDefault();
                    if (process == null)
                    {
                        var doUpdate = DetectUpdate();
                        if (doUpdate)
                        {
                            DoServerUpdateInstall();
                            StartConan();
                        }
                    }
                }

                if (_settings.Update.AnnounceTwitch)
                    _twitchClient = new TwitchService(_settings);
                if (_settings.Update.AnnounceDiscord)
                    _discordClient = new DiscordService(_settings);
                _general = new GeneralServices(_settings, _discordClient, _twitchClient, _messages);
                _general.StartServices();
                RunUpdateChecks();
                _quitEvent.WaitOne();
            });
            return true;
        }

        private async void CheckForUpdate()
        {
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("ConanExilesServerUpdater"));
                var releases = await github.Repository.Release.GetAll("prom3theu5", "ConanExilesServerUpdater");
                var latest = releases[0];
                var version = Convert.ToDouble(latest.TagName);
                if (_version < version)
                {
                    Log.Warning("Version {newversion} detected on github. Please download from: {releaseurl}", version, latest.HtmlUrl);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error checking github for updates: {Exception}", e.Message);
            }

        }

        private void InstallConanServer()
        {
            if (!Directory.Exists(_settings.Conan.FolderPath))
            {
                Directory.CreateDirectory(_settings.Conan.FolderPath);
                Log.Information("Conan server is missing. Installing it now");
                DoServerUpdateInstall();
            }
        }

        private void InstallSteamCmd()
        {
            try
            {
                if (!Directory.Exists(_settings.Update.SteamCmdPath))
                {
                    Log.Information("SteamCMD Missing. Downloading Now");
                    Directory.CreateDirectory(_settings.Update.SteamCmdPath);
                    using (var wc = new WebClient())
                    {
                        wc.DownloadFile(new Uri("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"), Path.Combine(_settings.Update.SteamCmdPath, "steamcmd.zip"));
                        Log.Information("Extracting SteamCMD Zip");
                        ZipFile.ExtractToDirectory(Path.Combine(_settings.Update.SteamCmdPath, "steamcmd.zip"), _settings.Update.SteamCmdPath);
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = $"{_settings.Update.SteamCmdPath}steamcmd.exe",
                            Arguments = $"+quit",
                            RedirectStandardOutput = false,
                            UseShellExecute = false
                        };
                        var process = Process.Start(processStartInfo);
                        process.WaitForExit();
                        Log.Information("SteamCMD installed successfully");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error downloading SteamCMD: {Exception}", e.Message);
            }

        }

        #endregion

        #region Stop Method (Service Shutdown)

        public bool StopUpdater()
        {
            _runUpdates = false;

            if (_twitchClient != null)
            _twitchClient.Disconnect();

            if (_discordClient != null)
                _discordClient.Disconnect();

            if (_quitEvent != null)
                _quitEvent.Set();

            return true;
        }

        #endregion

        #region Updater Methods

        private async void RunUpdateChecks()
        {
            await Task.Run(async () => {
                while (_runUpdates)
                {
                    await Task.Delay(_settings.Update.UpdateCheckInterval * 1000*60);
                    var doUpdate = DetectUpdate();
                    if (doUpdate)
                    {
                        _general.StopServices();
                        var ready = await DoUpdate();
                        if (ready)
                            StartConan();
                    }
                }
            });
        }

        private async Task<bool> DoUpdate()
        {
            if (_settings.Update.AnnounceMinutesBefore != 0)
            {
                await Task.Delay(_settings.Update.AnnounceMinutesBefore * 1000 * 60);
            }

            var process = Process.GetProcesses().Where(c => c.ProcessName.Contains("ConanSandboxServer")).FirstOrDefault();
            if (process != null)
            {
                // Until we have RCON - Use AutoHotKey.Interop to send ^C to the server for a clean shutdown.
                Utils.TerminateServer();
                await Task.Delay(30 * 1000);
                process = Process.GetProcesses().Where(c => c.ProcessName.Contains("ConanSandboxServer")).FirstOrDefault();
                if (process != null)
                    process.Kill();
                // Wait 30 seconds for a clean shutdown
                await Task.Delay(30 * 1000);
            }

            DoServerUpdateInstall();
            
            return true;
        }

        private void DoServerUpdateInstall()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = $"{_settings.Update.SteamCmdPath}steamcmd.exe",
                Arguments = $"+@ShutdownOnFailedCommand +@NoPromptForPassword 1 +nSubscribedAutoDownloadMaxSimultaneous 32 +@cMaxContentServersToRequest 16 +@cMaxInitialDownloadSources 1 +@fMinDataRateToAttemptTwoConnectionsMbps 0.01 +@fDownloadRateImprovementToAddAnotherConnection 0.01 +login anonymous +force_install_dir {_settings.Conan.FolderPath} +app_update 443030 +quit",
                RedirectStandardOutput = false,
                UseShellExecute = false
            };
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
        }

        private void StartConan()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = $"{_settings.Conan.FolderPath}{_settings.Conan.Executable}",
                Arguments = $"{_settings.Conan.StartupParameters} -log",
                RedirectStandardOutput = false,
                UseShellExecute = false
            };
            Process.Start(processStartInfo);
            _general.StartServices();
        }

        private bool DetectUpdate()
        {
            try
            {
                if (_settings.Update.InstalledBuild == 0)
                {
                    if (string.IsNullOrWhiteSpace(_settings.Conan.FolderPath))
                    {
                        Log.Error("You must set your Installation Folder path in the config.json file");
                        return false;
                    }
                    Log.Information("Current Conan Dedicated Server Build Version not stored in Settings, Trying to get it now from your Application Manifest file");

                    var installedBuild = File.ReadAllLines(Path.Combine(_settings.Conan.FolderPath, "steamapps", "appmanifest_443030.acf"))
                        ?.FirstOrDefault(c => c.Contains("buildid"))
                        ?.Split(new char[] { '\t', '\t' })
                        ?.LastOrDefault()
                        ?.Trim()
                        ?.Replace("\"", "");
                    if (installedBuild == null)
                        Log.Error("Couldn't get installed Conan Dedicated Server Build Version from your appmanifest_443030.acf file. Please set this manually in config.json");
                    else
                    {
                        _settings.Update.InstalledBuild = Convert.ToInt32(installedBuild);
                        Utils.SaveSettings(Program.StartupPath, _settings);
                        Log.Information("Conan Dedicated Server Build Version has been detected and set as {buildversion}", installedBuild);
                        return false;
                    }
                }

                // Clear SteamCMD App Cache so correct Live build is Pulled!
                // App needs to be admin, or have write permissions on the steamcmd directory.
                var cache = Path.Combine(_settings.Update.SteamCmdPath, "appcache");
                if (Directory.Exists(cache))
                {
                    Directory.Delete(cache, true);
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = $"{_settings.Update.SteamCmdPath}steamcmd.exe",
                    Arguments = "+login anonymous +app_info_update 1 +app_info_print 443030 +app_info_print 443030 +quit",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var process = Process.Start(processStartInfo);
                var output = process.StandardOutput.ReadToEnd().Split(new char[] { '\r', '\n' });
                process.WaitForExit();
                var steamVersionString = output.FirstOrDefault(c => c.Contains("buildid"));
                if (steamVersionString == null)
                {
                    Log.Error("Steam Version was Not Detected");
                    return false;
                }
                var steamVersion = Convert.ToInt32(steamVersionString.Split(new char[] { '\t', '\t' })
                        ?.LastOrDefault()
                        ?.Trim()
                        ?.Replace("\"", ""));
                if (steamVersion <= _settings.Update.InstalledBuild)
                {
                    Log.Information("Installed Version is the same or greater than the steam version. No Update Needed!");
                    return false;
                }

                Log.Information("Detected SteamVersion as: {steamversion}. Your version is: {localVersion} An Update is required.", steamVersion, _settings.Update.InstalledBuild.ToString());

                if (_settings.Update.AnnounceTwitch)
                {
                    if (_twitchClient != null)
                        _twitchClient.SendMessage(_messages.Twitch.TwitchUpdateMessage.Replace("@version",$"{steamVersion}").Replace("@announcebefore", $"{_settings.Update.AnnounceMinutesBefore}{(_settings.Update.AnnounceMinutesBefore == 1 ? "Minute" : "Minutes")}"));
                }
                if (_settings.Update.AnnounceDiscord)
                {
                    if (_discordClient != null)
                        _discordClient.SendMessage(_messages.Discord.DiscordUpdateMessage.Replace("@version", $"{steamVersion}").Replace("@announcebefore", $"{_settings.Update.AnnounceMinutesBefore}{(_settings.Update.AnnounceMinutesBefore == 1 ? "Minute" : "Minutes")}"));
                }
                _settings.Update.InstalledBuild = Convert.ToInt32(steamVersion);
                Utils.SaveSettings(Program.StartupPath, _settings);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Exception occured in Detecting Update: {exception}", e.Message);
                return false;
            }
        }

        #endregion
    }
}

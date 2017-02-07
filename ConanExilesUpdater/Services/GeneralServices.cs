using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConanExilesUpdater.Models;
using Serilog;

namespace ConanExilesUpdater.Services
{
    public class GeneralServices
    {
        private readonly Settings _settings;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _token;
        private readonly DiscordService _discordClient;
        private readonly TwitchService _twitchService;

        public GeneralServices(Settings settings, DiscordService discord, TwitchService twitch)
        {
            _settings = settings;
            _discordClient = discord;
            _twitchService = twitch;
        }

        public async void StartServices()
        {
            if (_settings.General.ShouldRestartConanOnNotRunning == true)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _token = _cancellationTokenSource.Token;

                await Task.Run(async () =>
                {
                    while (true)
                    {
                        Log.Information("Starting To Monitor Server is Running");
                        await Task.Delay(30 * 1000);
                        var process = Process.GetProcesses().Where(c => c.ProcessName.Contains("ConanSandbox")).FirstOrDefault();
                        if (process != null)
                        {
                            if (_settings.General.RestartServerAfterHours == 0) return;
                            if (process.StartTime.AddHours(_settings.General.RestartServerAfterHours) >= DateTime.Now)
                            {
                                if (_settings.Update.AnnounceTwitch || _settings.Update.AnnounceDiscord)
                                {
                                    var runningTime = DateTime.Now - process.StartTime;
                                    var announceMessage = $"Conan Server Automatic Restarts are set to run every {_settings.General.RestartServerAfterHours} Hours. The server has been up for {runningTime.TotalHours} H {runningTime.Minutes} M. The Server will restart in {_settings.Update.AnnounceMinutesBefore} minutes.";
                                    if (_discordClient != null)
                                        _discordClient.SendMessage(announceMessage);
                                    if (_twitchService != null)
                                        _twitchService.SendMessage(announceMessage);
                                }
                                if (_settings.Update.AnnounceMinutesBefore > 0)
                                {
                                    await Task.Delay(_settings.Update.AnnounceMinutesBefore * 60 * 1000);
                                }
                                Utils.TerminateServer();
                                Log.Information("Server exceeded maximum specified running time, and a restart request was successfully made.");
                            }
                        }
                        else
                        {
                            Log.Information("Conan Server Not Detected - Launching Now");
                            var processStartInfo = new ProcessStartInfo
                            {
                                FileName = $"{_settings.Conan.FolderPath}ConanSandboxServer.exe",
                                Arguments = $"{_settings.Conan.StartupParameters} -log",
                                RedirectStandardOutput = false,
                                UseShellExecute = false
                            };
                            Process.Start(processStartInfo);
                        }
                    }
                }, _token);
            }
        }
        
        public void StopServices()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                Log.Information("Monitor Service has been stopped successfully");
            }
        }
    }
}

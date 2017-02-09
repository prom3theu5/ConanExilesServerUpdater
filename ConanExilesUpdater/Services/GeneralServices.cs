using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConanExilesUpdater.Models;
using Serilog;
using System;
using System.Linq;

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
                Log.Information("Starting To Monitor Server is Running");
                _cancellationTokenSource = new CancellationTokenSource();
                _token = _cancellationTokenSource.Token;

                await Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(30 * 1000);
                            var process = Process.GetProcesses().Where(c => c.ProcessName.Contains("ConanSandboxServer")).FirstOrDefault();
                            if (process != null)
                            {
                                    var startTime = process.StartTime;
                                    if (_settings.General.RestartServerAfterHours == 0) continue;
                                    if (startTime.AddHours(_settings.General.RestartServerAfterHours) >= DateTime.Now)
                                    {
                                        if (_settings.Update.AnnounceTwitch || _settings.Update.AnnounceDiscord)
                                        {
                                            var runningTime = DateTime.Now.Subtract(startTime);
                                            var announceMessage = $"Conan Server Automatic Restarts are set to run every {_settings.General.RestartServerAfterHours} Hours. The server has been up for {Math.Round(runningTime.TotalHours, 2)} H {runningTime.Minutes} M. The Server will restart in {_settings.Update.AnnounceMinutesBefore} {(_settings.Update.AnnounceMinutesBefore == 1 ? "Minute" : "Minutes")}.";
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
                                    FileName = $"{_settings.Conan.FolderPath}{_settings.Conan.Executable}",
                                    Arguments = $"{_settings.Conan.StartupParameters} -log",
                                    RedirectStandardOutput = false,
                                    UseShellExecute = false
                                };
                                Process.Start(processStartInfo);

                                if (_settings.Update.AnnounceTwitch || _settings.Update.AnnounceDiscord)
                                {
                                    var announceMessage = $"Conan Server was not detected as running. Restarting now. The server should show as being online in 2-3 Minutes.";
                                    if (_discordClient != null)
                                        _discordClient.SendMessage(announceMessage);
                                    if (_twitchService != null)
                                        _twitchService.SendMessage(announceMessage);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error("Error in Application Monitor: {exception}", e.Message);
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

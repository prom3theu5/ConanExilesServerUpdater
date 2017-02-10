using System;
using ConanExilesUpdater.Models;
using ConanExilesUpdater.Models.Messages;
using ConanExilesUpdater.Services;
using Newtonsoft.Json;
using Serilog;
using Topshelf;

namespace ConanExilesUpdater
{
    class Program
    {
        #region Properties

        private static Updater _updater;
        private static Settings _settings;
        private static Messages _messages;
        public static string StartupPath;

        #endregion

        static void Main(string[] args)
        {
            StartupPath = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

            #region Setup Logging

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile("log-{Date}.txt")
                .CreateLogger();

            #endregion

            #region Load Settings & Messages

            if (System.IO.File.Exists("config.json"))
            {
                _settings = JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText("config.json"));
                Log.Information("Loaded settings from file: {settings}", "config.json");
            }
            else
            {
                Utils.SaveSettings(StartupPath, new Settings());
                Log.Information("No settings existed. Created new settings file: {settings}", "config.json");
            }

            if (System.IO.File.Exists("messages.json"))
            {
                _messages = JsonConvert.DeserializeObject<Messages>(System.IO.File.ReadAllText("messages.json"));
                Log.Information("Loaded Messages from file: {settings}", "messages.json");
            }
            else
            {
                Utils.SaveMessages(StartupPath, new Messages());
                Log.Information("No messages existed. Created new messages file: {settings}", "messages.json");
            }

            #endregion

            #region Updater Instance

            _updater = new Updater(_settings, _messages);

            #endregion

            #region TopShelf Service

            HostFactory.Run(x => {
                x.Service<Updater>(updater => {
                    updater.ConstructUsing(() => _updater);
                    updater.WhenStarted(async b => await b.StartUpdater());
                    updater.WhenStopped(b => b.StopUpdater());
                });
                x.StartAutomatically();
                x.RunAsPrompt();
                x.UseSerilog();
                x.SetServiceName("ConanExilesUpdater");
                x.SetDescription("Conan Exiles Server Updater");
                x.SetInstanceName("ConanExilesUpdater");
                x.EnableServiceRecovery(r => {
                    r.RestartService(0);
                    r.RestartService(0);
                    r.RestartService(0);
                });
            });

            #endregion
        }
    }
}

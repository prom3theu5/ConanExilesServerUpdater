using System;
using ConanExilesUpdater.Services;
using Serilog;
using Topshelf;

namespace ConanExilesUpdater
{
    class Program
    {
        #region Properties

        private static Updater _updater;
        public static string StartupPath;

        #endregion

        static void Main(string[] args)
        {
            StartupPath = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;

            #region Setup Logging

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile("log-{Date}.txt")
                .CreateLogger();

            #endregion

            #region Updater Instance

            _updater = new Updater(StartupPath);

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

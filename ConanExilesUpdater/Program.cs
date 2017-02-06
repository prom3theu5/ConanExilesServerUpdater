using ConanExilesUpdater.Services;
using Serilog;
using Topshelf;

namespace ConanExilesUpdater
{
    class Program
    {
        public static Updater Updater { get; private set; }

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile("log-{Date}.txt", buffered: true)
                .CreateLogger();

            Updater = new Updater();

            HostFactory.Run(x => {
                x.Service<Updater>(updater => {
                    updater.ConstructUsing(() => Updater);
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
        }
    }
}

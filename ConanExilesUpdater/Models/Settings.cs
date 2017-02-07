namespace ConanExilesUpdater.Models
{
    public class Settings
    {
        public Twitch Twitch { get; set; }
        public Discord Discord { get; set; }
        public Conan Conan { get; set; }
        public Update Update { get; set; }
        public General General { get; set; }

        public Settings()
        {
            Twitch = new Twitch();
            Discord = new Discord();
            Conan = new Conan();
            Update = new Update();
            General = new General();
        }
    }

    public class Twitch
    {
        public string OAuth { get; set; }
        public string Username { get; set; }
        public string Channel { get; set; }
    }

    public class Discord
    {
        public string DiscordToken { get; set; }
        public ulong ChannelId { get; set; }
    }

    public class Conan
    {
        public string FolderPath { get; set; }
        public string StartupParameters { get; set; }
    }

    public class Update
    {
        public string SteamCmdPath { get; set; }
        public bool AnnounceDiscord { get; set; }
        public bool AnnounceTwitch { get; set; }
        public int AnnounceMinutesBefore { get; set; }
        public int UpdateCheckInterval { get; set; }
        public int InstalledBuild { get; set; }

        public Update()
        {
            AnnounceDiscord = false;
            AnnounceTwitch = false;
            UpdateCheckInterval = 300;
            AnnounceMinutesBefore = 300;
        }
    }

    public class General
    {
        public bool ShouldRestartConanOnNotRunning { get; set; }
        public int RestartServerAfterHours { get; set; }

        public General()
        {
            ShouldRestartConanOnNotRunning = true;
            RestartServerAfterHours = 0;
        }

    }
}

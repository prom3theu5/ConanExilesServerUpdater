namespace ConanExilesUpdater.Models.Messages
{
    public class Messages
    {
        public Discord Discord { get; set; }
        public Twitch Twitch { get; set; }
        public int AnnounceIntervalInMinutes { get; set; }

        public Messages()
        {
            Discord = new Discord();
            Twitch = new Twitch();
            AnnounceIntervalInMinutes = 0;
        }
    }

    public class Discord
    {
        public string DiscordUpdateMessage { get; set; }
        public string DiscordServerUptimeMessage { get; set; }
        public string DiscordServerRestartingMessage { get; set; }
        public string DiscordServerNotRunning { get; set; }

        public Discord()
        {
            DiscordServerRestartingMessage = "@Everyone The Conan server will restart in @countdownminutes. It will show online in a few minutes.";
            DiscordUpdateMessage = "@Everyone New Conan Server Version Detected, Build: @version. The server will restart in @announcebefore.";
            DiscordServerUptimeMessage = "The Conan server has been running for @uptime. Restarts are scheduled every @restartinterval. The server will restart in @countdownminutes.";
            DiscordServerNotRunning = "@Everyone The Conan server was not detected as running. It will start now, and be online within 2-3 Minutes";
        }
    }

    public class Twitch
    {
        public string TwitchUpdateMessage { get; set; }
        public string TwitchServerUptimeMessage { get; set; }
        public string TwitchServerRestartingMessage { get; set; }
        public string TwitchServerNotRunning { get; set; }

        public Twitch()
        {
            TwitchServerRestartingMessage = "The Conan Server is now restarting. It will show online in a few minutes";
            TwitchUpdateMessage = "New Conan Server Version Detected, Build: @version. The server will restart in @announcebefore.";
            TwitchServerUptimeMessage = "The Conan server has been running for @uptime. Restarts are scheduled every @restartinterval. The server will restart in @countdownminutes.";
            TwitchServerNotRunning = "The Conan server was not detected as running. It will start now, and be online within 2-3 Minutes";
        }
    }
}

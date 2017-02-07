using ConanExilesUpdater.Models;
using Serilog;
using TwitchLib;
using TwitchLib.Models.Client;

namespace ConanExilesUpdater.Services
{
    public class TwitchService
    {
        #region Properties

        private TwitchClient _client;
        private readonly Settings _settings;

        #endregion

        #region Constructor

        public TwitchService(Settings settings)
        {
            _settings = settings;

            var credentials = new ConnectionCredentials(
                _settings.Twitch.Username,
                _settings.Twitch.OAuth);
            _client = new TwitchClient(
                credentials,
                _settings.Twitch.Channel);
            _client.OnConnected += Client_OnConnected;
            _client.OnConnectionError += Client_OnConnectionError;
            _client.OnDisconnected += Client_OnDisconnected;
            _client.OnJoinedChannel += Client_OnJoinedChannel;
            _client.Connect();
        }

        #endregion

        #region Event Handling

        private void Client_OnJoinedChannel(object sender, TwitchLib.Events.Client.OnJoinedChannelArgs e)
        {
            Log.Information("Joined Twitch Channel: {channel}", e.Channel);
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Events.Client.OnDisconnectedArgs e)
        {
            Log.Information("TwitchClient has Disconnected");
        }
        
        private void Client_OnConnectionError(object sender, TwitchLib.Events.Client.OnConnectionErrorArgs e)
        {
            Log.Error("An error occured connecting to Twitch. Please check your details in config.json");
        }

        private void Client_OnConnected(object sender, TwitchLib.Events.Client.OnConnectedArgs e)
        {
            Log.Information("TwitchClient has Connected to Twitch");
        }

        #endregion

        #region Public Methods

        public void Disconnect()
        {
            if (_client.IsConnected)
                _client.Disconnect();
        }

        public void SendMessage(string message)
        {
            _client.SendMessage($".me {message}");
        }
        
        #endregion
    }
}

using System;
using System.Threading.Tasks;
using ConanExilesUpdater.Models;
using Discord;
using Serilog;

namespace ConanExilesUpdater.Services
{
    public class DiscordService
    {
        #region Properties

        private readonly Settings _settings;
        private DiscordClient _client;

        #endregion

        #region Constructor

        public DiscordService(Settings _settings)
        {
            this._settings = _settings;
            _client = new DiscordClient(x => { x.UsePermissionsCache = false; });
            _client.Ready += Client_Ready;

            Task.Run(() =>
            {
                _client.ExecuteAndWait(async () =>
                {
                    await _client.Connect(_settings.Discord.DiscordToken, TokenType.Bot);
                });
            });
        }

        #endregion

        #region Event Handling

        private void Client_Ready(object sender, EventArgs e)
        {
            Log.Information("Discord Connected!");
        }

        #endregion

        #region Public Methods

        public async void Disconnect()
        {
            if (_client.State == ConnectionState.Connected)
                await _client.Disconnect();
        }

        public async void SendMessage(string message)
        {
            var channel = _client.GetChannel(_settings.Discord.ChannelId);
            await channel.SendMessage(message).ConfigureAwait(false);
        }

        #endregion
    }
}

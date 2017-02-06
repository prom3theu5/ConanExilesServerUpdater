using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace ConanExilesUpdater.Services
{
    public class Updater
    {
        ManualResetEvent _quitEvent;

        public async Task<bool> StartUpdater()
        {
            _quitEvent = new ManualResetEvent(false);
            await Task.Run(() => {
                
                Log.Information("ConanExilesUpdater Started Running {DateAndTime}", DateTime.UtcNow);
                _quitEvent.WaitOne();
            });
            return true;
        }

        public bool StopUpdater()
        {
            if (_quitEvent != null)
                _quitEvent.Set();
            return true;
        }
    }
}

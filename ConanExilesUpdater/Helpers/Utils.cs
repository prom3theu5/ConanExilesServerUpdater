using AutoHotkey.Interop;
using ConanExilesUpdater.Models;
using Newtonsoft.Json;

namespace ConanExilesUpdater
{
    public static class Utils
    {
        #region Save Settings

        public static void SaveSettings(string _startupPath, Settings _settings)
        {
            using (var sW = new System.IO.StreamWriter(System.IO.Path.Combine(_startupPath, "config.json"), false))
            {
                using (var jsonWriter = new JsonTextWriter(sW) { Formatting = Formatting.Indented })
                {
                    jsonWriter.WriteRaw(JsonConvert.SerializeObject(_settings, Formatting.Indented));
                }
            }
        }

        #endregion

        #region AutoHotKey Terminate Server

        public static void TerminateServer()
        {
            var ahk = AutoHotkeyEngine.Instance;
            var script = "ControlSend, , ^C, Conan Exiles - press Ctrl+C to shutdown";
            ahk.ExecRaw(script);
        }

        #endregion
    }
}

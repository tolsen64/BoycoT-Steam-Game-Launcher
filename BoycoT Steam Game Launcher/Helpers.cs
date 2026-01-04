using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BoycoT_Steam_Game_Launcher
{
    class Helpers
    {
        internal static string GetIconCacheFolder()
        {
            // Cache folder inside AppData
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string _iconCacheFolder = Path.Combine(appData, "BoycoTSteamLauncher", "icons");
            Directory.CreateDirectory(_iconCacheFolder);
            return _iconCacheFolder;
        }
    }
}

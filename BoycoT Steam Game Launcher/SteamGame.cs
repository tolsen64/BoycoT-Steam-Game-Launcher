namespace BoycoT_Steam_Game_Launcher
{
    public class SteamGame
    {
        public int AppId { get; set; }          // Steam App ID
        public string Name { get; set; }        // Game name
        public string InstallDir { get; set; }  // Folder where the game is installed
        public string LibraryPath { get; set; } // Steam library folder
        public string IconPath { get; set; }    // Local path or URL to the icon
    }
}

using System.Collections.ObjectModel;

namespace BoycoT_Steam_Game_Launcher
{
    public class MainWindowViewModel
    {
        public ObservableCollection<SteamGame> Games { get; set; } = new ObservableCollection<SteamGame>();
    }
}

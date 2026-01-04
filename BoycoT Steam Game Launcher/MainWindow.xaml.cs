using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace BoycoT_Steam_Game_Launcher
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; set; } = new MainWindowViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;

            // Start loading Steam games
            LoadGamesAsync();
        }

        private async void LoadGamesAsync()
        {
            try
            {
                var scanner = new SteamScanner();
                var installedGames = await scanner.GetInstalledGamesAsync();

                foreach (var game in installedGames)
                {
                    ViewModel.Games.Add(game);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Failed to load Steam games: " + ex.Message);
            }
        }

        private void GameIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not SteamGame game)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{game.AppId}",
                    UseShellExecute = true
                });
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show(
                    $"Steam could not launch \"{game.Name}\".\n\n" +
                    $"Make sure Steam is installed and running.\n\n{ex.Message}",
                    "Launch Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while launching \"{game.Name}\".\n\n{ex.Message}",
                    "Launch Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}

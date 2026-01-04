using Microsoft.Web.WebView2.Core;
using System.Windows;

namespace BoycoT_Steam_Game_Launcher
{
    public partial class WebViewWindow : Window
    {
        public string SelectedImageUrl { get; private set; } = string.Empty;

        public WebViewWindow(string startUrl)
        {
            InitializeComponent();
            InitializeWebView(startUrl);
        }

        private async void InitializeWebView(string url)
        {
            await WebView2.EnsureCoreWebView2Async(null);
            WebView2.CoreWebView2.Navigate(url);
        }

        private async void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string script = @"
                    (function() {
                        var img = document.querySelector('.game_header_image_full');
                        return img ? img.src : '';
                    })();";

                string result = await WebView2.ExecuteScriptAsync(script);

                SelectedImageUrl = System.Text.Json.JsonSerializer.Deserialize<string>(result);

                this.DialogResult = true; // closes the window
            }
            catch
            {
                MessageBox.Show("Failed to get image URL.");
            }
        }
    }
}

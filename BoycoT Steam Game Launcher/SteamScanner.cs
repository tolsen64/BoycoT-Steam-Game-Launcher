using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BoycoT_Steam_Game_Launcher
{
    public class SteamScanner
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _iconCacheFolder;

        private static readonly HashSet<string> IgnoredNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Steamworks Common Redistributables",
                "SteamVR",
                "Steamworks SDK"
            };

        private static readonly HashSet<int> IgnoredAppIds =
            new()
            {
                228980 // Steamworks Common Redistributables
            };

        public SteamScanner()
        {
            _iconCacheFolder = Helpers.GetIconCacheFolder();
            Directory.CreateDirectory(_iconCacheFolder);
        }

        // Entry point
        public async Task<List<SteamGame>> GetInstalledGamesAsync()
        {
            var games = new List<SteamGame>();

            string steamPath = GetSteamInstallPath()
                ?? throw new Exception("Steam not found on this PC.");

            string mainLibrary = Path.Combine(steamPath, "steamapps");
            games.AddRange(await ScanLibraryAsync(mainLibrary));

            string libraryFoldersFile = Path.Combine(mainLibrary, "libraryfolders.vdf");
            if (File.Exists(libraryFoldersFile))
            {
                foreach (var lib in ParseLibraryFolders(libraryFoldersFile))
                {
                    string steamapps = Path.Combine(lib, "steamapps");
                    games.AddRange(await ScanLibraryAsync(steamapps));
                }
            }

            // Alphabetical ordering (case-insensitive)
            return games
                .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string GetSteamInstallPath()
        {
            string[] paths =
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam"
            };

            return paths.FirstOrDefault(Directory.Exists);
        }

        private static List<string> ParseLibraryFolders(string filePath)
        {
            var libraries = new List<string>();
            var regex = new Regex("\"path\"\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);

            foreach (var line in File.ReadLines(filePath))
            {
                var match = regex.Match(line);
                if (!match.Success)
                    continue;

                string path = match.Groups[1].Value.Replace("\\\\", "\\");
                if (Directory.Exists(path))
                    libraries.Add(path);
            }

            return libraries;
        }

        private async Task<List<SteamGame>> ScanLibraryAsync(string steamappsPath)
        {
            var games = new List<SteamGame>();
            if (!Directory.Exists(steamappsPath))
                return games;

            foreach (var file in Directory.GetFiles(steamappsPath, "appmanifest_*.acf"))
            {
                var game = await ParseAppManifestAsync(file, steamappsPath);
                if (game != null)
                    games.Add(game);
            }

            return games;
        }

        private async Task<SteamGame> ParseAppManifestAsync(string acfFile, string steamappsPath)
        {
            try
            {
                string content = File.ReadAllText(acfFile);

                int appid = int.Parse(Regex.Match(content, "\"appid\"\\s*\"(\\d+)\"").Groups[1].Value);
                string name = Regex.Match(content, "\"name\"\\s*\"([^\"]+)\"").Groups[1].Value;
                string installDir = Regex.Match(content, "\"installdir\"\\s*\"([^\"]+)\"").Groups[1].Value;

                if (IgnoredAppIds.Contains(appid) || IgnoredNames.Contains(name))
                    return null;

                var stateMatch = Regex.Match(content, "\"StateFlags\"\\s*\"(\\d+)\"");
                if (stateMatch.Success &&
                    int.TryParse(stateMatch.Groups[1].Value, out int flags) &&
                    (flags & 4) == 0)
                {
                    return null;
                }

                string iconPath = await GetCachedIconPathAsync(appid) ?? string.Empty;

                return new SteamGame
                {
                    AppId = appid,
                    Name = name,
                    InstallDir = installDir,
                    LibraryPath = steamappsPath,
                    IconPath = iconPath
                };
            }
            catch
            {
                return null;
            }
        }

        // Always returns local file path or null
        private async Task<string> GetCachedIconPathAsync(int appid)
        {
            string localFile = Path.Combine(_iconCacheFolder, $"{appid}.jpg");

            if (File.Exists(localFile))
                return localFile;

            // Direct CDN attempt
            string directUrl =
                $"https://cdn.akamai.steamstatic.com/steam/apps/{appid}/header.jpg";

            if (await TryDownloadAsync(directUrl, localFile))
                return localFile;

            // Steam API fallback
            try
            {
                string apiUrl =
                    $"https://store.steampowered.com/api/appdetails?appids={appid}";

                using var doc = JsonDocument.Parse(await _httpClient.GetStringAsync(apiUrl));
                var root = doc.RootElement.GetProperty(appid.ToString());

                if (!root.GetProperty("success").GetBoolean())
                    return null;

                if (root.GetProperty("data")
                        .TryGetProperty("header_image", out var header))
                {
                    string headerUrl = header.GetString();
                    if (!string.IsNullOrEmpty(headerUrl) &&
                        await TryDownloadAsync(headerUrl, localFile))
                    {
                        return localFile;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static async Task<bool> TryDownloadAsync(string url, string destination)
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(destination, bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

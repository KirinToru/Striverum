using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Striverum
{
    public partial class FetchWindow : Window
    {
        public bool success;
        public Mod _mod;

        public FetchWindow(Mod mod)
        {
            InitializeComponent();
            _mod = mod;
            if (_mod != null)
            {
                Title = $"Fetch Metadata - {_mod.name}";
                ModNameBlock.Text = _mod.name;

                // Pre-fill existing URL if available
                string modJsonPath = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{_mod.name}{Global.s}mod.json";
                if (File.Exists(modJsonPath))
                {
                    try
                    {
                        var meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(modJsonPath));
                        if (meta?.homepage != null)
                        {
                            UrlBox.Text = meta.homepage.ToString();
                        }
                    }
                    catch { }
                }
            }

            Loaded += (s, e) =>
            {
                UrlBox.Focus();
                if (!string.IsNullOrEmpty(UrlBox.Text))
                {
                    UrlBox.SelectAll();
                }
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Uri CreateUri(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            if ((Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || Uri.TryCreate("http://" + url, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                string host = uri.DnsSafeHost;
                if (uri.Segments.Length != 3)
                    return null;
                switch (host)
                {
                    case "www.gamebanana.com":
                    case "gamebanana.com":
                        return uri;
                }
            }
            return null;
        }

        private async void Fetch()
        {
            StatusBlock.Visibility = Visibility.Collapsed;
            Uri url = CreateUri(UrlBox.Text);
            if (url == null)
            {
                StatusBlock.Text = "Invalid URL. Format should be: https://gamebanana.com/<Mod Type>/<Mod ID>";
                StatusBlock.Visibility = Visibility.Visible;
                return;
            }

            ConfirmButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            UrlBox.IsEnabled = false;
            StatusBlock.Text = "Fetching metadata from GameBanana...";
            StatusBlock.Foreground = System.Windows.Media.Brushes.LightGray;
            StatusBlock.Visibility = Visibility.Visible;

            try
            {
                var modType = char.ToUpper(url.Segments[1][0]) + url.Segments[1].Substring(1, url.Segments[1].Length - 3);
                var modId = url.Segments[2].TrimEnd('/');
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Striverum");
                var requestUrl = $"https://gamebanana.com/apiv6/{modType}/{modId}?_csvProperties=_sName,_aSubmitter,_sDescription,_aPreviewMedia,_sProfileUrl,_aSuperCategory,_aCategory,_tsDateUpdated";
                string responseString = await client.GetStringAsync(requestUrl);
                var record = JsonSerializer.Deserialize<GameBananaAPIV4>(responseString);
                if (record == null)
                {
                    throw new Exception("Unable to parse GameBanana response.");
                }

                var metadata = new Metadata
                {
                    name = !string.IsNullOrEmpty(record.Title) ? string.Concat(record.Title.Split(Path.GetInvalidFileNameChars())) : _mod.name,
                    submitter = record.Owner?.Name,
                    description = record.Description,
                    preview = record.Image,
                    homepage = record.Link ?? url,
                    avi = record.Owner?.Avatar,
                    upic = record.Owner?.Upic,
                    cat = record.CategoryName,
                    subcategory = record.Category?.Name ?? "",
                    caticon = record.Category?.Icon,
                    lastupdate = record.DateUpdated,
                    tags = new List<string>()
                };

                if (record.RootCategory != null && !string.IsNullOrEmpty(record.RootCategory.Name))
                    metadata.tags.Add(record.RootCategory.Name);
                if (record.Category != null && !string.IsNullOrEmpty(record.Category.Name) && !metadata.tags.Contains(record.Category.Name))
                    metadata.tags.Add(record.Category.Name);

                string modDir = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{_mod.name}";
                Directory.CreateDirectory(modDir);
                string metadataString = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText($@"{modDir}{Global.s}mod.json", metadataString);

                Global.logger.WriteLine($"Successfully fetched metadata for {_mod.name} from {url}", LoggerType.Info);
                success = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Error fetching metadata: {ex.Message}";
                StatusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 83, 80));
                StatusBlock.Visibility = Visibility.Visible;
                Global.logger.WriteLine($"Fetch metadata failed: {ex.Message}", LoggerType.Error);
                ConfirmButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                UrlBox.IsEnabled = true;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Fetch();
        }

        private void UrlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Fetch();
        }
    }
}

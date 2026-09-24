using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;

namespace Striverum.UI
{
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        public string _name;
        public bool _folder;
        public string directory = null;
        public string newName;
        public string loadout = null;
        public EditWindow(string name, bool folder)
        {
            InitializeComponent();
            _folder = folder;
            if (!String.IsNullOrEmpty(name))
            {
                _name = name;
                NameBox.Text = name;
                Title = $"Edit {name}";
                HeaderIcon.Icon = _folder ? FontAwesome5.EFontAwesomeIcon.Solid_Edit : FontAwesome5.EFontAwesomeIcon.Solid_SlidersH;
            }
            else
            {
                if (_folder)
                {
                    Title = "Create New Mod";
                    HeaderIcon.Icon = FontAwesome5.EFontAwesomeIcon.Solid_Plus;
                }
                else
                {
                    Title = "Create New Loadout";
                    HeaderIcon.Icon = FontAwesome5.EFontAwesomeIcon.Solid_SlidersH;
                }
            }

            if (!_folder || string.IsNullOrEmpty(_name))
            {
                ResetDefaultButton.Visibility = Visibility.Collapsed;
            }

            Loaded += (s, e) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (_folder)
                if (_name != null)
                    EditFolderName();
                else
                    CreateName();
            else
                CreateLoadoutName();

        }
        private void CreateName()
        {
            var newDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{NameBox.Text}";
            if (!Directory.Exists(newDirectory))
            {
                directory = newDirectory;
                Close();
            }
            else
                Global.logger.WriteLine($"{newDirectory} already exists", LoggerType.Error);
        }
        private void CreateLoadoutName()
        {
            if (String.IsNullOrWhiteSpace(NameBox.Text))
            {
                Global.logger.WriteLine($"Invalid loadout name", LoggerType.Error);
                return;
            }
            if (!Global.config.Configs[Global.config.CurrentGame].Loadouts.ContainsKey(NameBox.Text))
            {
                loadout = NameBox.Text;
                Close();
            }
            else
                Global.logger.WriteLine($"{NameBox.Text} already exists", LoggerType.Error);
        }
        private void EditFolderName()
        {
            if (!NameBox.Text.Equals(_name, StringComparison.InvariantCultureIgnoreCase))
            {
                var oldDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{_name}";
                var newDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{NameBox.Text}";
                if (!Directory.Exists(newDirectory))
                {
                    try
                    {
                        Directory.Move(oldDirectory, newDirectory);
                        // Rename in every single loadout
                        foreach (var key in Global.config.Configs[Global.config.CurrentGame].Loadouts.Keys)
                        {
                            var index = Global.config.Configs[Global.config.CurrentGame].Loadouts[key].ToList().FindIndex(x => x.name == _name);
                            if (index >= 0)
                            {
                                Global.config.Configs[Global.config.CurrentGame].Loadouts[key][index].name = NameBox.Text;
                            }
                        }
                        Global.ModList = Global.config.Configs[Global.config.CurrentGame].Loadouts[Global.config.Configs[Global.config.CurrentGame].CurrentLoadout];
                        Close();
                    }
                    catch (Exception ex)
                    {
                        Global.logger.WriteLine($"Couldn't rename {oldDirectory} to {newDirectory} ({ex.Message})", LoggerType.Error);
                    }
                }
                else
                    Global.logger.WriteLine($"{newDirectory} already exists", LoggerType.Error);
            }
            else
            {
                Close();
            }
        }

        private async void ResetDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            StatusBlock.Visibility = Visibility.Collapsed;
            if (!_folder || string.IsNullOrEmpty(_name)) return;

            string currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}";
            string modDir = $@"{currentModDirectory}{Global.s}{_name}";
            string modJsonPath = $@"{modDir}{Global.s}mod.json";

            Metadata meta = null;
            if (File.Exists(modJsonPath))
            {
                try
                {
                    meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(modJsonPath));
                }
                catch { }
            }

            if (meta != null && !string.IsNullOrWhiteSpace(meta.name))
            {
                string cleanName = string.Concat(meta.name.Split(Path.GetInvalidFileNameChars()));
                if (!string.IsNullOrWhiteSpace(cleanName))
                {
                    NameBox.Text = cleanName;
                    NameBox.SelectAll();
                    StatusBlock.Text = "Name reset to default.";
                    StatusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106));
                    StatusBlock.Visibility = Visibility.Visible;
                    return;
                }
            }

            // Fallback: If meta.homepage is a GameBanana URL, fetch original title
            if (meta?.homepage != null)
            {
                var uri = meta.homepage;
                if (uri.Segments.Length == 3 &&
                    (uri.DnsSafeHost.Equals("gamebanana.com", StringComparison.OrdinalIgnoreCase) ||
                     uri.DnsSafeHost.Equals("www.gamebanana.com", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        ResetDefaultButton.IsEnabled = false;
                        StatusBlock.Text = "Fetching original name from GameBanana...";
                        StatusBlock.Foreground = System.Windows.Media.Brushes.LightGray;
                        StatusBlock.Visibility = Visibility.Visible;

                        var modType = char.ToUpper(uri.Segments[1][0]) + uri.Segments[1].Substring(1, uri.Segments[1].Length - 3);
                        var modId = uri.Segments[2].TrimEnd('/');
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("User-Agent", "Striverum");
                        var requestUrl = $"https://gamebanana.com/apiv6/{modType}/{modId}?_csvProperties=_sName";
                        string responseString = await client.GetStringAsync(requestUrl);
                        var record = JsonSerializer.Deserialize<GameBananaAPIV4>(responseString);
                        if (record != null && !string.IsNullOrWhiteSpace(record.Title))
                        {
                            string cleanName = string.Concat(record.Title.Split(Path.GetInvalidFileNameChars()));
                            NameBox.Text = cleanName;
                            NameBox.SelectAll();
                            StatusBlock.Text = "Name reset to default from GameBanana.";
                            StatusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106));
                            StatusBlock.Visibility = Visibility.Visible;

                            meta.name = cleanName;
                            File.WriteAllText(modJsonPath, JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Global.logger.WriteLine($"Failed fetching mod title from GameBanana: {ex.Message}", LoggerType.Error);
                    }
                    finally
                    {
                        ResetDefaultButton.IsEnabled = true;
                    }
                }
            }

            StatusBlock.Text = "No default name found in metadata.";
            StatusBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 179, 0));
            StatusBlock.Visibility = Visibility.Visible;
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (_folder)
                    if (_name != null)
                        EditFolderName();
                    else
                        CreateName();
                else
                    CreateLoadoutName();
            }
        }
    }
}

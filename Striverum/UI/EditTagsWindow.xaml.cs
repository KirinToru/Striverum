using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Striverum.UI
{
    public partial class EditTagsWindow : Window
    {
        private readonly Mod _mod;

        public EditTagsWindow(Mod mod)
        {
            InitializeComponent();
            _mod = mod ?? throw new ArgumentNullException(nameof(mod));
            Title = $"Edit Tags - {_mod.name}";
            HeaderTitle.Text = "Edit Tags";
            ModNameBlock.Text = _mod.name;

            if (_mod.tags != null && _mod.tags.Count > 0)
            {
                TagsBox.Text = string.Join(", ", _mod.tags);
            }

            Loaded += (s, e) =>
            {
                TagsBox.Focus();
                TagsBox.CaretIndex = TagsBox.Text.Length;
            };
        }

        private void ResetDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            var defTags = GetDefaultTags();
            TagsBox.Text = string.Join(", ", defTags);
            TagsBox.CaretIndex = TagsBox.Text.Length;
        }

        private List<string> GetDefaultTags()
        {
            var defaultTags = new List<string>();
            string currentModDirectory = Global.GetCurrentModDirectory();
            string modJsonPath = $@"{currentModDirectory}{Global.s}{_mod.name}{Global.s}mod.json";

            Metadata meta = null;
            if (File.Exists(modJsonPath))
            {
                try
                {
                    meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(modJsonPath));
                }
                catch { }
            }

            bool isGameBanana = false;
            if (meta?.homepage != null && meta.homepage.ToString().Contains("gamebanana.com", StringComparison.OrdinalIgnoreCase))
            {
                isGameBanana = true;
            }

            if (isGameBanana)
            {
                if (!string.IsNullOrEmpty(meta.cat))
                    defaultTags.Add(meta.cat);
                if (!string.IsNullOrEmpty(meta.subcategory) && !defaultTags.Contains(meta.subcategory, StringComparer.OrdinalIgnoreCase))
                    defaultTags.Add(meta.subcategory);
            }

            if (defaultTags.Count == 0)
            {
                if (!string.IsNullOrEmpty(_mod.cat) && !_mod.cat.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                    defaultTags.Add(_mod.cat);
                if (!string.IsNullOrEmpty(_mod.subcategory) && !defaultTags.Contains(_mod.subcategory, StringComparer.OrdinalIgnoreCase))
                    defaultTags.Add(_mod.subcategory);
            }

            if (defaultTags.Count == 0)
            {
                defaultTags.Add("Unknown");
            }

            return defaultTags;
        }

        private void QuickTag_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var tagToAdd = btn?.Tag?.ToString();
            if (string.IsNullOrEmpty(tagToAdd)) return;

            var currentTags = ParseCurrentTags();
            int existingIndex = currentTags.FindIndex(t => t.Equals(tagToAdd, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                currentTags.RemoveAt(existingIndex);
            }
            else
            {
                currentTags.Add(tagToAdd);
            }

            TagsBox.Text = string.Join(", ", currentTags);
            TagsBox.CaretIndex = TagsBox.Text.Length;
        }

        private List<string> ParseCurrentTags()
        {
            return TagsBox.Text
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void TagsBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Save();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            var newTags = ParseCurrentTags();
            _mod.tags = newTags;

            // Persist to mod.json
            try
            {
                string currentModDirectory = Global.GetCurrentModDirectory();
                string modJsonPath = $@"{currentModDirectory}{Global.s}{_mod.name}{Global.s}mod.json";
                Metadata meta = null;
                if (File.Exists(modJsonPath))
                {
                    try
                    {
                        meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(modJsonPath));
                    }
                    catch { }
                }

                if (meta == null)
                {
                    meta = new Metadata();
                }

                meta.tags = newTags;
                File.WriteAllText(modJsonPath, JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
                Global.logger.WriteLine($"Updated tags for {_mod.name}: [{string.Join(", ", newTags)}]", LoggerType.Info);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Could not save mod.json for {_mod.name}: {ex.Message}", LoggerType.Error);
            }

            DialogResult = true;
            Close();
        }
    }
}

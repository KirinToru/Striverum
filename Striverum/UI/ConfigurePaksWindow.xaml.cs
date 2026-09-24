using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Striverum.UI
{
    public partial class ConfigurePaksWindow : Window
    {
        public Mod _mod;

        public ConfigurePaksWindow(Mod mod)
        {
            InitializeComponent();
            if (mod != null)
            {
                _mod = mod;
                Title = $"Configure Paks - {_mod.name}";
                ModNameBlock.Text = _mod.name;
                PakList.ItemsSource = new ObservableCollection<KeyValuePair<string, bool>>(_mod.paks);
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            if (_mod?.paks != null)
            {
                int enabledCount = _mod.paks.Values.Count(v => v);
                PakCountBlock.Text = $"{enabledCount} of {_mod.paks.Count} packages enabled";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.DataContext is KeyValuePair<string, bool> item && _mod?.paks != null)
            {
                _mod.paks[item.Key] = button.IsChecked == true;
                UpdateStatus();
            }
        }
    }
}

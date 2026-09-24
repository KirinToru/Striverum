using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Striverum.UI
{
    /// <summary>
    /// Interaction logic for UpdateFileBox.xaml
    /// </summary>
    public partial class UpdateFileBox : Window
    {
        public string chosenFileUrl;
        public string chosenFileName;
        public string chosenFileDescription;
        public bool selectedDownloadAll;

        public UpdateFileBox(List<GameBananaItemFile> files, string packageName)
        {
            InitializeComponent();
            selectedDownloadAll = false;
            FileList.ItemsSource = files;
            TitleBox.Text = packageName;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button?.DataContext as GameBananaItemFile;
            if (item != null)
            {
                chosenFileUrl = item.DownloadUrl;
                chosenFileName = item.FileName;
                chosenFileDescription = item.Description;
            }
            Close();
        }

        private void DownloadAll_Click(object sender, RoutedEventArgs e)
        {
            selectedDownloadAll = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

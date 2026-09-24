using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Striverum
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public bool YesNo = false;

        public DownloadWindow(GameBananaAPIV4 record)
        {
            InitializeComponent();
            ModTitleBlock.Text = record?.Title ?? "Mod";
            SubmitterBlock.Text = !string.IsNullOrEmpty(record?.Owner?.Name) ? $"Submitted by {record.Owner.Name}" : "";
            if (record?.Image != null)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = record.Image;
                    bitmap.EndInit();
                    Preview.Source = bitmap;
                }
                catch { }
            }
        }

        public DownloadWindow(GameBananaRecord record)
        {
            InitializeComponent();
            ModTitleBlock.Text = record?.Title ?? "Mod";
            SubmitterBlock.Text = !string.IsNullOrEmpty(record?.Owner?.Name) ? $"Submitted by {record.Owner.Name}" : "";
            if (record?.Image != null)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = record.Image;
                    bitmap.EndInit();
                    Preview.Source = bitmap;
                }
                catch { }
            }
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            YesNo = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            YesNo = false;
            Close();
        }
    }
}

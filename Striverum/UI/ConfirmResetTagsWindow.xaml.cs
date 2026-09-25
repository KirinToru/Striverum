using System.Windows;

namespace Striverum.UI
{
    public partial class ConfirmResetTagsWindow : Window
    {
        public ConfirmResetTagsWindow()
        {
            InitializeComponent();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

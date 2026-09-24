using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Striverum
{
    /// <summary>
    /// Converts a tag name string to its first 2 characters (initials) for display in circular badges.
    /// </summary>
    public class TagInitialConverter : IValueConverter
    {
        public static readonly TagInitialConverter Instance = new TagInitialConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && s.Length > 0)
            {
                var words = s.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2)
                    return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[1][0])}";
                return s.Length >= 2 ? s.Substring(0, 2).ToUpper() : s.ToUpper();
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new BoolToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool flag && flag;
            if (parameter?.ToString() == "Inverse")
                b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

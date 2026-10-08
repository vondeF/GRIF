using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GRIF.ViewModels
{
    class LengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility && visibility == Visibility.Visible)
                return new GridLength(1, GridUnitType.Star);
            else
                return new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

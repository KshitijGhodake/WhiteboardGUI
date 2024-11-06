using System;
using System.Globalization;
using System.Windows.Data;

namespace WhiteboardGUI.Converters
{
    public class HeightToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height / 2; // Adjust this formula as needed
            }
            return 12; // Default font size if height is not valid
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WhiteboardGUI.Converters
{
    public class DarkModeColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var colorString = values[0] as string;
            bool isDarkMode = (bool)values[1];
            try
            {
                if (ColorConverter.ConvertFromString(colorString) is Color originalColor)
                {
                    if (isDarkMode)
                    {
                        if (originalColor == Colors.Black)
                        {
                            return new SolidColorBrush(Colors.White);
                        }
                    }
                    return new SolidColorBrush(originalColor);
                }
                return DependencyProperty.UnsetValue;
            }
            catch
            {
                Debug.WriteLine("InvalidColor");
                return DependencyProperty.UnsetValue;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

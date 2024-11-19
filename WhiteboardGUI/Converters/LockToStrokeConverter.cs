using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WhiteboardGUI.Converters
{
    public class LockToStrokeConverter : IMultiValueConverter
    {
        public Brush LockedBySelfBrush { get; set; } = Brushes.Blue;
        public Brush LockedByOthersBrush { get; set; } = Brushes.Red;
        public Brush UnlockedBrush { get; set; } = Brushes.Transparent;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return UnlockedBrush;

            if (values[0] == null || values[1] == null || values[2] == null)
                return UnlockedBrush;

            bool isLocked = (bool)values[0];
            double lockedByUserID = (double)values[1];
            double currentUserID = (double)values[2];

            if (isLocked)
            {
                if (lockedByUserID == currentUserID)
                {
                    return Colors.Black;
                }
                else
                {
                    return Colors.Red;
                }
            }
            else
            {
                return Colors.Transparent;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

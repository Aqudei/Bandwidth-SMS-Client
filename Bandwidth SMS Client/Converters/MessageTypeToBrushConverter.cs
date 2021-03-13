using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bandwidth_SMS_Client.Converters
{
    class MessageTypeToBrushConverter : IValueConverter
    {
        static readonly SolidColorBrush MyMessageColor = new SolidColorBrush(Color.FromArgb(30, 0, 127, 150));
        static readonly SolidColorBrush TheirMessageColor = new SolidColorBrush(Color.FromArgb(30, 0, 127, 255));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "OUTGOING" ? MyMessageColor : TheirMessageColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}

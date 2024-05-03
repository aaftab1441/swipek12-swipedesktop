using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SwipeDesktop.Common;
using SwipeDesktop.Models;
using Xceed.Wpf.Toolkit.Core.Converters;

namespace SwipeDesktop.Converters
{
    public class LocationKioskVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var show = value as Boolean?;

            if (show.HasValue && show.Value)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Hidden;
        }
    }
    
}

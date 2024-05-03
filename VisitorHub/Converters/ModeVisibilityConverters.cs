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
    public class TardyModeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tuple = value as Tuple<SwipeMode, bool>;

            if (tuple == null)
                return Visibility.Collapsed;

            if (tuple.Item2 || tuple.Item1 == SwipeMode.ClassroomTardy)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Hidden;
        }
    }

    public class NotTardyModeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tuple = value as Tuple<SwipeMode, bool>;

            if (tuple == null)
                return Visibility.Visible;

            if (tuple.Item1 == SwipeMode.ClassroomTardy || tuple.Item1 == SwipeMode.CafeEntrance || tuple.Item1 == SwipeMode.Group)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Visible;
        }
    }

    public class LocationModeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tuple = value as Tuple<SwipeMode, bool>;

            if (tuple == null)
                return Visibility.Collapsed;

            if (!tuple.Item2 && (tuple.Item1 == SwipeMode.Location || tuple.Item1 == SwipeMode.Group))
                return Visibility.Visible;
            
            //if (value == null || value.ToString().Contains("Location"))
            //    return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Hidden;
        }
    }

    public class NormalModeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tuple = value as Tuple<SwipeMode, bool>;

            if (tuple == null)
                return Visibility.Collapsed;

            if (!tuple.Item2 && (tuple.Item1 == SwipeMode.Entry))
                return Visibility.Visible;

            //if (value == null || value.ToString().Contains("Location"))
            //    return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Hidden;
        }
    }

    public class InOutVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tuple = value as Tuple<SwipeMode, bool>;

            if (tuple == null)
                return Visibility.Visible;

            if (tuple.Item1 == SwipeMode.Location)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }

    public class CafeEntranceVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tuple = value as Tuple<SwipeMode, bool>;

            if (tuple == null)
                return Visibility.Visible;

            if (tuple.Item1 == SwipeMode.CafeEntrance)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }
    public class GroupVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tuple = value as Tuple<SwipeMode, bool>;

            if (tuple == null)
                return Visibility.Visible;

            if (tuple.Item1 == SwipeMode.Group)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }


    public class NumpadVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var searchString = value as string;

            if (string.IsNullOrEmpty(searchString))
                return Visibility.Collapsed;

            if (searchString == "Type or scan the student ID:")
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }


    public class KeyboardVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var searchString = value as string;

            if (string.IsNullOrEmpty(searchString))
                return Visibility.Collapsed;

            if (searchString == "Type the student name:")
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }

    public class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var shown = value as bool?;

            if (!shown.HasValue)
                return Visibility.Visible;

            if (shown.Value)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }
}

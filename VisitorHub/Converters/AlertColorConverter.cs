using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using log4net;

namespace SwipeDesktop.Converters
{
    public class AlertColorConverter : IValueConverter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AlertColorConverter));

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
           
            int color = 0;

            Color brushColor = Colors.Yellow;

            if (int.TryParse(value.ToString(), out color))
            {
                try
                {
                    byte[] bytes = BitConverter.GetBytes(color);
                    brushColor = Color.FromRgb(bytes[2], bytes[1], bytes[0]);

                    return brushColor;
                }catch(Exception ex){
                    Logger.Error(ex);
                }

               
            }

            return brushColor;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}

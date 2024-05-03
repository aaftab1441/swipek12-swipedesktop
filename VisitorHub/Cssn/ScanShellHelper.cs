using System.Reflection;
using System.Resources;

namespace SwipeDesktop.Cssn
{
    public class ScanShellHelper
    {
        private static string AssemblyName
        {
            get
            {
                string[] _assemblyNameArray = Assembly.GetEntryAssembly().EntryPoint.ReflectedType.ToString().Split('.');
                return _assemblyNameArray[0];
            }
        }

        public static string GetDataClassError(int value)
        {
            var resMgr = new ResourceManager(AssemblyName + ".Cssn.IdDataErrors", Assembly.GetExecutingAssembly());
            string valueString = value.ToString();
            try
            {
                string errorValue = resMgr.GetString(valueString);

                if (string.IsNullOrEmpty(errorValue))
                {
                    return (valueString);
                }

                return errorValue;

            }
            catch
            {
                return valueString;
            }
        }

        public static string GetScannerClassError(int value)
        {
            var resMgr = new ResourceManager(AssemblyName + ".Cssn.ScanLibErrors", Assembly.GetExecutingAssembly());
            string valueString = value.ToString();
            try
            {
                string errorValue = resMgr.GetString(valueString);

                if (string.IsNullOrEmpty(errorValue))
                {
                    return (valueString);
                }

                return errorValue;

            }
            catch
            {
                return valueString;
            }
        }

    }
}

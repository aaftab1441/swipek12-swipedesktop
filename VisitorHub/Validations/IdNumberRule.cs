using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SwipeDesktop.Validations
{
    public class IdNumberValidator : ValidationRule
    {
        private int _max = 25;
        public int Max
        {
            get { return _max; }
            set { _max = value; }
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {

            if (value == null)
                return new ValidationResult(false, "Id Number cannot be empty.");

            if (string.IsNullOrWhiteSpace(value.ToString()))
                return new ValidationResult(false, "Id Number cannot be empty.");

            if (value.ToString().Length > Max)
                return new ValidationResult(false, string.Format("Id Number cannot be more than {0} characters.", Max));
            
            return ValidationResult.ValidResult;
        }
    }
}

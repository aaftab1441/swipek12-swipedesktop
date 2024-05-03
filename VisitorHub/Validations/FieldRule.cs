using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SwipeDesktop.Validations
{
    public class FieldValidator : ValidationRule
    {
        private int _max = 25;
        public int Max
        {
            get { return _max; }
            set { _max = value; }
        }

        private string _field = "Field";
        public string Field
        {
            get { return _field; }
            set { _field = value; }
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {

            if (value == null)
                return new ValidationResult(false, string.Format("{0} cannot be empty.", Field));

            if (string.IsNullOrWhiteSpace(value.ToString()))
                return new ValidationResult(false, string.Format("{0} cannot be empty.", Field));

            if (value.ToString().Length > Max)
                return new ValidationResult(false, string.Format("{0} cannot be more than {1} characters.", Field, Max));
            
            return ValidationResult.ValidResult;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SwipeDesktop.Validations
{
    public class BirthDateValidator : ValidationRule
    {
        private DateTime _birthDate;

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {

            if (value == null)
                return new ValidationResult(false, "Date of birth cannot be empty.");

            if (string.IsNullOrWhiteSpace(value.ToString()))
                return new ValidationResult(false, "Date of birth cannot be empty.");

            if (!DateTime.TryParse(value.ToString(), out _birthDate))
                return new ValidationResult(false, string.Format("Date of birth is not valid."));
            
            return ValidationResult.ValidResult;
        }
    }
}

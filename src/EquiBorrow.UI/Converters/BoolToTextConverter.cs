using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EquiBorrow.UI.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public string TrueText { get; set; } = "Active";
        public string FalseText { get; set; } = "Inactive";

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueText : FalseText;

            return FalseText;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
//fahhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh,I DONT WANT TO TOUCH GITHUB ANYMORE. WE HAVE BEEEF
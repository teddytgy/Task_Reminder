using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Task_Reminder.Wpf.Converters;

public sealed class UtcToLocalDateTimeConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.ToLocalTime().ToString("g", culture);
        }

        return "-";
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

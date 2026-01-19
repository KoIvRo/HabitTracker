using System.Globalization;

namespace HabitTracker.Converters;

public class MoodToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int mood)
        {
            return mood switch
            {
                1 => Color.FromArgb("#8A2BE2"),  // Purple
                2 => Color.FromArgb("#1E90FF"),  // Blue
                3 => Color.FromArgb("#00BFFF"),  // Light blue
                4 => Color.FromArgb("#A9A9A9"),  // Gray
                5 => Color.FromArgb("#FFD700"),  // Yellow
                6 => Color.FromArgb("#FF8C00"),  // Orange
                7 => Color.FromArgb("#DC143C"),  // Red
                _ => Color.FromArgb("#333333")   // Default
            };
        }
        return Color.FromArgb("#333333");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CompletionToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double completionRate)
        {
            // Calculate color based on completion rate (0-1)
            if (completionRate <= 0)
                return Color.FromArgb("#121212"); // Background color

            // Non-linear scaling for better visual
            float factor = (float)Math.Pow(completionRate, 0.7);

            // Dark green to bright green
            byte r = (byte)(0x4C * factor);
            byte g = (byte)(0xAF * factor);
            byte b = (byte)(0x50 * factor);

            return Color.FromRgb(r, g, b);
        }
        return Color.FromArgb("#121212");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
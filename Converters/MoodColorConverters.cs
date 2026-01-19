using System.Globalization;

namespace HabitTracker.Converters;

public class MoodColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int selectedMood && parameter is string paramStr && int.TryParse(paramStr, out int circleNumber))
        {
            // Если этот кружок выбран - показываем цвет, если нет - светло-серый
            if (selectedMood == circleNumber)
            {
                // Цвета для шкалы (фиолетовый → красный)
                return circleNumber switch
                {
                    1 => Color.FromArgb("#8A2BE2"),  // Темно-фиолетовый
                    2 => Color.FromArgb("#9370DB"),  // Средне-фиолетовый  
                    3 => Color.FromArgb("#6495ED"),  // Голубой
                    4 => Color.FromArgb("#00BFFF"),  // Ярко-голубой
                    5 => Color.FromArgb("#FFD700"),  // Золотой
                    6 => Color.FromArgb("#FF6347"),  // Оранжево-красный
                    7 => Color.FromArgb("#DC143C"),  // Ярко-красный
                    _ => Colors.LightGray
                };
            }
            else
            {
                // Не выбранный кружок - светло-серый с тонкой обводкой
                return Colors.LightGray;
            }
        }
        return Colors.LightGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
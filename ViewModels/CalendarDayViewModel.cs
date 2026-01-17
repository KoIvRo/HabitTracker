using CommunityToolkit.Mvvm.ComponentModel;

namespace HabitTracker.ViewModels;

public partial class CalendarDayViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private int _dayNumber;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private int _mood;

    [ObservableProperty]
    private int _completedHabits;

    [ObservableProperty]
    private int _totalHabits;

    [ObservableProperty]
    private Color _moodColor = Colors.Gray;

    [ObservableProperty]
    private Color _completionColor = Colors.Transparent;

    [ObservableProperty]
    private float _completionRatio = 1f;

    [ObservableProperty]
    private string _completionText;

    partial void OnHasDataChanged(bool value)
    {
        UpdateColors();
    }

    partial void OnMoodChanged(int value)
    {
        UpdateColors();
    }

    partial void OnCompletedHabitsChanged(int value)
    {
        UpdateColors();
    }

    partial void OnTotalHabitsChanged(int value)
    {
        UpdateColors();
    }

    private void UpdateColors()
    {
        if (!HasData || Mood == 0)
        {
            MoodColor = Colors.Gray;
            CompletionColor = Colors.Transparent;
            CompletionRatio = 1f;
            CompletionText = "";
            return;
        }

        // Цвет настроения
        MoodColor = Mood switch
        {
            1 => Color.FromArgb("#1e3c72"),
            2 => Color.FromArgb("#2a5298"),
            3 => Color.FromArgb("#3a6bc2"),
            4 => Color.FromArgb("#4a85e6"),
            5 => Color.FromArgb("#6ba1ff"),
            6 => Color.FromArgb("#ffcc00"),
            7 => Color.FromArgb("#ff9900"),
            8 => Color.FromArgb("#ff6600"),
            9 => Color.FromArgb("#ff3300"),
            10 => Color.FromArgb("#cc0000"),
            _ => Colors.Gray
        };

        // Заливка для выполненных привычек
        if (TotalHabits > 0)
        {
            CompletionColor = Color.FromArgb("#4CAF50");
            CompletionRatio = 1f - ((float)CompletedHabits / TotalHabits);
            CompletionText = $"{CompletedHabits}/{TotalHabits}";
        }
        else
        {
            CompletionColor = Colors.Transparent;
            CompletionRatio = 1f;
            CompletionText = "";
        }
    }

    public string MoodEmoji => Mood switch
    {
        1 => "😭",
        2 => "😢",
        3 => "😔",
        4 => "😐",
        5 => "🙂",
        6 => "😊",
        7 => "😄",
        8 => "😁",
        9 => "🤩",
        10 => "🥳",
        _ => ""
    };
}
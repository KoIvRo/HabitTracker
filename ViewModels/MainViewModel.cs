using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HabitTracker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private int _mood = 4;

    [ObservableProperty]
    private Color _moodColor = Color.FromArgb("#00BFFF");

    [ObservableProperty]
    private string _moodDescription = "Нейтрально";

    public MainViewModel()
    {
        // Конструктор
    }

    [RelayCommand]
    private async Task GoToCalendar()
    {
        // Переход на страницу календаря
        await Shell.Current.GoToAsync("CalendarPage");
    }

    [RelayCommand]
    private void SetMood(int moodLevel)
    {
        Mood = moodLevel;
        UpdateMoodColor();
    }

    private void UpdateMoodColor()
    {
        // Цвета для шкалы настроения (фиолетовый → красный)
        MoodColor = Mood switch
        {
            1 => Color.FromArgb("#8A2BE2"),  // Темно-фиолетовый
            2 => Color.FromArgb("#9370DB"),  // Средне-фиолетовый
            3 => Color.FromArgb("#6495ED"),  // Голубой
            4 => Color.FromArgb("#00BFFF"),  // Ярко-голубой
            5 => Color.FromArgb("#FFD700"),  // Золотой
            6 => Color.FromArgb("#FF6347"),  // Оранжево-красный
            7 => Color.FromArgb("#DC143C"),  // Ярко-красный
            _ => Colors.Gray
        };

        MoodDescription = Mood switch
        {
            1 => "Очень плохо",
            2 => "Плохо",
            3 => "Слегка плохо",
            4 => "Нейтрально",
            5 => "Хорошо",
            6 => "Очень хорошо",
            7 => "Отлично!",
            _ => "Не выбрано"
        };
    }

    [RelayCommand]
    private void PreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand]
    private void NextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }
}
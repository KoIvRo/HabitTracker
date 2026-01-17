using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HabitTracker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private int _mood = 5;

    [ObservableProperty]
    private Color _moodColor = Color.FromArgb("#6ba1ff");

    [ObservableProperty]
    private string _moodDescription = "Нормально";

    public MainViewModel()
    {
        // Конструктор
    }

    [RelayCommand]
    private async Task GoToCalendar()
    {
        // Простой переход на страницу календаря
        await Shell.Current.GoToAsync("CalendarPage");
    }

    [RelayCommand]
    private void SetTestMood()
    {
        // Циклически меняем настроение
        Mood = (Mood % 10) + 1;
        UpdateMoodColor();
    }

    private void UpdateMoodColor()
    {
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

        MoodDescription = Mood switch
        {
            1 => "Очень плохо",
            2 => "Плохо",
            3 => "Не очень",
            4 => "Средне",
            5 => "Нормально",
            6 => "Хорошо",
            7 => "Очень хорошо",
            8 => "Отлично",
            9 => "Прекрасно",
            10 => "Эйфория",
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

    [RelayCommand]
    private void Today()
    {
        SelectedDate = DateTime.Today;
    }

    [RelayCommand]
    private async Task ShowMessage()
    {
        await Application.Current.MainPage.DisplayAlert(
            "Информация",
            $"Текущая дата: {SelectedDate:dd.MM.yyyy}\nНастроение: {MoodDescription}",
            "OK");
    }
}
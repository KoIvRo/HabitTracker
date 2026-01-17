using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitTracker.Database;
using System.Collections.ObjectModel;

namespace HabitTracker.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly DatabaseContext _database;

    [ObservableProperty]
    private DateTime _currentMonth = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<CalendarDayViewModel> _calendarDays = new();

    [ObservableProperty]
    private string _currentMonthYear;

    [ObservableProperty]
    private string _statsText;

    public CalendarViewModel()
    {
        _database = new DatabaseContext();
        UpdateMonthYear();
        LoadCalendar();

        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(CurrentMonth))
            {
                UpdateMonthYear();
                LoadCalendar();
            }
        };
    }

    private void UpdateMonthYear()
    {
        CurrentMonthYear = CurrentMonth.ToString("MMMM yyyy");
    }

    private async void LoadCalendar()
    {
        var days = new ObservableCollection<CalendarDayViewModel>();

        // Первый день месяца
        var firstDay = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
        // Последний день месяца
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        // Получаем записи за месяц
        var records = await _database.GetRecordsDictionaryAsync(firstDay, lastDay);

        // Определяем день недели первого дня (0-воскресенье, 1-понедельник...)
        int firstDayOffset = ((int)firstDay.DayOfWeek + 6) % 7; // Чтобы понедельник был первым

        // Пустые дни перед первым числом
        for (int i = 0; i < firstDayOffset; i++)
        {
            days.Add(new CalendarDayViewModel { IsEmpty = true });
        }

        // Дни месяца
        for (int day = 1; day <= lastDay.Day; day++)
        {
            var date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, day);
            var record = records.ContainsKey(date) ? records[date] : null;

            days.Add(new CalendarDayViewModel
            {
                Date = date,
                DayNumber = day,
                HasData = record != null,
                Mood = record?.Mood ?? 0,
                CompletedHabits = record?.CompletedHabits ?? 0,
                TotalHabits = record?.TotalHabits ?? 0
            });
        }

        CalendarDays = days;
        UpdateStats();
    }

    private void UpdateStats()
    {
        var daysWithData = CalendarDays.Count(d => d.HasData);
        var totalDays = CalendarDays.Count(d => !d.IsEmpty);

        StatsText = $"Заполнено дней: {daysWithData} из {totalDays}";
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
    }

    [RelayCommand]
    private void NextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
    }

    [RelayCommand]
    private async Task SelectDate(DateTime date)
    {
        // Возвращаемся на главную с выбранной датой
        await Shell.Current.GoToAsync($"//MainPage?SelectedDate={date:yyyy-MM-dd}");
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task GoToToday()
    {
        CurrentMonth = DateTime.Today;
        await Shell.Current.GoToAsync($"//MainPage?SelectedDate={DateTime.Today:yyyy-MM-dd}");
    }
}
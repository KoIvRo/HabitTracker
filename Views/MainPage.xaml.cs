using HabitTracker.Database;
using HabitTracker.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;

namespace HabitTracker.Views;

public partial class MainPage : ContentPage
{
    private DateTime _selectedDate = DateTime.Today;
    private int _currentMood = 4;
    private DatabaseContext _database;
    private List<Habit> _habits = new();
    private Dictionary<int, bool> _habitCompletionStatus = new();

    public MainPage()
    {
        try
        {
            InitializeComponent();
            _database = new DatabaseContext();

            // Инициализация
            LoadData();

            // Назначение обработчиков
            PrevDayBtn.Clicked += OnPrevDayClicked;
            NextDayBtn.Clicked += OnNextDayClicked;
            CalendarBtn.Clicked += OnCalendarClicked;
            AddHabitBtn.Clicked += OnAddHabitClicked;

            // Обработчики для кнопок настроения
            MoodBtn1.Clicked += (s, e) => SetMood(1);
            MoodBtn2.Clicked += (s, e) => SetMood(2);
            MoodBtn3.Clicked += (s, e) => SetMood(3);
            MoodBtn4.Clicked += (s, e) => SetMood(4);
            MoodBtn5.Clicked += (s, e) => SetMood(5);
            MoodBtn6.Clicked += (s, e) => SetMood(6);
            MoodBtn7.Clicked += (s, e) => SetMood(7);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
        }
    }

    private async void LoadData()
    {
        try
        {
            // Загружаем данные для выбранной даты
            await LoadHabits();
            await LoadMood();
            UpdateDateDisplay();
            UpdateMoodDisplay();
            UpdateHabitsUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
            await DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
        }
    }

    private async Task LoadHabits()
    {
        _habits = await _database.GetHabitsAsync();
        _habitCompletionStatus.Clear();

        // Загружаем статус выполнения для каждой привычки на выбранную дату
        foreach (var habit in _habits)
        {
            var isCompleted = await _database.GetHabitCompletionStatusAsync(habit.Id, _selectedDate);
            _habitCompletionStatus[habit.Id] = isCompleted;
        }
    }

    private async Task LoadMood()
    {
        var record = await _database.GetDailyRecordAsync(_selectedDate);
        if (record != null && record.Mood > 0)
        {
            _currentMood = record.Mood;
        }
        else
        {
            _currentMood = 4; // По умолчанию нейтрально
        }
    }

    private void UpdateDateDisplay()
    {
        DateLabel.Text = _selectedDate.ToString("dd.MM.yyyy");
    }

    private void UpdateMoodDisplay()
    {
        // Устанавливаем цвета для всех кнопок
        SetButtonColors();

        // Сбрасываем обводку у всех кнопок
        ResetButtonBorders();

        // Обводим выбранную кнопку фиолетовым цветом на темном фоне
        Button selectedButton = _currentMood switch
        {
            1 => MoodBtn1,
            2 => MoodBtn2,
            3 => MoodBtn3,
            4 => MoodBtn4,
            5 => MoodBtn5,
            6 => MoodBtn6,
            7 => MoodBtn7,
            _ => null
        };

        if (selectedButton != null)
        {
            selectedButton.BorderColor = Color.FromArgb("#BB86FC");  // Фиолетовая обводка
            selectedButton.BorderWidth = 3;
        }

        // Обновляем описание
        MoodDescriptionLabel.Text = _currentMood switch
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

    private async void UpdateHabitsUI()
    {
        HabitsContainer.Children.Clear();

        foreach (var habit in _habits)
        {
            AddHabitToUI(habit);
        }
    }

    private void SetButtonColors()
    {
        // Устанавливаем цвета для каждой кнопки (яркие на темном фоне)
        MoodBtn1.BackgroundColor = Color.FromArgb("#8A2BE2");  // Фиолетовый
        MoodBtn1.TextColor = Colors.White;

        MoodBtn2.BackgroundColor = Color.FromArgb("#1E90FF");  // Синий
        MoodBtn2.TextColor = Colors.White;

        MoodBtn3.BackgroundColor = Color.FromArgb("#00BFFF");  // Голубой
        MoodBtn3.TextColor = Colors.White;

        MoodBtn4.BackgroundColor = Color.FromArgb("#A9A9A9");  // Серый
        MoodBtn4.TextColor = Colors.White;

        MoodBtn5.BackgroundColor = Color.FromArgb("#FFD700");  // Желтый
        MoodBtn5.TextColor = Colors.Black;

        MoodBtn6.BackgroundColor = Color.FromArgb("#FF8C00");  // Оранжевый
        MoodBtn6.TextColor = Colors.White;

        MoodBtn7.BackgroundColor = Color.FromArgb("#DC143C");  // Красный
        MoodBtn7.TextColor = Colors.White;
    }

    private void ResetButtonBorders()
    {
        MoodBtn1.BorderColor = Colors.Transparent;
        MoodBtn1.BorderWidth = 0;

        MoodBtn2.BorderColor = Colors.Transparent;
        MoodBtn2.BorderWidth = 0;

        MoodBtn3.BorderColor = Colors.Transparent;
        MoodBtn3.BorderWidth = 0;

        MoodBtn4.BorderColor = Colors.Transparent;
        MoodBtn4.BorderWidth = 0;

        MoodBtn5.BorderColor = Colors.Transparent;
        MoodBtn5.BorderWidth = 0;

        MoodBtn6.BorderColor = Colors.Transparent;
        MoodBtn6.BorderWidth = 0;

        MoodBtn7.BorderColor = Colors.Transparent;
        MoodBtn7.BorderWidth = 0;
    }

    private async void SetMood(int mood)
    {
        _currentMood = mood;
        UpdateMoodDisplay();

        // Сохраняем настроение в БД
        var record = await _database.GetDailyRecordAsync(_selectedDate) ??
                    new DailyRecord { Date = _selectedDate };
        record.Mood = mood;

        // Обновляем статистику привычек
        record.TotalHabits = _habits.Count;
        record.CompletedHabits = _habitCompletionStatus.Count(kvp => kvp.Value);

        await _database.SaveDailyRecordAsync(record);
    }

    private async void OnPrevDayClicked(object sender, EventArgs e)
    {
        _selectedDate = _selectedDate.AddDays(-1);
        LoadData();
    }

    private async void OnNextDayClicked(object sender, EventArgs e)
    {
        _selectedDate = _selectedDate.AddDays(1);
        LoadData();
    }

    private async void OnCalendarClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Информация", "Календарь будет добавлен позже", "OK");
    }

    private async void OnAddHabitClicked(object sender, EventArgs e)
    {
        string habitName = await DisplayPromptAsync(
            "Новая привычка",
            "Введите название привычки:",
            "Добавить",
            "Отмена");

        if (!string.IsNullOrWhiteSpace(habitName))
        {
            var habit = new Habit { Name = habitName };
            await _database.AddHabitAsync(habit);
            LoadData();
        }
    }

    private void AddHabitToUI(Habit habit)
    {
        var habitBorder = new Border
        {
            Padding = 12,
            BackgroundColor = Color.FromArgb("#2D2D2D"),
            Stroke = Color.FromArgb("#BB86FC"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) }
        };

        var habitLayout = new HorizontalStackLayout
        {
            Spacing = 10
        };

        var habitLabel = new Label
        {
            Text = habit.Name,
            FontSize = 16,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.StartAndExpand
        };

        var doneBtn = new Button
        {
            Text = "✓",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            BackgroundColor = _habitCompletionStatus.ContainsKey(habit.Id) && _habitCompletionStatus[habit.Id]
                ? Color.FromArgb("#4CAF50")
                : Color.FromArgb("#1B5E20"),
            TextColor = Colors.White
        };

        var notDoneBtn = new Button
        {
            Text = "✗",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            BackgroundColor = _habitCompletionStatus.ContainsKey(habit.Id) && !_habitCompletionStatus[habit.Id]
                ? Color.FromArgb("#F44336")
                : Color.FromArgb("#B71C1C"),
            TextColor = Colors.White
        };

        var deleteBtn = new Button
        {
            Text = "🗑",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            BackgroundColor = Color.FromArgb("#424242"),
            TextColor = Colors.White
        };

        // Обработчики
        doneBtn.Clicked += async (s, e) =>
        {
            await _database.SetHabitCompletionAsync(habit.Id, _selectedDate, true);
            LoadData();
        };

        notDoneBtn.Clicked += async (s, e) =>
        {
            await _database.SetHabitCompletionAsync(habit.Id, _selectedDate, false);
            LoadData();
        };

        deleteBtn.Clicked += async (s, e) =>
        {
            bool confirm = await DisplayAlert(
                "Удаление привычки",
                $"Вы уверены, что хотите удалить привычку \"{habit.Name}\"?",
                "Да",
                "Нет");

            if (confirm)
            {
                await _database.DeleteHabitAsync(habit.Id);
                LoadData();
            }
        };

        habitLayout.Children.Add(habitLabel);
        habitLayout.Children.Add(doneBtn);
        habitLayout.Children.Add(notDoneBtn);
        habitLayout.Children.Add(deleteBtn);

        habitBorder.Content = habitLayout;
        HabitsContainer.Children.Add(habitBorder);
    }
}
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
            BaseHabitsBtn2.Clicked += OnBaseHabitsClicked;

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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            // Загружаем данные для выбранной даты
            await LoadHabits();
            await LoadMood();
            UpdateDateDisplay();
            UpdateDayInfo();
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
        // Загружаем привычки для конкретной даты
        _habits = await _database.GetHabitsForDateAsync(_selectedDate);
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

    private void UpdateDayInfo()
    {
        if (_selectedDate.Date == DateTime.Today.Date)
        {
            DayInfoLabel.Text = "Сегодня. Вы можете отмечать выполнение привычек.";
            DayInfoLabel.TextColor = Color.FromArgb("#4CAF50");
        }
        else if (_selectedDate.Date < DateTime.Today.Date)
        {
            DayInfoLabel.Text = "Прошедший день. Только просмотр.";
            DayInfoLabel.TextColor = Color.FromArgb("#FF9800");
        }
        else
        {
            DayInfoLabel.Text = "Будущий день. Только просмотр.";
            DayInfoLabel.TextColor = Color.FromArgb("#2196F3");
        }
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

        if (_habits.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "Нет привычек на этот день",
                FontSize = 16,
                TextColor = Color.FromArgb("#888888"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20)
            };
            HabitsContainer.Children.Add(emptyLabel);
            return;
        }

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
        // Проверяем, можно ли изменять настроение для этой даты
        if (_selectedDate.Date != DateTime.Today.Date)
        {
            await DisplayAlert("Информация", "Вы можете изменять настроение только для сегодняшнего дня.", "OK");
            return;
        }

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

    private async void OnBaseHabitsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new BaseHabitsPage());
    }

    private async void OnAddHabitClicked(object sender, EventArgs e)
    {
        // Проверяем, можно ли добавлять привычки для этой даты
        if (_selectedDate.Date != DateTime.Today.Date)
        {
            await DisplayAlert("Информация", "Вы можете добавлять привычки только для сегодняшнего дня.", "OK");
            return;
        }

        string habitName = await DisplayPromptAsync(
            "Новая привычка на сегодня",
            "Введите название привычки (будет добавлена только в этот день):",
            "Добавить",
            "Отмена");

        if (!string.IsNullOrWhiteSpace(habitName))
        {
            // Проверяем, не существует ли уже такая привычка сегодня
            var exists = await _database.HabitExistsForTodayAsync(habitName);
            if (exists)
            {
                await DisplayAlert("Внимание", "Привычка с таким названием уже существует сегодня.", "OK");
                return;
            }

            // Создаем привычку только для текущего дня (не базовую)
            var habit = new Habit
            {
                Name = habitName.Trim(),
                IsBaseHabit = false,
                CreatedDate = DateTime.Today
            };
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
            Stroke = habit.IsBaseHabit ? Color.FromArgb("#4CAF50") : Color.FromArgb("#BB86FC"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) }
        };

        var habitLayout = new HorizontalStackLayout
        {
            Spacing = 10
        };

        var habitLabel = new StackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.StartAndExpand
        };

        var nameLabel = new Label
        {
            Text = habit.Name,
            FontSize = 16,
            TextColor = Colors.White
        };

        var typeLabel = new Label
        {
            Text = habit.IsBaseHabit ? "(Базовая)" : "(Только сегодня)",
            FontSize = 12,
            TextColor = habit.IsBaseHabit ? Color.FromArgb("#4CAF50") : Color.FromArgb("#BB86FC")
        };

        habitLabel.Children.Add(nameLabel);
        habitLabel.Children.Add(typeLabel);

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

        // Активируем кнопки только для сегодняшнего дня
        bool canModify = _selectedDate.Date == DateTime.Today.Date;
        doneBtn.IsEnabled = canModify;
        notDoneBtn.IsEnabled = canModify;
        deleteBtn.IsEnabled = canModify;

        if (!canModify)
        {
            doneBtn.BackgroundColor = Color.FromArgb("#555555");
            notDoneBtn.BackgroundColor = Color.FromArgb("#555555");
            deleteBtn.BackgroundColor = Color.FromArgb("#555555");
        }

        // Обработчики
        doneBtn.Clicked += async (s, e) =>
        {
            if (!canModify) return;
            await _database.SetHabitCompletionAsync(habit.Id, _selectedDate, true);
            LoadData();
        };

        notDoneBtn.Clicked += async (s, e) =>
        {
            if (!canModify) return;
            await _database.SetHabitCompletionAsync(habit.Id, _selectedDate, false);
            LoadData();
        };

        deleteBtn.Clicked += async (s, e) =>
        {
            if (!canModify) return;

            if (habit.IsBaseHabit)
            {
                // Для базовой привычки предлагаем удалить только из текущего дня
                bool confirm = await DisplayAlert(
                    "Удаление базовой привычки",
                    $"Удалить базовую привычку \"{habit.Name}\" только из сегодняшнего дня?\n\n" +
                    "✓ Останется базовой привычкой\n" +
                    "✓ Будет появляться в будущих днях\n" +
                    "✓ Останется в прошедших днях\n" +
                    "✓ Исчезнет только из сегодняшнего дня",
                    "Удалить из сегодня",
                    "Отмена");

                if (confirm)
                {
                    try
                    {
                        // Удаляем привычку из сегодняшнего дня
                        var success = await _database.RemoveHabitFromDayAsync(habit.Id, _selectedDate);
                        if (success)
                        {
                            LoadData();
                            await DisplayAlert("Успех", $"Привычка \"{habit.Name}\" удалена из сегодняшнего дня", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось удалить привычку", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", $"Не удалось удалить привычку: {ex.Message}", "OK");
                    }
                }
            }
            else
            {
                // Для обычной привычки удаляем полностью
                bool confirm = await DisplayAlert(
                    "Удаление привычки",
                    $"Удалить привычку \"{habit.Name}\" полностью?\n\n" +
                    "Эта привычка удалится из всех дней.",
                    "Да, удалить",
                    "Отмена");

                if (confirm)
                {
                    try
                    {
                        await _database.DeleteHabitAsync(habit.Id);
                        LoadData();
                        await DisplayAlert("Успех", "Привычка удалена", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", $"Не удалось удалить привычку: {ex.Message}", "OK");
                    }
                }
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
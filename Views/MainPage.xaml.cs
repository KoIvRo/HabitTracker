using Microsoft.Maui.Graphics;

namespace HabitTracker.Views;

public partial class MainPage : ContentPage
{
    private DateTime _selectedDate = DateTime.Today;
    private int _currentMood = 4;
    private List<Border> _moodBorders = new(); // Используем Border вместо Frame

    public MainPage()
    {
        try
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MainPage: InitializeComponent завершен");

            // Инициализация
            UpdateDateDisplay();
            UpdateMoodDisplay();

            // Собираем все границы настроения в список
            _moodBorders = new List<Border>
            {
                MoodBorder1, MoodBorder2, MoodBorder3, MoodBorder4,
                MoodBorder5, MoodBorder6, MoodBorder7
            };

            System.Diagnostics.Debug.WriteLine($"Найдено границ: {_moodBorders.Count}");

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

            // Добавляем тестовые привычки
            AddTestHabits();

            System.Diagnostics.Debug.WriteLine("MainPage: Инициализация завершена успешно");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ОШИБКА В MainPage: {ex.Message} ===");
            System.Diagnostics.Debug.WriteLine($"=== StackTrace: {ex.StackTrace} ===");

            // Показываем ошибку на экране
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label
                    {
                        Text = $"Ошибка: {ex.Message}",
                        TextColor = Colors.Red,
                        FontSize = 16,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = ex.StackTrace,
                        TextColor = Colors.Gray,
                        FontSize = 10,
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            };
        }
    }

    private void UpdateDateDisplay()
    {
        try
        {
            DateLabel.Text = _selectedDate.ToString("dd.MM.yyyy");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка UpdateDateDisplay: {ex.Message}");
        }
    }

    private void UpdateMoodDisplay()
    {
        try
        {
            // Сбрасываем обводку у всех границ
            foreach (var border in _moodBorders)
            {
                border.Stroke = Colors.Transparent;
                border.StrokeThickness = 0;
            }

            // Обводим выбранную границу
            if (_currentMood >= 1 && _currentMood <= 7 && _moodBorders.Count >= _currentMood)
            {
                var selectedBorder = _moodBorders[_currentMood - 1];
                selectedBorder.Stroke = Colors.Black;
                selectedBorder.StrokeThickness = 3;
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка UpdateMoodDisplay: {ex.Message}");
        }
    }

    private void SetMood(int mood)
    {
        _currentMood = mood;
        UpdateMoodDisplay();
    }

    private void OnPrevDayClicked(object sender, EventArgs e)
    {
        _selectedDate = _selectedDate.AddDays(-1);
        UpdateDateDisplay();
    }

    private void OnNextDayClicked(object sender, EventArgs e)
    {
        _selectedDate = _selectedDate.AddDays(1);
        UpdateDateDisplay();
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
            AddHabitToUI(habitName);
        }
    }

    private void AddTestHabits()
    {
        AddHabitToUI("Пить воду");
        AddHabitToUI("Спорт");
        AddHabitToUI("Чтение");
    }

    private void AddHabitToUI(string habitName)
    {
        try
        {
            var habitFrame = new Frame
            {
                Padding = 10,
                BackgroundColor = Colors.LightGray,
                CornerRadius = 5
            };

            var habitLayout = new HorizontalStackLayout
            {
                Spacing = 10
            };

            var habitLabel = new Label
            {
                Text = habitName,
                FontSize = 16,
                VerticalOptions = LayoutOptions.Center
            };

            var doneBtn = new Button
            {
                Text = "✓",
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 20,
                BackgroundColor = Colors.LightGreen
            };

            var notDoneBtn = new Button
            {
                Text = "✗",
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 20,
                BackgroundColor = Colors.LightPink
            };

            var deleteBtn = new Button
            {
                Text = "🗑",
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 20,
                BackgroundColor = Colors.LightGray
            };

            doneBtn.Clicked += (s, e) =>
            {
                doneBtn.BackgroundColor = Colors.Green;
                notDoneBtn.BackgroundColor = Colors.LightPink;
            };

            notDoneBtn.Clicked += (s, e) =>
            {
                notDoneBtn.BackgroundColor = Colors.Red;
                doneBtn.BackgroundColor = Colors.LightGreen;
            };

            deleteBtn.Clicked += (s, e) =>
            {
                HabitsContainer.Children.Remove(habitFrame);
            };

            habitLayout.Children.Add(habitLabel);
            habitLayout.Children.Add(doneBtn);
            habitLayout.Children.Add(notDoneBtn);
            habitLayout.Children.Add(deleteBtn);

            habitFrame.Content = habitLayout;
            HabitsContainer.Children.Add(habitFrame);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка добавления привычки: {ex.Message}");
        }
    }
}
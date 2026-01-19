namespace HabitTracker.Views;

public partial class MainPage : ContentPage
{
    private DateTime _selectedDate = DateTime.Today;
    private int _currentMood = 4;

    public MainPage()
    {
        InitializeComponent();

        // Инициализация
        UpdateDateDisplay();
        UpdateMoodDisplay();

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

        // Обводим выбранную кнопку белым цветом
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
            selectedButton.BorderColor = Colors.White;
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

    private void SetButtonColors()
    {
        // Устанавливаем цвета для каждой кнопки
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
}
using HabitTracker.Database;
using HabitTracker.Models;
using System.Diagnostics;

namespace HabitTracker.Views;

public partial class CalendarPage : ContentPage
{
    private DatabaseContext _database;
    private DateTime _selectedDate = DateTime.Today;
    private Dictionary<DateTime, DailyRecord> _records = new();
    private Dictionary<DateTime, double> _completionRates = new();

    public CalendarPage()
    {
        try
        {
            InitializeComponent();
            _database = new DatabaseContext();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Calendar initialization error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadCalendarData();
    }

    /// <summary>
    /// Загружает данные календаря за последние 64 дня и рассчитывает статистику выполнения привычек.
    /// Для каждого дня вычисляется процент выполнения и загружается настроение.
    /// </summary>
    private async void LoadCalendarData()
    {
        try
        {
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-63);

            _records = await _database.GetRecordsDictionaryAsync(startDate, endDate);

            _completionRates.Clear();
            foreach (var record in _records)
            {
                _completionRates[record.Key] = record.Value.TotalHabits > 0
                    ? (double)record.Value.CompletedHabits / record.Value.TotalHabits
                    : 0;
            }

            GenerateCalendarGrid();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading calendar data: {ex.Message}");
        }
    }

    /// <summary>
    /// Генерирует сетку календаря 8x8 (64 дня) начиная с даты 64 дня назад.
    /// Каждый день представлен квадратом с цветовой индикацией выполнения привычек.
    /// </summary>
    private void GenerateCalendarGrid()
    {
        CalendarContainer.Children.Clear();

        DateTime startDate = DateTime.Today.AddDays(-63);

        for (int row = 0; row < 8; row++)
        {
            var rowLayout = new HorizontalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center
            };

            for (int col = 0; col < 8; col++)
            {
                rowLayout.Children.Add(CreateDayBox(startDate.AddDays(row * 8 + col)));
            }

            CalendarContainer.Children.Add(rowLayout);
        }
    }

    /// <summary>
    /// Создает визуальный элемент для отображения дня в календаре.
    /// Цвет фона зависит от процента выполнения привычек, цвет границы - от настроения.
    /// </summary>
    /// <param name="date">Дата для отображения</param>
    /// <returns>Frame с визуальным представлением дня</returns>
    private View CreateDayBox(DateTime date)
    {
        _records.TryGetValue(date, out var record);
        _completionRates.TryGetValue(date, out var completionRate);

        var dayFrame = new Frame
        {
            WidthRequest = 35,
            HeightRequest = 35,
            CornerRadius = 4,
            BackgroundColor = CalculateCompletionColor(completionRate),
            BorderColor = CalculateMoodColor(record?.Mood ?? 0),
            HasShadow = false,
            Padding = 0
        };

        var dayLabel = new Label
        {
            Text = date.Day.ToString(),
            FontSize = 10,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        dayFrame.Content = dayLabel;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnDayTapped(date, record, completionRate);
        dayFrame.GestureRecognizers.Add(tapGesture);

        return dayFrame;
    }

    /// <summary>
    /// Вычисляет цвет фона дня на основе процента выполнения привычек.
    /// От темного зеленого (#121212) до яркого зеленого (#4CAF50) в зависимости от completionRate.
    /// </summary>
    /// <param name="completionRate">Процент выполнения привычек от 0 до 1</param>
    /// <returns>Цвет фона для отображения</returns>
    private Color CalculateCompletionColor(double completionRate)
    {
        if (completionRate <= 0) return Color.FromArgb("#121212");

        float factor = (float)Math.Pow(completionRate, 0.7);
        return Color.FromRgb(
            (byte)(0x4C * factor),
            (byte)(0xAF * factor),
            (byte)(0x50 * factor)
        );
    }

    /// <summary>
    /// Определяет цвет границы дня на основе уровня настроения.
    /// Соответствует цветовой схеме кнопок настроения на главной странице.
    /// </summary>
    /// <param name="mood">Уровень настроения от 0 до 7</param>
    /// <returns>Цвет границы соответствующего настроению</returns>
    private Color CalculateMoodColor(int mood)
    {
        return mood switch
        {
            1 => Color.FromArgb("#8A2BE2"),
            2 => Color.FromArgb("#1E90FF"),
            3 => Color.FromArgb("#00BFFF"),
            4 => Color.FromArgb("#A9A9A9"),
            5 => Color.FromArgb("#FFD700"),
            6 => Color.FromArgb("#FF8C00"),
            7 => Color.FromArgb("#DC143C"),
            _ => Color.FromArgb("#333333")
        };
    }

    /// <summary>
    /// Обрабатывает нажатие на день календаря, отображая детальную информацию о выбранном дне.
    /// Показывает настроение, статистику выполнения и делает доступной кнопку перехода к дню.
    /// </summary>
    /// <param name="date">Выбранная дата</param>
    /// <param name="record">Запись дня из базы данных</param>
    /// <param name="completionRate">Процент выполнения привычек</param>
    private void OnDayTapped(DateTime date, DailyRecord record, double completionRate)
    {
        _selectedDate = date;
        SelectedDateLabel.Text = date.ToString("dd.MM.yyyy");

        if (record != null)
        {
            string moodText = record.Mood switch
            {
                1 => "Very bad",
                2 => "Bad",
                3 => "Somewhat bad",
                4 => "Neutral",
                5 => "Good",
                6 => "Very good",
                7 => "Excellent",
                _ => "Not set"
            };

            MoodLabel.Text = $"Mood: {record.Mood} ({moodText})";
            CompletionLabel.Text = $"Completed: {record.CompletedHabits}/{record.TotalHabits} ({(completionRate * 100):0}%)";
        }
        else
        {
            MoodLabel.Text = "Mood: Not set";
            CompletionLabel.Text = "No data for this day";
        }

        DayInfoBorder.IsVisible = true;
    }

    /// <summary>
    /// Переход к главной странице с выбранной датой для просмотра и редактирования привычек дня.
    /// </summary>
    private async void OnGoToDayClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//Today?date={_selectedDate:yyyy-MM-dd}");
    }
}
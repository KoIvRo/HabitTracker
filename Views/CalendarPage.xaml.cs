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

    private async void LoadCalendarData()
    {
        try
        {
            // Calculate date range (last 64 days)
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-63); // 64 days total

            // Load records for this period
            var records = await _database.GetRecordsDictionaryAsync(startDate, endDate);
            _records = records;

            // Calculate completion rates
            _completionRates.Clear();
            foreach (var record in records)
            {
                if (record.Value.TotalHabits > 0)
                {
                    double rate = (double)record.Value.CompletedHabits / record.Value.TotalHabits;
                    _completionRates[record.Key] = rate;
                }
                else
                {
                    _completionRates[record.Key] = 0;
                }
            }

            // Generate calendar grid
            GenerateCalendarGrid();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading calendar data: {ex.Message}");
        }
    }

    private void GenerateCalendarGrid()
    {
        CalendarContainer.Children.Clear();

        DateTime today = DateTime.Today;
        DateTime startDate = today.AddDays(-63);

        // Create 8 rows of 8 days (8x8 grid = 64 days)
        for (int row = 0; row < 8; row++)
        {
            var rowLayout = new HorizontalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center
            };

            for (int col = 0; col < 8; col++)
            {
                int dayIndex = row * 8 + col;
                DateTime currentDate = startDate.AddDays(dayIndex);

                var dayBox = CreateDayBox(currentDate);
                rowLayout.Children.Add(dayBox);
            }

            CalendarContainer.Children.Add(rowLayout);
        }
    }

    private View CreateDayBox(DateTime date)
    {
        // Get record for this date
        _records.TryGetValue(date, out var record);
        _completionRates.TryGetValue(date, out var completionRate);

        // Calculate background color based on completion rate
        Color backgroundColor = CalculateCompletionColor(completionRate);

        // Calculate border color based on mood
        Color borderColor = CalculateMoodColor(record?.Mood ?? 0);

        var dayFrame = new Frame
        {
            WidthRequest = 35,
            HeightRequest = 35,
            CornerRadius = 4,
            BackgroundColor = backgroundColor,
            BorderColor = borderColor,
            HasShadow = false,
            Padding = 0
        };

        // Add date label (only day number)
        var dayLabel = new Label
        {
            Text = date.Day.ToString(),
            FontSize = 10,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        dayFrame.Content = dayLabel;

        // Add tap gesture
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnDayTapped(date, record, completionRate);
        dayFrame.GestureRecognizers.Add(tapGesture);

        return dayFrame;
    }

    private Color CalculateCompletionColor(double completionRate)
    {
        // Darker green for lower completion, brighter for higher
        // completionRate from 0 to 1
        if (completionRate <= 0)
            return Color.FromArgb("#121212"); // Background color

        // Scale from dark green to bright green
        float factor = (float)Math.Pow(completionRate, 0.7); // Non-linear for better visual

        // Extract RGB components from #4CAF50
        byte r = (byte)(0x4C * factor);
        byte g = (byte)(0xAF * factor);
        byte b = (byte)(0x50 * factor);

        return Color.FromRgb(r, g, b);
    }

    private Color CalculateMoodColor(int mood)
    {
        return mood switch
        {
            1 => Color.FromArgb("#8A2BE2"), // Purple (Very bad)
            2 => Color.FromArgb("#1E90FF"), // Blue (Bad)
            3 => Color.FromArgb("#00BFFF"), // Light blue (Somewhat bad)
            4 => Color.FromArgb("#A9A9A9"), // Gray (Neutral)
            5 => Color.FromArgb("#FFD700"), // Yellow (Good)
            6 => Color.FromArgb("#FF8C00"), // Orange (Very good)
            7 => Color.FromArgb("#DC143C"), // Red (Excellent)
            _ => Color.FromArgb("#333333")  // Default border
        };
    }

    private void OnDayTapped(DateTime date, DailyRecord record, double completionRate)
    {
        _selectedDate = date;

        // Update selected day info
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

        // Show day info
        DayInfoBorder.IsVisible = true;
    }

    private async void OnGoToDayClicked(object sender, EventArgs e)
    {
        // Navigate back to MainPage with the selected date
        await Shell.Current.GoToAsync($"//Today?date={_selectedDate:yyyy-MM-dd}");
    }
}
using HabitTracker.Database;
using HabitTracker.Models;
using System.Diagnostics;

namespace HabitTracker.Views;

public partial class BaseHabitsPage : ContentPage
{
    private DatabaseContext _database;
    private List<Habit> _baseHabits = new();

    public BaseHabitsPage()
    {
        try
        {
            InitializeComponent();
            _database = new DatabaseContext();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Initialization error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadBaseHabits();
    }

    private async void LoadBaseHabits()
    {
        try
        {
            _baseHabits = await _database.GetBaseHabitsAsync();
            UpdateHabitsUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading basic habits: {ex.Message}");
        }
    }

    private void UpdateHabitsUI()
    {
        HabitsContainer.Children.Clear();

        if (_baseHabits.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No basic habits. Add first one!",
                FontSize = 16,
                TextColor = Color.FromArgb("#888888"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20)
            };
            HabitsContainer.Children.Add(emptyLabel);
            return;
        }

        foreach (var habit in _baseHabits)
        {
            AddHabitToUI(habit);
        }
    }

    /// <summary>
    /// Создает UI-элемент для базовой привычки с кнопкой удаления.
    /// Базовая привычка отображается с зеленой границей и может быть удалена только из будущих дней.
    /// </summary>
    /// <param name="habit">Базовая привычка для отображения</param>
    private void AddHabitToUI(Habit habit)
    {
        var frame = new Frame
        {
            Padding = 12,
            BackgroundColor = Color.FromArgb("#2D2D2D"),
            BorderColor = Color.FromArgb("#4CAF50"),
            CornerRadius = 8,
            HasShadow = false
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

        var deleteBtn = new Button
        {
            Text = "Delete",
            Padding = new Thickness(10, 5),
            BackgroundColor = Color.FromArgb("#D32F2F"),
            TextColor = Colors.White,
            FontSize = 12,
            CornerRadius = 8
        };

        deleteBtn.Clicked += async (s, e) =>
        {
            bool confirm = await DisplayAlert(
                "Delete basic habit",
                $"Delete basic habit \"{habit.Name}\" from future days?\n\n" +
                "✓ Will not appear in future days\n" +
                "✓ Will remain in past days\n" +
                "✓ To remove from today, do it on main page",
                "Delete from future",
                "Cancel");

            if (confirm)
            {
                try
                {
                    var success = await _database.RemoveBaseHabitFromFutureAsync(habit.Id);
                    if (success) LoadBaseHabits();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete habit: {ex.Message}", "OK");
                }
            }
        };

        habitLayout.Children.Add(habitLabel);
        habitLayout.Children.Add(deleteBtn);
        frame.Content = habitLayout;
        HabitsContainer.Children.Add(frame);
    }

    /// <summary>
    /// Создает новую базовую привычку, которая будет появляться во всех будущих днях.
    /// Проверяет уникальность имени привычки перед добавлением.
    /// </summary>
    private async void OnAddHabitClicked(object sender, EventArgs e)
    {
        string habitName = await DisplayPromptAsync(
            "New basic habit",
            "Enter habit name (will be added to all future days):",
            "Add",
            "Cancel",
            maxLength: 50);

        if (!string.IsNullOrWhiteSpace(habitName))
        {
            if (await _database.HabitExistsAsync(habitName))
            {
                await DisplayAlert("Warning", "Habit with this name already exists.", "OK");
                return;
            }

            var habit = new Habit
            {
                Name = habitName.Trim(),
                IsBaseHabit = true,
                DeactivatedDate = null,
                CreatedDate = DateTime.Today
            };
            await _database.AddHabitAsync(habit);
            LoadBaseHabits();
            await DisplayAlert("Success", "Basic habit added", "OK");
        }
    }
}
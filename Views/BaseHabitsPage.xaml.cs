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
            // Load only active basic habits
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

        // Handler for deleting basic habit from future days
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
                    // Remove habit from future days
                    var success = await _database.RemoveBaseHabitFromFutureAsync(habit.Id);
                    if (success)
                    {
                        LoadBaseHabits();
                    }
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
            // Check if habit already exists
            var exists = await _database.HabitExistsAsync(habitName);
            if (exists)
            {
                await DisplayAlert("Warning", "Habit with this name already exists.", "OK");
                return;
            }

            // Create basic habit
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
using HabitTracker.Database;
using HabitTracker.Models;
using Microsoft.Maui.Controls.Shapes;
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
            Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
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
            // Загружаем только активные базовые привычки
            _baseHabits = await _database.GetBaseHabitsAsync();
            UpdateHabitsUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка загрузки базовых привычек: {ex.Message}");
            await DisplayAlert("Ошибка", $"Не удалось загрузить базовые привычки: {ex.Message}", "OK");
        }
    }

    private void UpdateHabitsUI()
    {
        HabitsContainer.Children.Clear();

        if (_baseHabits.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "Нет базовых привычек. Добавьте первую!",
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
        var habitBorder = new Border
        {
            Padding = 12,
            BackgroundColor = Color.FromArgb("#2D2D2D"),
            Stroke = Color.FromArgb("#4CAF50"),
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

        var deleteBtn = new Button
        {
            Text = "🗑 Удалить",
            Padding = new Thickness(10, 5),
            BackgroundColor = Color.FromArgb("#D32F2F"),
            TextColor = Colors.White,
            FontSize = 12,
            CornerRadius = 8
        };

        // Обработчик удаления базовой привычки из будущих дней
        deleteBtn.Clicked += async (s, e) =>
        {
            bool confirm = await DisplayAlert(
                "Удаление базовой привычки",
                $"Удалить базовую привычку \"{habit.Name}\" из будущих дней?\n\n" +
                "✓ Не будет появляться в будущих днях\n" +
                "✓ Останется в прошедших днях\n" +
                "✓ Чтобы удалить из сегодняшнего дня, сделайте это на главной странице",
                "Удалить из будущих",
                "Отмена");

            if (confirm)
            {
                try
                {
                    // Удаляем привычку из будущих дней
                    var success = await _database.RemoveBaseHabitFromFutureAsync(habit.Id);
                    if (success)
                    {
                        LoadBaseHabits();
                        await DisplayAlert("Успех", $"Привычка \"{habit.Name}\" удалена из будущих дней", "OK");
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
        };

        habitLayout.Children.Add(habitLabel);
        habitLayout.Children.Add(deleteBtn);

        habitBorder.Content = habitLayout;
        HabitsContainer.Children.Add(habitBorder);
    }

    private async void OnAddHabitClicked(object sender, EventArgs e)
    {
        string habitName = await DisplayPromptAsync(
            "Новая базовая привычка",
            "Введите название привычки, которая будет автоматически добавляться во все будущие дни:",
            "Добавить",
            "Отмена",
            maxLength: 50);

        if (!string.IsNullOrWhiteSpace(habitName))
        {
            // Проверяем, не существует ли уже такая привычка
            var exists = await _database.HabitExistsAsync(habitName);
            if (exists)
            {
                await DisplayAlert("Внимание", "Привычка с таким названием уже существует.", "OK");
                return;
            }

            // Создаем базовую привычку
            var habit = new Habit
            {
                Name = habitName.Trim(),
                IsBaseHabit = true,
                DeactivatedDate = null,
                CreatedDate = DateTime.Today
            };
            await _database.AddHabitAsync(habit);
            LoadBaseHabits();
            await DisplayAlert("Успех", "Базовая привычка добавлена", "OK");
        }
    }
}
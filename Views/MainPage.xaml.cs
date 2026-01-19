using HabitTracker.Database;
using HabitTracker.Models;
using System.Diagnostics;

namespace HabitTracker.Views;

[QueryProperty(nameof(SelectedDateString), "date")]
public partial class MainPage : ContentPage
{
    private DateTime _selectedDate = DateTime.Today;
    private int _currentMood = 4;
    private DatabaseContext _database;
    private List<Habit> _habits = new();
    private Dictionary<int, bool> _habitCompletionStatus = new();

    public string SelectedDateString
    {
        set
        {
            if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out var date))
            {
                _selectedDate = date.Date;
                if (_database != null)
                {
                    LoadData();
                }
            }
        }
    }

    public MainPage()
    {
        try
        {
            InitializeComponent();
            _database = new DatabaseContext();

            LoadData();

            PrevDayBtn.Clicked += OnPrevDayClicked;
            NextDayBtn.Clicked += OnNextDayClicked;
            AddHabitBtn.Clicked += OnAddHabitClicked;

            MoodBtn1.Clicked += (s, e) => SetMood(1);
            MoodBtn2.Clicked += (s, e) => SetMood(2);
            MoodBtn3.Clicked += (s, e) => SetMood(3);
            MoodBtn4.Clicked += (s, e) => SetMood(4);
            MoodBtn5.Clicked += (s, e) => SetMood(5);
            MoodBtn6.Clicked += (s, e) => SetMood(6);
            MoodBtn7.Clicked += (s, e) => SetMood(7);

            AddHoverEffectsToMoodButtons();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Initialization error: {ex.Message}");
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
            await LoadHabits();
            await LoadMood();
            UpdateDateDisplay();
            UpdateMoodDisplay();
            UpdateHabitsUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Data loading error: {ex.Message}");
        }
    }

    private async Task LoadHabits()
    {
        _habits = await _database.GetHabitsForDateAsync(_selectedDate);
        _habitCompletionStatus.Clear();

        foreach (var habit in _habits)
        {
            var isCompleted = await _database.GetHabitCompletionStatusAsync(habit.Id, _selectedDate);
            _habitCompletionStatus[habit.Id] = isCompleted;
        }
    }

    private async Task LoadMood()
    {
        var record = await _database.GetDailyRecordAsync(_selectedDate);
        _currentMood = record?.Mood > 0 ? record.Mood : 4;
    }

    private void UpdateDateDisplay()
    {
        DateLabel.Text = _selectedDate.ToString("dd.MM.yyyy");
    }

    private void UpdateMoodDisplay()
    {
        SetButtonColors();
        ResetButtonBorders();

        Button selectedButton = _currentMood switch
        {
            1 => MoodBtn1, 2 => MoodBtn2, 3 => MoodBtn3, 4 => MoodBtn4,
            5 => MoodBtn5, 6 => MoodBtn6, 7 => MoodBtn7, _ => null
        };

        if (selectedButton != null)
        {
            selectedButton.BorderColor = Color.FromArgb("#BB86FC");
            selectedButton.BorderWidth = 3;
        }

        MoodDescriptionLabel.Text = _currentMood switch
        {
            1 => "Very bad", 2 => "Bad", 3 => "Somewhat bad", 4 => "Neutral",
            5 => "Good", 6 => "Very good", 7 => "Excellent!", _ => "Not selected"
        };
    }

    private async void UpdateHabitsUI()
    {
        HabitsContainer.Children.Clear();

        if (_habits.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No notes for this day",
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
        MoodBtn1.BackgroundColor = Color.FromArgb("#8A2BE2");
        MoodBtn2.BackgroundColor = Color.FromArgb("#1E90FF");
        MoodBtn3.BackgroundColor = Color.FromArgb("#00BFFF");
        MoodBtn4.BackgroundColor = Color.FromArgb("#A9A9A9");
        MoodBtn5.BackgroundColor = Color.FromArgb("#FFD700");
        MoodBtn6.BackgroundColor = Color.FromArgb("#FF8C00");
        MoodBtn7.BackgroundColor = Color.FromArgb("#DC143C");
    }

    private void ResetButtonBorders()
    {
        var buttons = new[] { MoodBtn1, MoodBtn2, MoodBtn3, MoodBtn4, MoodBtn5, MoodBtn6, MoodBtn7 };
        foreach (var button in buttons)
        {
            button.BorderColor = Colors.Transparent;
            button.BorderWidth = 0;
        }
    }

    /// <summary>
    /// Добавляет эффекты наведения и отведения для кнопок настроения.
    /// Использует Windows-specific API для обработки событий PointerEntered/PointerExited.
    /// При наведении: увеличение на 20%, белая граница, свечение.
    /// При отведении: возврат к исходному размеру, восстановление состояния.
    /// </summary>
    private void AddHoverEffectsToMoodButtons()
    {
        var buttons = new[] { MoodBtn1, MoodBtn2, MoodBtn3, MoodBtn4, MoodBtn5, MoodBtn6, MoodBtn7 };

        foreach (var button in buttons)
        {
            var originalBorderColor = button.BorderColor;
            var originalBorderWidth = button.BorderWidth;

#if WINDOWS
            button.HandlerChanged += (s, e) =>
            {
                if (button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button winButton)
                {
                    winButton.PointerEntered += (sender, args) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            button.ScaleTo(1.2, 100, Easing.CubicInOut);
                            button.BorderColor = Colors.White;
                            button.BorderWidth = 2;
                            button.Shadow = new Shadow
                            {
                                Brush = new SolidColorBrush(Colors.White),
                                Offset = new Point(0, 0),
                                Radius = 15,
                                Opacity = 1
                            };
                        });
                    };

                    winButton.PointerExited += (sender, args) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            button.ScaleTo(1.0, 100, Easing.CubicInOut);
                            button.BorderColor = originalBorderColor;
                            button.BorderWidth = originalBorderWidth;
                            button.Shadow = null;

                            int buttonIndex = Array.IndexOf(buttons, button) + 1;
                            if (buttonIndex == _currentMood)
                            {
                                button.BorderColor = Color.FromArgb("#BB86FC");
                                button.BorderWidth = 3;
                            }
                        });
                    };
                }
            };
#endif
        }
    }

    /// <summary>
    /// Устанавливает настроение для текущей даты и сохраняет в базу данных.
    /// Можно изменять только настроение для сегодняшнего дня.
    /// </summary>
    /// <param name="mood">Уровень настроения от 1 (очень плохо) до 7 (отлично)</param>
    private async void SetMood(int mood)
    {
        if (_selectedDate.Date != DateTime.Today.Date)
        {
            await DisplayAlert("Info", "You can change mood only for today.", "OK");
            return;
        }

        _currentMood = mood;
        UpdateMoodDisplay();

        var record = await _database.GetDailyRecordAsync(_selectedDate) ??
                    new DailyRecord { Date = _selectedDate };
        record.Mood = mood;
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

    private async void OnAddHabitClicked(object sender, EventArgs e)
    {
        if (_selectedDate.Date != DateTime.Today.Date)
        {
            await DisplayAlert("Info", "You can add notes only for today.", "OK");
            return;
        }

        string habitName = await DisplayPromptAsync(
            "New note for today",
            "Enter note name (will be added only for today):",
            "Add",
            "Cancel");

        if (!string.IsNullOrWhiteSpace(habitName))
        {
            if (await _database.HabitExistsForTodayAsync(habitName))
            {
                await DisplayAlert("Warning", "Note with this name already exists today.", "OK");
                return;
            }

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

    /// <summary>
    /// Создает UI-элемент для привычки с кнопками выполнения и удаления.
    /// Кнопка выполнения меняет цвет с серого на фиолетовый при выполнении.
    /// Добавляет эффекты наведения для обеих кнопок.
    /// </summary>
    /// <param name="habit">Привычка для отображения</param>
    private void AddHabitToUI(Habit habit)
    {
        var isCompleted = _habitCompletionStatus.ContainsKey(habit.Id) && _habitCompletionStatus[habit.Id];

        var frame = new Frame
        {
            Padding = new Thickness(12, 8),
            BackgroundColor = Color.FromArgb("#2D2D2D"),
            BorderColor = habit.IsBaseHabit ? Color.FromArgb("#4CAF50") : Color.FromArgb("#BB86FC"),
            CornerRadius = 8,
            HasShadow = false
        };

        var habitLayout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        var nameStack = new StackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center
        };

        var nameLabel = new Label
        {
            Text = habit.Name,
            FontSize = 16,
            TextColor = isCompleted ? Color.FromArgb("#888888") : Colors.White,
            VerticalOptions = LayoutOptions.Center
        };

        if (isCompleted)
        {
            nameLabel.TextDecorations = TextDecorations.Strikethrough;
        }

        var typeLabel = new Label
        {
            Text = habit.IsBaseHabit ? "(Habit)" : "(Today only)",
            FontSize = 12,
            TextColor = habit.IsBaseHabit ? Color.FromArgb("#4CAF50") : Color.FromArgb("#BB86FC")
        };

        nameStack.Children.Add(nameLabel);
        nameStack.Children.Add(typeLabel);
        Grid.SetColumn(nameStack, 0);
        habitLayout.Children.Add(nameStack);

        var checkButton = new Button
        {
            Text = "✓",
            FontSize = 16,
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            BackgroundColor = isCompleted ? Color.FromArgb("#BB86FC") : Color.FromArgb("#2D2D2D"),
            TextColor = isCompleted ? Colors.White : Color.FromArgb("#555555"),
            BorderColor = isCompleted ? Color.FromArgb("#BB86FC") : Color.FromArgb("#555555"),
            BorderWidth = 1,
            Opacity = 0.9
        };

        var deleteButton = new Button
        {
            Text = "✕",
            FontSize = 16,
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            BackgroundColor = Color.FromArgb("#2D2D2D"),
            TextColor = Color.FromArgb("#FF5252"),
            BorderColor = Color.FromArgb("#FF5252"),
            BorderWidth = 1,
            Opacity = 0.9
        };

        AddHoverEffectsToHabitButtons(checkButton, deleteButton, isCompleted);

        bool canModify = _selectedDate.Date == DateTime.Today.Date;
        checkButton.IsEnabled = canModify;
        deleteButton.IsEnabled = canModify;

        if (!canModify)
        {
            checkButton.Opacity = 0.3;
            deleteButton.Opacity = 0.3;
        }

        checkButton.Clicked += async (s, e) =>
        {
            if (!canModify) return;
            await _database.SetHabitCompletionAsync(habit.Id, _selectedDate, !isCompleted);
            LoadData();
        };

        deleteButton.Clicked += async (s, e) =>
        {
            if (!canModify) return;

            if (habit.IsBaseHabit)
            {
                bool confirm = await DisplayAlert(
                    "Remove basic habit",
                    $"Remove basic habit \"{habit.Name}\" only from today?\n\n" +
                    "✓ Will remain a basic habit\n" +
                    "✓ Will appear in future days\n" +
                    "✓ Will remain in past days\n" +
                    "✓ Will be removed only from today",
                    "Remove from today",
                    "Cancel");

                if (confirm)
                {
                    try
                    {
                        var success = await _database.RemoveHabitFromDayAsync(habit.Id, _selectedDate);
                        if (success) LoadData();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to remove note: {ex.Message}", "OK");
                    }
                }
            }
            else
            {
                bool confirm = await DisplayAlert(
                    "Delete note",
                    $"Delete note \"{habit.Name}\" completely?\n\n" +
                    "This note will be deleted from all days.",
                    "Delete",
                    "Cancel");

                if (confirm)
                {
                    try
                    {
                        await _database.DeleteHabitAsync(habit.Id);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete note: {ex.Message}", "OK");
                    }
                }
            }
        };

        Grid.SetColumn(checkButton, 1);
        Grid.SetColumn(deleteButton, 2);
        habitLayout.Children.Add(checkButton);
        habitLayout.Children.Add(deleteButton);
        frame.Content = habitLayout;
        HabitsContainer.Children.Add(frame);
    }

    /// <summary>
    /// Добавляет эффекты наведения для кнопок привычек.
    /// Для кнопки выполнения: зеленый цвет при наведении на невыполненную, ярко-фиолетовый для выполненной.
    /// Для кнопки удаления: красный цвет при наведении.
    /// Использует Windows-specific API для обработки событий PointerEntered/PointerExited.
    /// </summary>
    /// <param name="checkButton">Кнопка выполнения привычки</param>
    /// <param name="deleteButton">Кнопка удаления привычки</param>
    /// <param name="isCompleted">Флаг выполнения привычки</param>
    private void AddHoverEffectsToHabitButtons(Button checkButton, Button deleteButton, bool isCompleted)
    {
        var checkOriginalBackground = checkButton.BackgroundColor;
        var checkOriginalTextColor = checkButton.TextColor;
        var checkOriginalBorderColor = checkButton.BorderColor;

        var deleteOriginalBackground = deleteButton.BackgroundColor;
        var deleteOriginalTextColor = deleteButton.TextColor;
        var deleteOriginalBorderColor = deleteButton.BorderColor;

#if WINDOWS
        AddWindowsHoverEffects(checkButton, deleteButton, isCompleted,
            checkOriginalBackground, checkOriginalTextColor, checkOriginalBorderColor,
            deleteOriginalBackground, deleteOriginalTextColor, deleteOriginalBorderColor);
#endif
    }

    /// <summary>
    /// Windows-specific реализация эффектов наведения для кнопок привычек.
    /// Обрабатывает события PointerEntered и PointerExited через WinUI API.
    /// </summary>
#if WINDOWS
    private void AddWindowsHoverEffects(
        Button checkButton, Button deleteButton, bool isCompleted,
        Color checkOriginalBackground, Color checkOriginalTextColor, Color checkOriginalBorderColor,
        Color deleteOriginalBackground, Color deleteOriginalTextColor, Color deleteOriginalBorderColor)
    {
        checkButton.HandlerChanged += (s, e) =>
        {
            if (checkButton.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button winCheckButton)
            {
                winCheckButton.PointerEntered += (sender, args) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        checkButton.ScaleTo(1.3, 100, Easing.CubicInOut);
                        checkButton.Opacity = 1.0;

                        if (!isCompleted)
                        {
                            checkButton.BackgroundColor = Color.FromArgb("#4CAF50");
                            checkButton.TextColor = Colors.White;
                            checkButton.BorderColor = Color.FromArgb("#4CAF50");
                        }
                        else
                        {
                            checkButton.BackgroundColor = Color.FromArgb("#9C27B0");
                            checkButton.TextColor = Colors.White;
                            checkButton.BorderColor = Color.FromArgb("#9C27B0");
                        }
                    });
                };

                winCheckButton.PointerExited += (sender, args) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        checkButton.ScaleTo(1.0, 100, Easing.CubicInOut);
                        checkButton.Opacity = 0.9;
                        checkButton.BackgroundColor = checkOriginalBackground;
                        checkButton.TextColor = checkOriginalTextColor;
                        checkButton.BorderColor = checkOriginalBorderColor;
                    });
                };
            }
        };

        deleteButton.HandlerChanged += (s, e) =>
        {
            if (deleteButton.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button winDeleteButton)
            {
                winDeleteButton.PointerEntered += (sender, args) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        deleteButton.ScaleTo(1.3, 100, Easing.CubicInOut);
                        deleteButton.Opacity = 1.0;
                        deleteButton.BackgroundColor = Color.FromArgb("#FF5252");
                        deleteButton.TextColor = Colors.White;
                        deleteButton.BorderColor = Color.FromArgb("#FF5252");
                    });
                };

                winDeleteButton.PointerExited += (sender, args) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        deleteButton.ScaleTo(1.0, 100, Easing.CubicInOut);
                        deleteButton.Opacity = 0.9;
                        deleteButton.BackgroundColor = deleteOriginalBackground;
                        deleteButton.TextColor = deleteOriginalTextColor;
                        deleteButton.BorderColor = deleteOriginalBorderColor;
                    });
                };
            }
        };
    }
#endif
}
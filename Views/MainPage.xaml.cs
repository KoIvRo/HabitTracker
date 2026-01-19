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
                // Обновляем данные сразу, если страница уже загружена
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

            // Initialization
            LoadData();

            // Assign handlers
            PrevDayBtn.Clicked += OnPrevDayClicked;
            NextDayBtn.Clicked += OnNextDayClicked;
            AddHabitBtn.Clicked += OnAddHabitClicked;

            // Mood button handlers
            MoodBtn1.Clicked += (s, e) => SetMood(1);
            MoodBtn2.Clicked += (s, e) => SetMood(2);
            MoodBtn3.Clicked += (s, e) => SetMood(3);
            MoodBtn4.Clicked += (s, e) => SetMood(4);
            MoodBtn5.Clicked += (s, e) => SetMood(5);
            MoodBtn6.Clicked += (s, e) => SetMood(6);
            MoodBtn7.Clicked += (s, e) => SetMood(7);

            // Добавляем обработчики для наведения на кнопки настроения
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

        // Если пришли из календаря с датой, она уже установлена через QueryProperty
        // Просто загружаем данные
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            // Load data for selected date
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
        // Load habits for specific date
        _habits = await _database.GetHabitsForDateAsync(_selectedDate);
        _habitCompletionStatus.Clear();

        // Load completion status for each habit on selected date
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
            _currentMood = 4; // Default neutral
        }
    }

    private void UpdateDateDisplay()
    {
        DateLabel.Text = _selectedDate.ToString("dd.MM.yyyy");
    }

    private void UpdateMoodDisplay()
    {
        // Set colors for all buttons
        SetButtonColors();

        // Reset all button borders
        ResetButtonBorders();

        // Highlight selected button with purple color
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
            selectedButton.BorderColor = Color.FromArgb("#BB86FC");  // Purple border
            selectedButton.BorderWidth = 3;
        }

        // Update description
        MoodDescriptionLabel.Text = _currentMood switch
        {
            1 => "Very bad",
            2 => "Bad",
            3 => "Somewhat bad",
            4 => "Neutral",
            5 => "Good",
            6 => "Very good",
            7 => "Excellent!",
            _ => "Not selected"
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
        // Set colors for each button (bright on dark background)
        MoodBtn1.BackgroundColor = Color.FromArgb("#8A2BE2");  // Purple
        MoodBtn1.TextColor = Colors.White;

        MoodBtn2.BackgroundColor = Color.FromArgb("#1E90FF");  // Blue
        MoodBtn2.TextColor = Colors.White;

        MoodBtn3.BackgroundColor = Color.FromArgb("#00BFFF");  // Light blue
        MoodBtn3.TextColor = Colors.White;

        MoodBtn4.BackgroundColor = Color.FromArgb("#A9A9A9");  // Gray
        MoodBtn4.TextColor = Colors.White;

        MoodBtn5.BackgroundColor = Color.FromArgb("#FFD700");  // Yellow
        MoodBtn5.TextColor = Colors.Black;

        MoodBtn6.BackgroundColor = Color.FromArgb("#FF8C00");  // Orange
        MoodBtn6.TextColor = Colors.White;

        MoodBtn7.BackgroundColor = Color.FromArgb("#DC143C");  // Red
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

    private void AddHoverEffectsToMoodButtons()
    {
        // Добавляем обработчики событий для всех кнопок настроения
        var buttons = new[] { MoodBtn1, MoodBtn2, MoodBtn3, MoodBtn4, MoodBtn5, MoodBtn6, MoodBtn7 };

        foreach (var button in buttons)
        {
            // Сохраняем оригинальные свойства
            var originalScale = button.Scale;
            var originalBorderColor = button.BorderColor;
            var originalBorderWidth = button.BorderWidth;

            // Обработчик наведения (MouseOver для Windows, Pointer для других платформ)
            button.HandlerChanged += (s, e) =>
            {
                if (button.Handler?.PlatformView is object platformView)
                {
#if WINDOWS
                    var uiElement = platformView as Microsoft.UI.Xaml.FrameworkElement;
                    if (uiElement != null)
                    {
                        uiElement.PointerEntered += (sender, args) =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                // Анимация увеличения
                                button.ScaleTo(1.2, 100, Easing.CubicInOut);
                                button.BorderColor = Colors.White;
                                button.BorderWidth = 2;
                                
                                // Добавляем свечение
                                button.Shadow = new Shadow
                                {
                                    Brush = new SolidColorBrush(Colors.White),
                                    Offset = new Point(0, 0),
                                    Radius = 15,
                                    Opacity = 1
                                };
                            });
                        };

                        uiElement.PointerExited += (sender, args) =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                // Анимация уменьшения
                                button.ScaleTo(1.0, 100, Easing.CubicInOut);
                                
                                // Восстанавливаем оригинальные значения
                                button.BorderColor = originalBorderColor;
                                button.BorderWidth = originalBorderWidth;
                                
                                // Убираем свечение
                                button.Shadow = null;
                                
                                // Обновляем выделение если это выбранная кнопка
                                int buttonIndex = Array.IndexOf(buttons, button) + 1;
                                if (buttonIndex == _currentMood)
                                {
                                    button.BorderColor = Color.FromArgb("#BB86FC");
                                    button.BorderWidth = 3;
                                }
                            });
                        };
                    }
#endif
                }
            };

            // Простой кросс-платформенный подход с использованием GestureRecognizers
            var tapGestureRecognizer = new TapGestureRecognizer();
            var panGestureRecognizer = new PanGestureRecognizer();

            // Добавляем обработчики жестов
            var gestureRecognizers = new IGestureRecognizer[]
            {
                tapGestureRecognizer,
                panGestureRecognizer
            };

            foreach (var gesture in gestureRecognizers)
            {
                button.GestureRecognizers.Add(gesture);
            }

            // Альтернативный подход: используем события, которые доступны в MAUI
            AddMauiHoverEffects(button, buttons);
        }
    }

    private void AddMauiHoverEffects(Button button, Button[] allButtons)
    {
        // Используем доступные в MAUI события
        bool isHovering = false;

        // Для десктопных платформ (Windows, macOS)
        button.HandlerChanged += (s, e) =>
        {
#if WINDOWS
            if (button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button winButton)
            {
                winButton.PointerEntered += (sender, args) =>
                {
                    isHovering = true;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnMoodButtonHover(button, allButtons, true);
                    });
                };

                winButton.PointerExited += (sender, args) =>
                {
                    isHovering = false;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnMoodButtonHover(button, allButtons, false);
                    });
                };
            }
#endif

#if MACCATALYST || IOS
            // Для iOS/Mac используем Touch события
            button.HandlerChanged += (s, e) =>
            {
                // iOS не поддерживает ховер, используем тап
            };
#endif
        };

        // Простой подход через анимацию при нажатии и отпускании
        button.Pressed += (s, e) =>
        {
            // Сохраняем состояние нажатия
            button.ScaleTo(1.1, 50, Easing.CubicInOut);
        };

        button.Released += (s, e) =>
        {
            // Возвращаем к нормальному размеру
            button.ScaleTo(1.0, 50, Easing.CubicInOut);
        };
    }

    private void OnMoodButtonHover(Button button, Button[] allButtons, bool isHovering)
    {
        int buttonIndex = Array.IndexOf(allButtons, button) + 1;
        bool isSelected = buttonIndex == _currentMood;

        if (isHovering)
        {
            // Анимация увеличения при наведении
            button.ScaleTo(1.2, 100, Easing.CubicInOut);
            button.BorderColor = Colors.White;
            button.BorderWidth = 2;

            // Добавляем свечение
            button.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.White),
                Offset = new Point(0, 0),
                Radius = 15,
                Opacity = 1
            };
        }
        else
        {
            // Анимация уменьшения при уходе курсора
            button.ScaleTo(1.0, 100, Easing.CubicInOut);

            // Восстанавливаем состояние
            if (isSelected)
            {
                button.BorderColor = Color.FromArgb("#BB86FC");
                button.BorderWidth = 3;
            }
            else
            {
                button.BorderColor = Colors.Transparent;
                button.BorderWidth = 0;
            }

            // Убираем свечение
            button.Shadow = null;
        }
    }

    private async void SetMood(int mood)
    {
        // Check if mood can be changed for this date
        if (_selectedDate.Date != DateTime.Today.Date)
        {
            await DisplayAlert("Info", "You can change mood only for today.", "OK");
            return;
        }

        _currentMood = mood;
        UpdateMoodDisplay();

        // Save mood to database
        var record = await _database.GetDailyRecordAsync(_selectedDate) ??
                    new DailyRecord { Date = _selectedDate };
        record.Mood = mood;

        // Update habit statistics
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
        // Check if habits can be added for this date
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
            // Check if note already exists today
            var exists = await _database.HabitExistsForTodayAsync(habitName);
            if (exists)
            {
                await DisplayAlert("Warning", "Note with this name already exists today.", "OK");
                return;
            }

            // Create note only for today (not basic)
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
                new ColumnDefinition { Width = GridLength.Star }, // Name
                new ColumnDefinition { Width = GridLength.Auto }, // Check button
                new ColumnDefinition { Width = GridLength.Auto }  // Delete button
            },
            ColumnSpacing = 10
        };

        // Habit name with strike-through if completed
        var isCompleted = _habitCompletionStatus.ContainsKey(habit.Id) && _habitCompletionStatus[habit.Id];

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

        // Add strike-through effect for completed habits
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

        // Check button (✓) - ФИОЛЕТОВАЯ галочка
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

        // Delete button (🗑) - кнопка удаления
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

        // Добавляем обработчики событий для кнопок привычек
        AddHoverEffectsToHabitButtons(checkButton, deleteButton, isCompleted);

        // Activate buttons only for today
        bool canModify = _selectedDate.Date == DateTime.Today.Date;
        checkButton.IsEnabled = canModify;
        deleteButton.IsEnabled = canModify;

        if (!canModify)
        {
            checkButton.Opacity = 0.3;
            deleteButton.Opacity = 0.3;
            checkButton.TextColor = Color.FromArgb("#555555");
            deleteButton.TextColor = Color.FromArgb("#555555");
        }

        // Handlers
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
                // For basic habit - remove only from today
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
                        // Remove habit from today
                        var success = await _database.RemoveHabitFromDayAsync(habit.Id, _selectedDate);
                        if (success)
                        {
                            LoadData();
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to remove note: {ex.Message}", "OK");
                    }
                }
            }
            else
            {
                // For regular note - delete completely
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

    private void AddHoverEffectsToHabitButtons(Button checkButton, Button deleteButton, bool isCompleted)
    {
        // Сохраняем оригинальные свойства для кнопки галочки
        var checkOriginalBackground = checkButton.BackgroundColor;
        var checkOriginalTextColor = checkButton.TextColor;
        var checkOriginalBorderColor = checkButton.BorderColor;

        // Простой подход через анимации при нажатии
        checkButton.Pressed += (s, e) =>
        {
            // Анимация увеличения при нажатии
            checkButton.ScaleTo(1.3, 50, Easing.CubicInOut);
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
        };

        checkButton.Released += (s, e) =>
        {
            // Анимация уменьшения при отпускании
            checkButton.ScaleTo(1.0, 50, Easing.CubicInOut);
            checkButton.Opacity = 0.9;

            // Восстанавливаем оригинальные значения
            checkButton.BackgroundColor = checkOriginalBackground;
            checkButton.TextColor = checkOriginalTextColor;
            checkButton.BorderColor = checkOriginalBorderColor;
        };

        // Сохраняем оригинальные свойства для кнопки удаления
        var deleteOriginalBackground = deleteButton.BackgroundColor;
        var deleteOriginalTextColor = deleteButton.TextColor;
        var deleteOriginalBorderColor = deleteButton.BorderColor;

        // Обработчики для кнопки удаления
        deleteButton.Pressed += (s, e) =>
        {
            // Анимация увеличения при нажатии
            deleteButton.ScaleTo(1.3, 50, Easing.CubicInOut);
            deleteButton.Opacity = 1.0;
            deleteButton.BackgroundColor = Color.FromArgb("#FF5252");
            deleteButton.TextColor = Colors.White;
            deleteButton.BorderColor = Color.FromArgb("#FF5252");
        };

        deleteButton.Released += (s, e) =>
        {
            // Анимация уменьшения при отпускании
            deleteButton.ScaleTo(1.0, 50, Easing.CubicInOut);
            deleteButton.Opacity = 0.9;

            // Восстанавливаем оригинальные значения
            deleteButton.BackgroundColor = deleteOriginalBackground;
            deleteButton.TextColor = deleteOriginalTextColor;
            deleteButton.BorderColor = deleteOriginalBorderColor;
        };
    }
}
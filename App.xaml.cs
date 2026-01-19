using HabitTracker.Views;

namespace HabitTracker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Используем NavigationPage для возможности перехода между страницами
        MainPage = new NavigationPage(new MainPage())
        {
            BarBackgroundColor = Color.FromArgb("#121212"),
            BarTextColor = Colors.White,
        };
    }
}
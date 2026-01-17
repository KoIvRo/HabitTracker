using HabitTracker.Views;

namespace HabitTracker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new MainPage();
    }
}
using HabitTracker.Views;

namespace HabitTracker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // РЕГИСТРИРУЕМ ВСЕ СТРАНИЦЫ
        Routing.RegisterRoute("CalendarPage", typeof(CalendarPage));

        // Важно: создаем Shell, а не сразу MainPage
        MainPage = new AppShell();
    }
}
namespace HabitTracker.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnTestButtonClicked(object sender, EventArgs e)
    {
        if (BindingContext is ViewModels.MainViewModel vm)
        {
            await DisplayAlert("Текущая дата",
                $"Выбрана дата: {vm.SelectedDate:dd.MM.yyyy}",
                "OK");
        }
    }
}
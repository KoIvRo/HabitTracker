using CommunityToolkit.Mvvm.ComponentModel;

namespace HabitTracker.ViewModels;

public partial class HabitViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private Color _statusColor = Colors.LightGray;

    partial void OnIsCompletedChanged(bool value)
    {
        StatusColor = value ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
    }
}
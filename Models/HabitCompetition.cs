// Models/HabitCompletion.cs
using SQLite;

namespace HabitTracker.Models;

public class HabitCompletion
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int HabitId { get; set; }
    public DateTime Date { get; set; }
    public bool IsCompleted { get; set; }
}
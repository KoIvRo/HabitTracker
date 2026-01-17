// Models/DailyRecord.cs
using SQLite;

namespace HabitTracker.Models;

public class DailyRecord
{
    [PrimaryKey]
    public DateTime Date { get; set; }
    public int Mood { get; set; } // 1-10
    public int CompletedHabits { get; set; }
    public int TotalHabits { get; set; }
}
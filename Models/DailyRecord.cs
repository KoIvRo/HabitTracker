using SQLite;

namespace HabitTracker.Models;

public class DailyRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public DateTime Date { get; set; }

    public int Mood { get; set; } // 1-7
    public int CompletedHabits { get; set; }
    public int TotalHabits { get; set; }
}
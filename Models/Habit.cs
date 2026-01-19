using SQLite;

namespace HabitTracker.Models;

public class Habit
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // Новые поля для определения типа привычки
    public bool IsBaseHabit { get; set; } = false; // true - базовая привычка, false - привычка конкретного дня
    public DateTime? DeactivatedDate { get; set; } // Дата деактивации для базовых привычек
}
using SQLite;
using HabitTracker.Models;
using System.Diagnostics;

namespace HabitTracker.Database;

public class DatabaseContext
{
    private readonly SQLiteAsyncConnection _database;
    private bool _isInitialized = false;

    public DatabaseContext()
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "habittracker.db3");
            Debug.WriteLine($"Database path: {dbPath}");

            _database = new SQLiteAsyncConnection(dbPath);

            // Инициализируем БД только один раз
            if (!_isInitialized)
            {
                InitializeDatabase();
                _isInitialized = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    private async void InitializeDatabase()
    {
        try
        {
            // НЕ УДАЛЯЕМ СТАРЫЕ ТАБЛИЦЫ! Только создаем если их нет
            await _database.CreateTableAsync<Habit>();
            await _database.CreateTableAsync<DailyRecord>();
            await _database.CreateTableAsync<HabitCompletion>();

            Debug.WriteLine("Database tables created/checked");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating tables: {ex.Message}");
        }
    }

    // Методы для привычек
    public async Task<List<Habit>> GetHabitsAsync()
    {
        try
        {
            return await _database.Table<Habit>()
                .Where(h => h.IsActive)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting habits: {ex.Message}");
            return new List<Habit>();
        }
    }

    // Получить привычки для конкретной даты
    // Получить привычки для конкретной даты
    public async Task<List<Habit>> GetHabitsForDateAsync(DateTime date)
    {
        try
        {
            var allHabits = await GetHabitsAsync();
            var habitsForDate = new List<Habit>();

            foreach (var habit in allHabits)
            {
                // Если привычка создана до или в выбранную дату
                if (habit.CreatedDate.Date <= date.Date)
                {
                    // Если это базовая привычка
                    if (habit.IsBaseHabit)
                    {
                        // Проверяем, не была ли она деактивирована до этой даты
                        if (!habit.DeactivatedDate.HasValue ||
                            habit.DeactivatedDate.Value.Date > date.Date)
                        {
                            habitsForDate.Add(habit);
                        }
                    }
                    else // Если это НЕ базовая привычка (привычка для конкретного дня)
                    {
                        // Для небазовых привычек добавляем ТОЛЬКО если они созданы в этот же день
                        if (habit.CreatedDate.Date == date.Date)
                        {
                            habitsForDate.Add(habit);
                        }
                    }
                }
            }

            return habitsForDate.OrderBy(h => h.Name).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting habits for date: {ex.Message}");
            return new List<Habit>();
        }
    }

    // Получить только базовые привычки
    public async Task<List<Habit>> GetBaseHabitsAsync()
    {
        try
        {
            return await _database.Table<Habit>()
                .Where(h => h.IsActive && h.IsBaseHabit)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting base habits: {ex.Message}");
            return new List<Habit>();
        }
    }

    public async Task<int> AddHabitAsync(Habit habit)
    {
        try
        {
            return await _database.InsertAsync(habit);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding habit: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> UpdateHabitAsync(Habit habit)
    {
        try
        {
            return await _database.UpdateAsync(habit);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating habit: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> DeleteHabitAsync(int id)
    {
        try
        {
            var habit = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h => h.Id == id);
            if (habit != null)
            {
                // Полное удаление (только для небазовых привычек)
                if (!habit.IsBaseHabit)
                {
                    // Сначала удаляем все выполнения
                    await _database.ExecuteAsync(
                        "DELETE FROM HabitCompletion WHERE HabitId = ?",
                        habit.Id);

                    // Затем удаляем привычку
                    return await _database.DeleteAsync(habit);
                }
                else
                {
                    // Для базовых привычек только деактивируем
                    habit.IsActive = false;
                    return await _database.UpdateAsync(habit);
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting habit: {ex.Message}");
            return 0;
        }
    }

    // Удалить привычку только из текущего дня (не из базы)
    public async Task<int> RemoveHabitFromDayAsync(int habitId, DateTime date)
    {
        try
        {
            // Удаляем все выполнения привычки на эту дату
            await _database.ExecuteAsync(
                "DELETE FROM HabitCompletion WHERE HabitId = ? AND Date = ?",
                habitId, date.Date);

            // Обновляем статистику дня
            await UpdateDailyStatsAsync(date);

            return 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing habit from day: {ex.Message}");
            return 0;
        }
    }

    // Удалить привычку из всех будущих дней
    public async Task<int> RemoveHabitFromFutureDaysAsync(int habitId)
    {
        try
        {
            // Удаляем все выполнения привычки на будущие даты
            return await _database.ExecuteAsync(
                "DELETE FROM HabitCompletion WHERE HabitId = ? AND Date > ?",
                habitId, DateTime.Today);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing habit from future days: {ex.Message}");
            return 0;
        }
    }

    // Методы для ежедневных записей
    public async Task<DailyRecord> GetDailyRecordAsync(DateTime date)
    {
        try
        {
            var normalizedDate = date.Date;
            return await _database.Table<DailyRecord>()
                .FirstOrDefaultAsync(d => d.Date == normalizedDate);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting daily record: {ex.Message}");
            return null;
        }
    }

    public async Task<int> SaveDailyRecordAsync(DailyRecord record)
    {
        try
        {
            record.Date = record.Date.Date;
            var existing = await GetDailyRecordAsync(record.Date);
            if (existing != null)
            {
                record.Id = existing.Id;
                return await _database.UpdateAsync(record);
            }
            return await _database.InsertAsync(record);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving daily record: {ex.Message}");
            return 0;
        }
    }

    // Методы для выполнения привычек
    public async Task<bool> GetHabitCompletionStatusAsync(int habitId, DateTime date)
    {
        try
        {
            var completion = await _database.Table<HabitCompletion>()
                .FirstOrDefaultAsync(hc => hc.HabitId == habitId &&
                                         hc.Date == date.Date);
            return completion?.IsCompleted ?? false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting completion status: {ex.Message}");
            return false;
        }
    }

    public async Task SetHabitCompletionAsync(int habitId, DateTime date, bool isCompleted)
    {
        try
        {
            var completion = await _database.Table<HabitCompletion>()
                .FirstOrDefaultAsync(hc => hc.HabitId == habitId &&
                                         hc.Date == date.Date);

            if (completion != null)
            {
                completion.IsCompleted = isCompleted;
                await _database.UpdateAsync(completion);
            }
            else
            {
                completion = new HabitCompletion
                {
                    HabitId = habitId,
                    Date = date.Date,
                    IsCompleted = isCompleted
                };
                await _database.InsertAsync(completion);
            }

            await UpdateDailyStatsAsync(date);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error setting completion: {ex.Message}");
        }
    }

    private async Task UpdateDailyStatsAsync(DateTime date)
    {
        try
        {
            var habits = await GetHabitsForDateAsync(date);
            var totalHabits = habits.Count;
            var completedCount = 0;

            foreach (var habit in habits)
            {
                if (await GetHabitCompletionStatusAsync(habit.Id, date))
                {
                    completedCount++;
                }
            }

            var record = await GetDailyRecordAsync(date) ?? new DailyRecord { Date = date.Date };
            record.CompletedHabits = completedCount;
            record.TotalHabits = totalHabits;

            await SaveDailyRecordAsync(record);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating daily stats: {ex.Message}");
        }
    }

    // Метод для проверки существования привычки по имени
    public async Task<bool> HabitExistsAsync(string habitName)
    {
        try
        {
            var existing = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h => h.Name.ToLower() == habitName.ToLower() && h.IsActive);

            return existing != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking if habit exists: {ex.Message}");
            return false;
        }
    }
    // В класс DatabaseContext добавьте метод:
    public async Task<bool> RemoveBaseHabitFromTodayAsync(int habitId)
    {
        try
        {
            // Проверяем, что привычка существует и является базовой
            var habit = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h => h.Id == habitId && h.IsActive && h.IsBaseHabit);

            if (habit == null)
                return false;

            // Удаляем выполнение привычки на сегодня
            await _database.ExecuteAsync(
                "DELETE FROM HabitCompletion WHERE HabitId = ? AND Date = ?",
                habitId, DateTime.Today);

            // Обновляем статистику сегодняшнего дня
            await UpdateDailyStatsAsync(DateTime.Today);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing base habit from today: {ex.Message}");
            return false;
        }
    }

    // Метод для проверки существования привычки по имени сегодня
    public async Task<bool> HabitExistsForTodayAsync(string habitName)
    {
        try
        {
            var existing = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h =>
                    h.Name.ToLower() == habitName.ToLower() &&
                    h.IsActive &&
                    !h.IsBaseHabit && // Только небазовые
                    h.CreatedDate.Date == DateTime.Today); // Созданные сегодня
            return existing != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking if habit exists for today: {ex.Message}");
            return false;
        }
    }
}
using SQLite;
using HabitTracker.Models;
using System.Diagnostics;

namespace HabitTracker.Database;

public class DatabaseContext
{
    private readonly SQLiteAsyncConnection _database;
    private static bool _isInitialized = false;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

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
                _semaphore.Wait();
                try
                {
                    if (!_isInitialized)
                    {
                        InitializeDatabase();
                        _isInitialized = true;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
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
    public async Task<List<Habit>> GetHabitsForDateAsync(DateTime date)
    {
        try
        {
            var allHabits = await GetHabitsAsync();
            var habitsForDate = new List<Habit>();

            // Получаем ID привычек, которые исключены на эту дату
            var excludedHabitIds = await GetExcludedHabitIdsForDate(date);

            foreach (var habit in allHabits)
            {
                // Пропускаем исключенные привычки
                if (excludedHabitIds.Contains(habit.Id))
                    continue;

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
    public async Task<bool> RemoveHabitFromDayAsync(int habitId, DateTime date)
    {
        try
        {
            // Добавляем исключение для этой привычки на эту дату
            await AddHabitExclusionAsync(habitId, date);

            // Удаляем все выполнения привычки на эту дату
            await _database.ExecuteAsync(
                "DELETE FROM HabitCompletion WHERE HabitId = ? AND Date = ?",
                habitId, date.Date);

            // Обновляем статистику дня
            await UpdateDailyStatsAsync(date);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing habit from day: {ex.Message}");
            return false;
        }
    }

    // Удалить базовую привычку из базового списка (для будущих дней)
    public async Task<bool> RemoveBaseHabitFromFutureAsync(int habitId)
    {
        try
        {
            var habit = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h => h.Id == habitId && h.IsActive && h.IsBaseHabit);

            if (habit == null)
                return false;

            // Помечаем привычку как не базовую и устанавливаем дату деактивации
            habit.IsBaseHabit = false;
            habit.DeactivatedDate = DateTime.Today;
            await _database.UpdateAsync(habit);

            // Удаляем все будущие выполнения этой привычки
            await _database.ExecuteAsync(
                "DELETE FROM HabitCompletion WHERE HabitId = ? AND Date > ?",
                habitId, DateTime.Today);

            // Удаляем все исключения для будущих дат
            await _database.ExecuteAsync(
                "DELETE FROM HabitExclusion WHERE HabitId = ? AND ExclusionDate > ?",
                habitId, DateTime.Today);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing base habit from future: {ex.Message}");
            return false;
        }
    }

    // Методы для исключений привычек (чтобы базовые привычки не показывались в определенные дни)

    private async Task AddHabitExclusionAsync(int habitId, DateTime date)
    {
        try
        {
            // Создаем таблицу для исключений если её нет
            await _database.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS HabitExclusion (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HabitId INTEGER NOT NULL,
                    ExclusionDate DATE NOT NULL,
                    UNIQUE(HabitId, ExclusionDate)
                )");

            // Добавляем исключение
            await _database.ExecuteAsync(
                "INSERT OR IGNORE INTO HabitExclusion (HabitId, ExclusionDate) VALUES (?, ?)",
                habitId, date.Date);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding habit exclusion: {ex.Message}");
        }
    }

    private async Task<HashSet<int>> GetExcludedHabitIdsForDate(DateTime date)
    {
        try
        {
            var exclusions = await _database.QueryAsync<HabitExclusion>(
                "SELECT * FROM HabitExclusion WHERE ExclusionDate = ?",
                date.Date);

            return new HashSet<int>(exclusions.Select(e => e.HabitId));
        }
        catch (Exception ex)
        {
            // Если таблицы нет, возвращаем пустой список
            Debug.WriteLine($"Error getting excluded habits: {ex.Message}");
            return new HashSet<int>();
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

    // Метод для проверки существования привычки по имени сегодня (только небазовые)
    public async Task<bool> HabitExistsForTodayAsync(string habitName)
    {
        try
        {
            var existing = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h =>
                    h.Name.ToLower() == habitName.ToLower() &&
                    h.IsActive &&
                    !h.IsBaseHabit &&
                    h.CreatedDate.Date == DateTime.Today.Date);

            return existing != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking if habit exists for today: {ex.Message}");
            return false;
        }
    }

    // Модель для исключений
    private class HabitExclusion
    {
        public int Id { get; set; }
        public int HabitId { get; set; }
        public DateTime ExclusionDate { get; set; }
    }
}
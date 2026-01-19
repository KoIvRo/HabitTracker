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

    /// <summary>
    /// Инициализирует базу данных, создавая необходимые таблицы если они не существуют.
    /// Не удаляет существующие данные при повторной инициализации.
    /// </summary>
    private async void InitializeDatabase()
    {
        try
        {
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

    /// <summary>
    /// Получает список активных привычек, отсортированных по имени.
    /// </summary>
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

    /// <summary>
    /// Получает список привычек для указанной даты, учитывая базовые привычки и исключения.
    /// Базовые привычки показываются если не были деактивированы до этой даты.
    /// Небазовые привычки показываются только если созданы в указанный день.
    /// </summary>
    /// <param name="date">Дата для которой нужно получить привычки</param>
    public async Task<List<Habit>> GetHabitsForDateAsync(DateTime date)
    {
        try
        {
            var allHabits = await GetHabitsAsync();
            var habitsForDate = new List<Habit>();
            var excludedHabitIds = await GetExcludedHabitIdsForDate(date);

            foreach (var habit in allHabits)
            {
                if (excludedHabitIds.Contains(habit.Id) || habit.CreatedDate.Date > date.Date)
                    continue;

                if (habit.IsBaseHabit)
                {
                    if (!habit.DeactivatedDate.HasValue || habit.DeactivatedDate.Value.Date > date.Date)
                    {
                        habitsForDate.Add(habit);
                    }
                }
                else if (habit.CreatedDate.Date == date.Date)
                {
                    habitsForDate.Add(habit);
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

    /// <summary>
    /// Получает список активных базовых привычек, отсортированных по имени.
    /// Базовые привычки автоматически добавляются во все будущие дни.
    /// </summary>
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

    /// <summary>
    /// Добавляет новую привычку в базу данных.
    /// </summary>
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

    /// <summary>
    /// Полностью удаляет небазовую привычку или деактивирует базовую.
    /// Для базовых привычек: помечает как неактивную, сохраняя исторические данные.
    /// Для небазовых: полностью удаляет из базы данных.
    /// </summary>
    /// <param name="id">ID привычки для удаления</param>
    public async Task<int> DeleteHabitAsync(int id)
    {
        try
        {
            var habit = await _database.Table<Habit>().FirstOrDefaultAsync(h => h.Id == id);
            if (habit == null) return 0;

            if (!habit.IsBaseHabit)
            {
                await _database.ExecuteAsync("DELETE FROM HabitCompletion WHERE HabitId = ?", habit.Id);
                return await _database.DeleteAsync(habit);
            }
            else
            {
                habit.IsActive = false;
                return await _database.UpdateAsync(habit);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting habit: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Удаляет привычку только из указанного дня, добавляя её в исключения для этой даты.
    /// Сохраняет привычку в базе для других дней.
    /// </summary>
    /// <param name="habitId">ID привычки</param>
    /// <param name="date">Дата из которой нужно удалить привычку</param>
    public async Task<bool> RemoveHabitFromDayAsync(int habitId, DateTime date)
    {
        try
        {
            await AddHabitExclusionAsync(habitId, date);
            await _database.ExecuteAsync(
                "DELETE FROM HabitCompletion WHERE HabitId = ? AND Date = ?",
                habitId, date.Date);
            await UpdateDailyStatsAsync(date);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing habit from day: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Деактивирует базовую привычку для будущих дней, сохраняя её в истории.
    /// Удаляет все выполнения и исключения для будущих дат.
    /// </summary>
    /// <param name="habitId">ID базовой привычки</param>
    public async Task<bool> RemoveBaseHabitFromFutureAsync(int habitId)
    {
        try
        {
            var habit = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h => h.Id == habitId && h.IsActive && h.IsBaseHabit);

            if (habit == null) return false;

            habit.IsBaseHabit = false;
            habit.DeactivatedDate = DateTime.Today;
            await _database.UpdateAsync(habit);

            await _database.ExecuteAsync(
                "DELETE FROM HabitCompletion WHERE HabitId = ? AND Date > ?",
                habitId, DateTime.Today);

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

    /// <summary>
    /// Получает ежедневную запись с настроением и статистикой для указанной даты.
    /// </summary>
    /// <param name="date">Дата для получения записи</param>
    public async Task<DailyRecord> GetDailyRecordAsync(DateTime date)
    {
        try
        {
            return await _database.Table<DailyRecord>()
                .FirstOrDefaultAsync(d => d.Date == date.Date);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting daily record: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Сохраняет или обновляет ежедневную запись с настроением и статистикой привычек.
    /// </summary>
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

    /// <summary>
    /// Проверяет статус выполнения привычки для указанной даты.
    /// </summary>
    /// <param name="habitId">ID привычки</param>
    /// <param name="date">Дата для проверки</param>
    public async Task<bool> GetHabitCompletionStatusAsync(int habitId, DateTime date)
    {
        try
        {
            var completion = await _database.Table<HabitCompletion>()
                .FirstOrDefaultAsync(hc => hc.HabitId == habitId && hc.Date == date.Date);
            return completion?.IsCompleted ?? false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting completion status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Устанавливает статус выполнения привычки для указанной даты.
    /// Автоматически обновляет статистику дня.
    /// </summary>
    /// <param name="habitId">ID привычки</param>
    /// <param name="date">Дата выполнения</param>
    /// <param name="isCompleted">Статус выполнения</param>
    public async Task SetHabitCompletionAsync(int habitId, DateTime date, bool isCompleted)
    {
        try
        {
            var completion = await _database.Table<HabitCompletion>()
                .FirstOrDefaultAsync(hc => hc.HabitId == habitId && hc.Date == date.Date);

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

    /// <summary>
    /// Проверяет существует ли привычка с указанным именем.
    /// </summary>
    /// <param name="habitName">Имя привычки для проверки</param>
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

    /// <summary>
    /// Проверяет существует ли небазовая привычка с указанным именем для сегодняшнего дня.
    /// </summary>
    /// <param name="habitName">Имя привычки для проверки</param>
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

    /// <summary>
    /// Получает словарь ежедневных записей за указанный период для отображения в календаре.
    /// </summary>
    /// <param name="startDate">Начальная дата периода</param>
    /// <param name="endDate">Конечная дата периода</param>
    public async Task<Dictionary<DateTime, DailyRecord>> GetRecordsDictionaryAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var records = await _database.Table<DailyRecord>()
                .Where(d => d.Date >= startDate.Date && d.Date <= endDate.Date)
                .ToListAsync();
            return records.ToDictionary(r => r.Date, r => r);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting records dictionary: {ex.Message}");
            return new Dictionary<DateTime, DailyRecord>();
        }
    }

    /// <summary>
    /// Обновляет статистику дня: количество выполненных и общих привычек.
    /// Вызывается автоматически при изменении статуса выполнения привычки.
    /// </summary>
    private async Task UpdateDailyStatsAsync(DateTime date)
    {
        try
        {
            var habits = await GetHabitsForDateAsync(date);
            var completedCount = habits.Count(h => GetHabitCompletionStatusAsync(h.Id, date).Result);

            var record = await GetDailyRecordAsync(date) ?? new DailyRecord { Date = date.Date };
            record.CompletedHabits = completedCount;
            record.TotalHabits = habits.Count;

            await SaveDailyRecordAsync(record);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating daily stats: {ex.Message}");
        }
    }

    /// <summary>
    /// Добавляет исключение для привычки на указанную дату.
    /// Исключенные привычки не показываются в указанный день.
    /// </summary>
    private async Task AddHabitExclusionAsync(int habitId, DateTime date)
    {
        try
        {
            await _database.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS HabitExclusion (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HabitId INTEGER NOT NULL,
                    ExclusionDate DATE NOT NULL,
                    UNIQUE(HabitId, ExclusionDate)
                )");

            await _database.ExecuteAsync(
                "INSERT OR IGNORE INTO HabitExclusion (HabitId, ExclusionDate) VALUES (?, ?)",
                habitId, date.Date);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding habit exclusion: {ex.Message}");
        }
    }

    /// <summary>
    /// Получает список ID привычек, исключенных для указанной даты.
    /// </summary>
    private async Task<HashSet<int>> GetExcludedHabitIdsForDate(DateTime date)
    {
        try
        {
            var exclusions = await _database.QueryAsync<HabitExclusion>(
                "SELECT * FROM HabitExclusion WHERE ExclusionDate = ?", date.Date);
            return new HashSet<int>(exclusions.Select(e => e.HabitId));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting excluded habits: {ex.Message}");
            return new HashSet<int>();
        }
    }

    /// <summary>
    /// Вспомогательный класс для хранения исключений привычек.
    /// Определяет дни, когда конкретные привычки не должны показываться.
    /// </summary>
    private class HabitExclusion
    {
        public int Id { get; set; }
        public int HabitId { get; set; }
        public DateTime ExclusionDate { get; set; }
    }
}
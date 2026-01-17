// Database/DatabaseContext.cs
using SQLite;
using HabitTracker.Models;
using System.Diagnostics;

namespace HabitTracker.Database;

public class DatabaseContext
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseContext()
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "habittracker.db3");
            Debug.WriteLine($"Database path: {dbPath}");

            _database = new SQLiteAsyncConnection(dbPath);
            InitializeDatabase();
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
            await _database.CreateTableAsync<Habit>();
            await _database.CreateTableAsync<DailyRecord>();
            await _database.CreateTableAsync<HabitCompletion>();
            Debug.WriteLine("Database tables created successfully");
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
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting habits: {ex.Message}");
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

    public async Task<int> DeleteHabitAsync(int id)
    {
        try
        {
            var habit = await _database.Table<Habit>()
                .FirstOrDefaultAsync(h => h.Id == id);
            if (habit != null)
            {
                habit.IsActive = false;
                return await _database.UpdateAsync(habit);
            }
            return 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting habit: {ex.Message}");
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
                record.Date = existing.Date;
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
            var habits = await GetHabitsAsync();
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

    // Методы для календаря
    public async Task<List<DailyRecord>> GetMonthlyRecordsAsync(int year, int month)
    {
        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return await _database.Table<DailyRecord>()
                .Where(d => d.Date >= startDate && d.Date <= endDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting monthly records: {ex.Message}");
            return new List<DailyRecord>();
        }
    }

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
}
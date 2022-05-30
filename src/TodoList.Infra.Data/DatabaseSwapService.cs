using Microsoft.Data.Sqlite;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data;

public class DatabaseSwapService : IDatabaseSwapService
{
    public void DoSwap(string sourceFilename, string destFilename)
    {
        using var sourceDatabase = new SqliteConnection($"Data Source={sourceFilename}");
        using var targetDatabase = new SqliteConnection($"Data Source={destFilename}");

        sourceDatabase.Open();
        targetDatabase.Open();

        sourceDatabase.BackupDatabase(targetDatabase);

        targetDatabase.Close();
        sourceDatabase.Close();
    }
}
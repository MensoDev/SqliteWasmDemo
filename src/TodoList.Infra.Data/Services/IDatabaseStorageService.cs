namespace TodoList.Infra.Data.Services;

public interface IDatabaseStorageService
{
    Task<int> SyncDatabaseAsync(string filename);
    Task<string> GenerateDownloadLinkAsync(string filename);
}
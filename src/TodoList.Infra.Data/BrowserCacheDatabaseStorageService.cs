using Microsoft.JSInterop;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data;

public class BrowserCacheDatabaseStorageService : IDatabaseStorageService, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    
    public BrowserCacheDatabaseStorageService(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", $"./_content/Todo.Infra.Data/browserCacheDatabaseStorageService.js" ).AsTask()
        );
    }
    
    public async Task<int> SyncDatabaseAsync(string filename)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<int>("syncDatabaseWithStorageAsync", filename);
    }

    public async Task<string> GenerateDownloadLinkAsync(string filename)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("generateDownloadLinkAsync", filename);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
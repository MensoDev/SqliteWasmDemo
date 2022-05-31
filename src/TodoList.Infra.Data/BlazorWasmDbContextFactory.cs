using Microsoft.EntityFrameworkCore;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data;

public class BlazorWasmDbContextFactory<TContext> : IBlazorWasmDbContextFactory<TContext>
    where TContext : DbContext
{
    private static readonly IDictionary<Type, string> FileNames = new Dictionary<Type, string>();
    
    private readonly IDbContextFactory<TContext> _dbContextFactory;
    private readonly IDatabaseStorageService _dbStorageService;
    private readonly IDatabaseSwapService _dbSwapService;
    private Task<int>? _startupTask;
    private int _lastStatus = -2;
    private bool _init;

    public BlazorWasmDbContextFactory(
        IDbContextFactory<TContext> dbContextFactory, 
        IDatabaseStorageService dbStorageService, 
        IDatabaseSwapService dbSwapService)
    {
        _dbContextFactory = dbContextFactory;
        _dbStorageService = dbStorageService;
        _dbSwapService = dbSwapService;
        _startupTask = RestoreAsync();
    }
    
    private static string Filename => FileNames[typeof(TContext)];
    private static string BackupFile => $"{Filename}_backup";

    public async Task<TContext> CreateDbContextAsync()
    {
        // Quanto for executado pela primeira vez deve esperar a restauração acontecer.
        await CheckForStartupTaskAsync();

        // Aqui pegamos o contexto do banco de dados.
        var dbContext = await _dbContextFactory.CreateDbContextAsync();

        if (!_init)
        {
            // quando executado pela primeira vez, devemos criar o banco de dados.
            await dbContext.Database.EnsureCreatedAsync();
            _init = true;
        }

        // Aqui vamos monitorar sempre que o saved changes for chamado sincronizar e fechar a conexão com o banco de dados.
        dbContext.SavedChanges += (_, e) => DbContextSavedChanges(dbContext, e);

        return dbContext;
    }
    
    public static string? GetFilenameForType() =>
        FileNames.ContainsKey(typeof(TContext)) ? FileNames[typeof(TContext)] : null;

    private void DoSwap(string source, string target) =>
        _dbSwapService.DoSwap(source, target);

    private string GetFilename()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var filename = "filenotfound.db";
        var type = dbContext.GetType();
        if (FileNames.ContainsKey(type))
        {
            return FileNames[type];
        }

        var connectionString = dbContext.Database.GetConnectionString();

        var file = connectionString
            ?.Split(';')
            .Select(s => s.Split('='))
            .Select(split => new
            {
                key = split[0].ToLowerInvariant(),
                value = split[1],
            })
            .Where(kv => kv.key.Contains("data source") ||
                         kv.key.Contains("datasource") ||
                         kv.key.Contains("filename")
            )
            .Select(kv => kv.value)
            .FirstOrDefault();
        
        if (file is not null)
        {
            filename = file;
        }

        FileNames.Add(type, filename);
        return filename;
    }

    private async Task CheckForStartupTaskAsync()
    {
        if (_startupTask is not null)
        {
            _lastStatus = await _startupTask;
            _startupTask.Dispose();
            _startupTask = null;
        }
    }

    private async void DbContextSavedChanges(TContext ctx, SavedChangesEventArgs e)
    {
        await ctx.Database.CloseConnectionAsync();
        await CheckForStartupTaskAsync();
        if (e.EntitiesSavedCount <= 0) return;
        
        // exclusivo para evitar conflitos. É excluído após o cache.
        var backupName = $"{BackupFile}-{Guid.NewGuid().ToString().Split('-')[0]}";
        DoSwap(Filename, backupName);
        _lastStatus = await _dbStorageService.SyncDatabaseAsync(backupName);
    }

    private async Task<int> RestoreAsync()
    {
        var filename = $"{GetFilename()}_backup";
        _lastStatus = await _dbStorageService.SyncDatabaseAsync(filename);
        if (_lastStatus is 0)
        {
            DoSwap(filename, FileNames[typeof(TContext)]);
        }

        return _lastStatus;
    }
}
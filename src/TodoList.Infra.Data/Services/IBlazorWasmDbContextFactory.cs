using Microsoft.EntityFrameworkCore;

namespace TodoList.Infra.Data.Services;

public interface IBlazorWasmDbContextFactory<TContext>
    where TContext : DbContext
{
    Task<TContext> CreateDbContextAsync();
}
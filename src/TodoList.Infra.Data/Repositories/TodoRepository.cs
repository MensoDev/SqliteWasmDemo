using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Entities;
using TodoList.Domain.Repositories;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly IBlazorWasmDbContextFactory<TodoListDbContext> _contextFactory;

    public TodoRepository(IBlazorWasmDbContextFactory<TodoListDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async ValueTask<IEnumerable<Todo>> GetAllAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Todos.ToListAsync();
    }

    public async ValueTask<Todo?> GetByIdAsync(Guid id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id);
    }

    public async ValueTask RegisterAsync(Todo todo)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Todos.Add(todo);
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask UpdateAsync(Todo todo)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Todos.Update(todo);
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask RemoveAsync(Todo todo)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Todos.Remove(todo);
        await dbContext.SaveChangesAsync();
    }
}
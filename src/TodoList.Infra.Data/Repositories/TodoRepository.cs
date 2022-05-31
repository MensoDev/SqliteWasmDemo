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

    public ValueTask<Todo> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public ValueTask RegisterAsync(Todo todo)
    {
        throw new NotImplementedException();
    }

    public void Update(Todo todo)
    {
        throw new NotImplementedException();
    }

    public void Remove(Todo todo)
    {
        throw new NotImplementedException();
    }
}
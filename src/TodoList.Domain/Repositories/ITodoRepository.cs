using TodoList.Domain.Entities;

namespace TodoList.Domain.Repositories;

public interface ITodoRepository
{
    ValueTask<IEnumerable<Todo>> GetAllAsync();
    ValueTask<Todo> GetByIdAsync(Guid id);
    
    ValueTask RegisterAsync(Todo todo);
    
    void Update(Todo todo);
    
    void Remove(Todo todo);
}
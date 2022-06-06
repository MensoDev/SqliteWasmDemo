namespace TodoList.Domain.Entities;

public class Todo
{
    public Todo(string title, string description)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Done = false;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool Done { get; private set; }
    
    public void MarkAsDone()
    {
        Done = true;
    }
    
    public void MarkAsUndone()
    {
        Done = false;
    }
}
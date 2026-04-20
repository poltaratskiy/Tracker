using Tracker.Dotnet.Tasks.Domain.Enums;

namespace Tracker.Dotnet.Tasks.Domain.Entities;

public class TaskEntity
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public TaskEntityStatus Status { get; set; } = TaskEntityStatus.Created;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
}

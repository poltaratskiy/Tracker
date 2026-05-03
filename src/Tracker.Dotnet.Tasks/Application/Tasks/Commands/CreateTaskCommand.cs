using MediatR;
using Tracker.Dotnet.Tasks.Domain.Entities;
using Tracker.Dotnet.Tasks.Persistence;

namespace Tracker.Dotnet.Tasks.Application.Tasks.Commands;

public class CreateTaskCommand : IRequest<CreatedTaskModel>
{
    public required string Title { get; set; }
    public required string Description { get; init; }
    public decimal Price { get; set; }
}

public record CreatedTaskModel(int Id);


public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, CreatedTaskModel>
{
    private readonly ILogger<CreateTaskHandler> _logger;
    private readonly ApplicationDbContext _dbContext;

    public CreateTaskHandler(ILogger<CreateTaskHandler> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<CreatedTaskModel> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = new TaskEntity
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Status = Domain.Enums.TaskEntityStatus.Created,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedTaskModel(task.Id);
    }
}

using MediatR;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Tasks.Persistence;

namespace Tracker.Dotnet.Tasks.Application.Tasks.Commands;

public class UpdateTaskCommand : IRequest
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; init; }
    public decimal Price { get; set; }
}

public class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand>
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateTaskHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync(new object?[] { request.Id }, cancellationToken);

        if (task == null)
        {
            throw new NotFoundException();
        }

        if (task.Status != Domain.Enums.TaskEntityStatus.Created)
        {
            throw new ApiException("Task must be not assigned");
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Price = request.Price;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

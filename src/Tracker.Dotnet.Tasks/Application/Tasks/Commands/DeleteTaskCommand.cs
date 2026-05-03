using MediatR;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Tasks.Persistence;

namespace Tracker.Dotnet.Tasks.Application.Tasks.Commands;

public class DeleteTaskCommand : IRequest
{
    public int Id { get; set; }
}

public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteTaskHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync(new object?[] { request.Id }, cancellationToken: cancellationToken);

        if (task == null)
        {
            throw new NotFoundException();
        }

        if (task.Status != Domain.Enums.TaskEntityStatus.Created)
        {
            throw new ApiException("Task must be not assigned");
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

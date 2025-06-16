namespace Tracker.Dotnet.Users.Interfaces
{
    public interface IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}

namespace Tracker.Dotnet.Tasks.Application.Pagination;

public record PagedResponse<T> (
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

namespace WorkFlow.Application.Common.Pagination;

public sealed record PagedData<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount);
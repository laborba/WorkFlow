using WorkFlow.Domain.Entities;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectRepository
{
    Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default);
}
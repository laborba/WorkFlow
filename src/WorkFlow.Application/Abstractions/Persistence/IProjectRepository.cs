using WorkFlow.Domain.Entities;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectRepository
{
    Task<Project?> GetByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default);
}
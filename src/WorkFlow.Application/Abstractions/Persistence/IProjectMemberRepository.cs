using WorkFlow.Domain.Entities;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectMemberRepository
{
    Task<bool> IsActiveMemberAsync(
        long projectId,
        long userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default);
}
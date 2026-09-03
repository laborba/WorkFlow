using WorkFlow.Domain.Entities;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectMemberRepository
{
    Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default);
}
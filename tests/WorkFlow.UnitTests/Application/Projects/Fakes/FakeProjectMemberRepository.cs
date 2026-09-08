using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

internal sealed class FakeProjectMemberRepository :
    IProjectMemberRepository
{
    public ProjectMember? AddedProjectMember { get; private set; }

    public bool IsActiveMemberResult { get; set; }

    public long? QueriedProjectId { get; private set; }

    public long? QueriedUserId { get; private set; }

    public Task<bool> IsActiveMemberAsync(
        long projectId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        QueriedProjectId = projectId;
        QueriedUserId = userId;

        return Task.FromResult(IsActiveMemberResult);
    }

    public Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default)
    {
        AddedProjectMember = projectMember;

        return Task.CompletedTask;
    }
}

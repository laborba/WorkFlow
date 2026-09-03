using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

internal sealed class FakeProjectMemberRepository :
    IProjectMemberRepository
{
    public ProjectMember? AddedProjectMember { get; private set; }

    public Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default)
    {
        AddedProjectMember = projectMember;

        return Task.CompletedTask;
    }
}
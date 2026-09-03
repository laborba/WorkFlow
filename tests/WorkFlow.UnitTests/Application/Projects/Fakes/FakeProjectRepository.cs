using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

internal sealed class FakeProjectRepository :
    IProjectRepository
{
    public Project? AddedProject { get; private set; }

    public Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        AddedProject = project;

        return Task.CompletedTask;
    }
}
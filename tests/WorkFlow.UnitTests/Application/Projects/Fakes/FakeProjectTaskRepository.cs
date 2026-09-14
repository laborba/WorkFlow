using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Application.Projects.Fakes;

public sealed class FakeProjectTaskRepository :
    IProjectTaskRepository
{
    public IReadOnlyCollection<ProjectTaskStatus>
        StatusesToReturn
    { get; set; } =
            Array.Empty<ProjectTaskStatus>();

    public long? CheckedProjectId { get; private set; }

    public Task<IReadOnlyCollection<ProjectTaskStatus>>
        GetStatusesByProjectIdAsync(
            long projectId,
            CancellationToken cancellationToken = default)
    {
        CheckedProjectId =
            projectId;

        return Task.FromResult(
            StatusesToReturn);
    }
}
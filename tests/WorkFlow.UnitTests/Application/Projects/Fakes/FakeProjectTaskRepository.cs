using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
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

    public ProjectTask? AddedProjectTask { get; private set; }

    public ProjectTask? ProjectTaskForUpdateToReturn { get; set; }

    public List<(long ProjectId, Guid PublicId)> GetForUpdateCalls { get; } = [];


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

    public Task<ProjectTask?> GetForUpdateByPublicIdAsync(
        long projectId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        GetForUpdateCalls.Add(
            (projectId, publicId));

        return Task.FromResult(
            ProjectTaskForUpdateToReturn);
    }

    public Task AddAsync(
        ProjectTask projectTask,
        CancellationToken cancellationToken = default)
    {
        AddedProjectTask =
            projectTask;

        return Task.CompletedTask;
    }
}
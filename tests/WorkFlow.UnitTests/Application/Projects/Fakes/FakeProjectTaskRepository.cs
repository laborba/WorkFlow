using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
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

    public PagedData<ProjectTaskListItemData> PagedDataToReturn { get; set; } =
        new PagedData<ProjectTaskListItemData>(
            Array.Empty<ProjectTaskListItemData>(),
            0);

    public List<(
        long TenantId,
        long ProjectId,
        int PageNumber,
        int PageSize,
        string? Search,
        ProjectTaskStatus? Status,
        ProjectTaskPriority? Priority,
        Guid? ResponsibleUserPublicId,
        bool? IsArchived)> GetPagedCalls
    { get; } = [];

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

    public Task<PagedData<ProjectTaskListItemData>> GetPagedAsync(
        long tenantId,
        long projectId,
        int pageNumber,
        int pageSize,
        string? search,
        ProjectTaskStatus? status,
        ProjectTaskPriority? priority,
        Guid? responsibleUserPublicId,
        bool? isArchived,
        CancellationToken cancellationToken = default)
    {
        GetPagedCalls.Add(
            (
                tenantId,
                projectId,
                pageNumber,
                pageSize,
                search,
                status,
                priority,
                responsibleUserPublicId,
                isArchived
            ));

        return Task.FromResult(
            PagedDataToReturn);
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
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectTaskRepository
{
    Task<IReadOnlyCollection<ProjectTaskStatus>>
        GetStatusesByProjectIdAsync(
            long projectId,
            CancellationToken cancellationToken = default);

    Task<PagedData<ProjectTaskListItemData>> GetPagedAsync(
        long tenantId,
        long projectId,
        int pageNumber,
        int pageSize,
        string? search,
        ProjectTaskStatus? status,
        ProjectTaskPriority? priority,
        Guid? responsibleUserPublicId,
        bool? isArchived,
        CancellationToken cancellationToken = default);

    Task<ProjectTask?> GetForUpdateByPublicIdAsync(
        long projectId,
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProjectTask projectTask,
        CancellationToken cancellationToken = default);
}
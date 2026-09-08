using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectRepository
{
    Task<Project?> GetByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<Project?> GetTrackedByPublicIdAsync(
        long tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<PagedData<ProjectListItemData>> GetPagedAsync(
        long tenantId,
        long? activeMemberUserId,
        int pageNumber,
        int pageSize,
        string? search,
        ProjectStatus? status,
        Guid? responsibleUserPublicId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default);
}
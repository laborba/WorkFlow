using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Domain.Entities;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectMemberRepository
{
    Task<bool> IsActiveMemberAsync(
        long projectId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<PagedData<ProjectMemberListItemData>> GetPagedAsync(
        long projectId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProjectMember projectMember,
        CancellationToken cancellationToken = default);
}
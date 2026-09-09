using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence;

public interface IProjectMemberPermissionRepository
{
    Task<bool> IsActivePermissionAsync(
        long projectMemberId,
        ProjectPermission permission,
        CancellationToken cancellationToken = default);

    Task<ProjectMemberPermission?> GetActiveForUpdateAsync(
        long projectMemberId,
        ProjectPermission permission,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProjectMemberPermissionListItemData>>
        GetActivePermissionsAsync(
            long projectMemberId,
            CancellationToken cancellationToken = default);

    Task AddAsync(
        ProjectMemberPermission projectMemberPermission,
        CancellationToken cancellationToken = default);
}
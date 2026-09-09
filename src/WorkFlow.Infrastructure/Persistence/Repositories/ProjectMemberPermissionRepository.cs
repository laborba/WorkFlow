using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Infrastructure.Persistence.Repositories;

public sealed class ProjectMemberPermissionRepository :
    IProjectMemberPermissionRepository
{
    private readonly WorkFlowDbContext _context;

    public ProjectMemberPermissionRepository(
        WorkFlowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsActivePermissionAsync(
        long projectMemberId,
        ProjectPermission permission,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectMemberPermissions
            .AsNoTracking()
            .AnyAsync(
                projectMemberPermission =>
                    projectMemberPermission.ProjectMemberId ==
                        projectMemberId &&
                    projectMemberPermission.Permission ==
                        permission &&
                    projectMemberPermission.RevokedAt == null,
                cancellationToken);
    }

    public async Task<ProjectMemberPermission?> GetActiveForUpdateAsync(
        long projectMemberId,
        ProjectPermission permission,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectMemberPermissions
            .SingleOrDefaultAsync(
                projectMemberPermission =>
                    projectMemberPermission.ProjectMemberId ==
                        projectMemberId &&
                    projectMemberPermission.Permission ==
                        permission &&
                    projectMemberPermission.RevokedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectMemberPermissionListItemData>>
        GetActivePermissionsAsync(
            long projectMemberId,
            CancellationToken cancellationToken = default)
    {
        return await (
                from projectMemberPermission in
                    _context.ProjectMemberPermissions.AsNoTracking()
                join grantedByUser in
                    _context.Users.AsNoTracking()
                    on projectMemberPermission.GrantedByUserId
                    equals grantedByUser.Id
                where
                    projectMemberPermission.ProjectMemberId ==
                        projectMemberId &&
                    projectMemberPermission.RevokedAt == null
                orderby
                    projectMemberPermission.Permission,
                    projectMemberPermission.Id
                select new ProjectMemberPermissionListItemData(
                    projectMemberPermission.Permission,
                    grantedByUser.PublicId,
                    projectMemberPermission.GrantedAt))
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        ProjectMemberPermission projectMemberPermission,
        CancellationToken cancellationToken = default)
    {
        await _context.ProjectMemberPermissions.AddAsync(
            projectMemberPermission,
            cancellationToken);
    }
}
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects;

internal static class ProjectStatusAuthorization
{
    public static async Task<bool> CanExecuteAsync(
        User requestedByUser,
        Project project,
        ProjectPermission requiredPermission,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository
            projectMemberPermissionRepository,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUser.Role == UserRole.TenantAdmin)
            return true;

        if (requestedByUser.Role != UserRole.ProjectManager &&
            requestedByUser.Role != UserRole.Member)
        {
            return false;
        }

        var projectMember =
            await projectMemberRepository.GetActiveAsync(
                project.Id,
                requestedByUser.Id,
                cancellationToken);

        if (projectMember is null)
            return false;

        return await projectMemberPermissionRepository
            .IsActivePermissionAsync(
                projectMember.Id,
                requiredPermission,
                cancellationToken);
    }
}
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ListProjectMemberPermissions;

public sealed class ListProjectMemberPermissionsHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;

    public ListProjectMemberPermissionsHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository
            projectMemberPermissionRepository)
    {
        _tenantRepository =
            tenantRepository;

        _userRepository =
            userRepository;

        _projectRepository =
            projectRepository;

        _projectMemberRepository =
            projectMemberRepository;

        _projectMemberPermissionRepository =
            projectMemberPermissionRepository;
    }

    public async Task<Result<ListProjectMemberPermissionsResult>>
        HandleAsync(
            ListProjectMemberPermissionsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        if (query.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(query.TenantPublicId));
        }

        if (query.ProjectPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do projeto não pode estar vazio.",
                nameof(query.ProjectPublicId));
        }

        if (query.RequestedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(query.RequestedByUserPublicId));
        }

        if (query.UserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário alvo não pode estar vazio.",
                nameof(query.UserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ListProjectMemberPermissionsResult>
                .Failure(
                    TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ListProjectMemberPermissionsResult>
                .Failure(
                    TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                query.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<ListProjectMemberPermissionsResult>
                .Failure(
                    UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<ListProjectMemberPermissionsResult>
                .Failure(
                    UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetByPublicIdAsync(
                tenant.Id,
                query.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<ListProjectMemberPermissionsResult>
                .Failure(
                    ProjectErrors.NotFound);
        }

        if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            if (requestedByUser.Role != UserRole.ProjectManager &&
                requestedByUser.Role != UserRole.Member)
            {
                return Result<ListProjectMemberPermissionsResult>
                    .Failure(
                        ProjectMemberPermissionErrors
                            .ManageNotAllowed);
            }

            var requesterProjectMember =
                await _projectMemberRepository.GetActiveAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (requesterProjectMember is null)
            {
                return Result<ListProjectMemberPermissionsResult>
                    .Failure(
                        ProjectMemberPermissionErrors
                            .ManageNotAllowed);
            }

            var canManagePermissions =
                await _projectMemberPermissionRepository
                    .IsActivePermissionAsync(
                        requesterProjectMember.Id,
                        ProjectPermission
                            .ManageProjectPermissions,
                        cancellationToken);

            if (!canManagePermissions)
            {
                return Result<ListProjectMemberPermissionsResult>
                    .Failure(
                        ProjectMemberPermissionErrors
                            .ManageNotAllowed);
            }
        }

        var targetUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                query.UserPublicId,
                cancellationToken);

        if (targetUser is null)
        {
            return Result<ListProjectMemberPermissionsResult>
                .Failure(
                    UserErrors.NotFound);
        }

        var targetProjectMember =
            await _projectMemberRepository.GetActiveAsync(
                project.Id,
                targetUser.Id,
                cancellationToken);

        if (targetProjectMember is null)
        {
            return Result<ListProjectMemberPermissionsResult>
                .Failure(
                    ProjectMemberErrors.NotActive);
        }

        var permissions =
            await _projectMemberPermissionRepository
                .GetActivePermissionsAsync(
                    targetProjectMember.Id,
                    cancellationToken);

        var permissionResults =
            permissions
                .Select(permission =>
                    new ProjectMemberPermissionListItemResult(
                        permission.Permission,
                        permission.GrantedByUserPublicId,
                        permission.GrantedAt))
                .ToArray();

        return Result<ListProjectMemberPermissionsResult>
            .Success(
                new ListProjectMemberPermissionsResult(
                    project.PublicId,
                    targetUser.PublicId,
                    permissionResults));
    }
}
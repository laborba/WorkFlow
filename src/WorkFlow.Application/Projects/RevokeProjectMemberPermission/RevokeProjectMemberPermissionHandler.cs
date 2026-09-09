using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.RevokeProjectMemberPermission;

public sealed class RevokeProjectMemberPermissionHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeProjectMemberPermissionHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository
            projectMemberPermissionRepository,
        IUnitOfWork unitOfWork)
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

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result<RevokeProjectMemberPermissionResult>>
        HandleAsync(
            RevokeProjectMemberPermissionCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (command.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));
        }

        if (command.ProjectPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do projeto não pode estar vazio.",
                nameof(command.ProjectPublicId));
        }

        if (command.RequestedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(command.RequestedByUserPublicId));
        }

        if (command.UserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário alvo não pode estar vazio.",
                nameof(command.UserPublicId));
        }

        if (!Enum.IsDefined(
                command.Permission))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command.Permission),
                command.Permission,
                "A permissão informada é inválida.");
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetByPublicIdAsync(
                tenant.Id,
                command.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    ProjectErrors.NotFound);
        }

        if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            if (requestedByUser.Role != UserRole.ProjectManager &&
                requestedByUser.Role != UserRole.Member)
            {
                return Result<RevokeProjectMemberPermissionResult>
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
                return Result<RevokeProjectMemberPermissionResult>
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
                return Result<RevokeProjectMemberPermissionResult>
                    .Failure(
                        ProjectMemberPermissionErrors
                            .ManageNotAllowed);
            }
        }

        if (project.IsArchived)
        {
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    ProjectErrors.Archived);
        }

        var targetUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.UserPublicId,
                cancellationToken);

        if (targetUser is null)
        {
            return Result<RevokeProjectMemberPermissionResult>
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
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    ProjectMemberErrors.NotActive);
        }

        var permission =
            await _projectMemberPermissionRepository
                .GetActiveForUpdateAsync(
                    targetProjectMember.Id,
                    command.Permission,
                    cancellationToken);

        if (permission is null)
        {
            return Result<RevokeProjectMemberPermissionResult>
                .Failure(
                    ProjectMemberPermissionErrors.NotActive);
        }

        permission.Revoke(
            requestedByUser.Id);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<RevokeProjectMemberPermissionResult>
            .Success(
                new RevokeProjectMemberPermissionResult(
                    project.PublicId,
                    targetUser.PublicId,
                    permission.Permission,
                    requestedByUser.PublicId,
                    permission.RevokedAt!.Value));
    }
}
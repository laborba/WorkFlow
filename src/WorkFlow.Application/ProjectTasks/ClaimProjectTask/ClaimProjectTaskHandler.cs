using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ClaimProjectTask;

public sealed class ClaimProjectTaskHandler
{
    private readonly ITenantRepository
        _tenantRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly IProjectRepository
        _projectRepository;

    private readonly IProjectMemberRepository
        _projectMemberRepository;

    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;

    private readonly IProjectTaskRepository
        _projectTaskRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public ClaimProjectTaskHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository projectMemberPermissionRepository,
        IProjectTaskRepository projectTaskRepository,
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

        _projectTaskRepository =
            projectTaskRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result<ClaimProjectTaskResult>>
        HandleAsync(
            ClaimProjectTaskCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ValidatePublicIds(
            command);

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ClaimProjectTaskResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ClaimProjectTaskResult>.Failure(
                TenantErrors.Inactive);
        }

        var claimedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.ClaimedByUserPublicId,
                cancellationToken);

        if (claimedByUser is null)
        {
            return Result<ClaimProjectTaskResult>.Failure(
                UserErrors.NotFound);
        }

        if (!claimedByUser.IsActive)
        {
            return Result<ClaimProjectTaskResult>.Failure(
                UserErrors.Inactive);
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var project =
                await _projectRepository
                    .GetForUpdateByPublicIdAsync(
                        tenant.Id,
                        command.ProjectPublicId,
                        cancellationToken);

            if (project is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectErrors.NotFound);
            }

            var projectMember =
                await _projectMemberRepository.GetActiveAsync(
                    project.Id,
                    claimedByUser.Id,
                    cancellationToken);

            if (projectMember is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectTaskErrors.ClaimNotAllowed);
            }

            var canClaim =
                await CanClaimAsync(
                    claimedByUser.Role,
                    projectMember.Id,
                    cancellationToken);

            if (!canClaim)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectTaskErrors.ClaimNotAllowed);
            }

            if (project.Status == ProjectStatus.Completed ||
                project.Status == ProjectStatus.Archived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectTaskErrors
                        .ClaimBlockedByProjectStatus);
            }

            var projectTask =
                await _projectTaskRepository
                    .GetForUpdateByPublicIdAsync(
                        project.Id,
                        command.TaskPublicId,
                        cancellationToken);

            if (projectTask is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status != ProjectTaskStatus.Backlog &&
                projectTask.Status != ProjectTaskStatus.Todo &&
                projectTask.Status != ProjectTaskStatus.Paused)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectTaskErrors
                        .ClaimBlockedByTaskStatus);
            }

            if (projectTask.ResponsibleUserId is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskResult>.Failure(
                    ProjectTaskErrors.AlreadyAssigned);
            }

            projectTask.ClaimResponsible(
                claimedByUser.Id);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response =
                new ClaimProjectTaskResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    claimedByUser.PublicId,
                    projectTask.Status,
                    projectTask.UpdatedAt);

            return Result<ClaimProjectTaskResult>.Success(
                response);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task<bool> CanClaimAsync(
        UserRole role,
        long projectMemberId,
        CancellationToken cancellationToken)
    {
        if (role == UserRole.TenantAdmin)
            return true;

        if (role != UserRole.ProjectManager &&
            role != UserRole.Member)
        {
            return false;
        }

        return await _projectMemberPermissionRepository
            .IsActivePermissionAsync(
                projectMemberId,
                ProjectPermission.ClaimTask,
                cancellationToken);
    }

    private static void ValidatePublicIds(
        ClaimProjectTaskCommand command)
    {
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

        if (command.TaskPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da tarefa não pode estar vazio.",
                nameof(command.TaskPublicId));
        }

        if (command.ClaimedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário não pode estar vazio.",
                nameof(command.ClaimedByUserPublicId));
        }
    }
}
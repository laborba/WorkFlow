using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ClaimProjectTaskValidation;

public sealed class ClaimProjectTaskValidationHandler
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

    public ClaimProjectTaskValidationHandler(
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

    public async Task<Result<ClaimProjectTaskValidationResult>>
        HandleAsync(
            ClaimProjectTaskValidationCommand command,
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
            return Result<ClaimProjectTaskValidationResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ClaimProjectTaskValidationResult>.Failure(
                TenantErrors.Inactive);
        }

        var claimedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.ClaimedByUserPublicId,
                cancellationToken);

        if (claimedByUser is null)
        {
            return Result<ClaimProjectTaskValidationResult>.Failure(
                UserErrors.NotFound);
        }

        if (!claimedByUser.IsActive)
        {
            return Result<ClaimProjectTaskValidationResult>.Failure(
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

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectErrors.NotFound);
            }

            if (project.Status != ProjectStatus.InProgress)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectTaskErrors
                        .ClaimValidationBlockedByProjectStatus);
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

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectTaskErrors.ClaimValidationNotAllowed);
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

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status != ProjectTaskStatus.Validation)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectTaskErrors
                        .ClaimValidationBlockedByTaskStatus);
            }

            if (projectTask.ResponsibleUserId is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectTaskErrors
                        .ClaimValidationRequiresResponsible);
            }

            if (projectTask.ValidatorUserId is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    ProjectTaskErrors.ValidationAlreadyClaimed);
            }

            var isSelfValidation =
                projectTask.ResponsibleUserId ==
                claimedByUser.Id;

            var canClaimValidation =
                await CanClaimValidationAsync(
                    claimedByUser,
                    project,
                    projectMember,
                    isSelfValidation,
                    cancellationToken);

            if (!canClaimValidation)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                var error =
                    isSelfValidation
                        ? ProjectTaskErrors.SelfValidationNotAllowed
                        : ProjectTaskErrors.ClaimValidationNotAllowed;

                return Result<ClaimProjectTaskValidationResult>.Failure(
                    error);
            }

            projectTask.ClaimValidation(
                claimedByUser.Id);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response =
                new ClaimProjectTaskValidationResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    claimedByUser.PublicId,
                    projectTask.Status,
                    projectTask.UpdatedAt);

            return Result<ClaimProjectTaskValidationResult>.Success(
                response);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task<bool> CanClaimValidationAsync(
        User claimedByUser,
        Project project,
        ProjectMember projectMember,
        bool isSelfValidation,
        CancellationToken cancellationToken)
    {
        if (claimedByUser.Role == UserRole.TenantAdmin)
            return true;

        if (claimedByUser.Role != UserRole.ProjectManager &&
            claimedByUser.Role != UserRole.Member)
        {
            return false;
        }

        if (isSelfValidation)
        {
            if (claimedByUser.Role == UserRole.ProjectManager &&
                project.ResponsibleUserId == claimedByUser.Id)
            {
                return true;
            }

            return await _projectMemberPermissionRepository
                .IsActivePermissionAsync(
                    projectMember.Id,
                    ProjectPermission.SelfValidateTask,
                    cancellationToken);
        }

        if (project.ResponsibleUserId == claimedByUser.Id)
            return true;

        return await _projectMemberPermissionRepository
            .IsActivePermissionAsync(
                projectMember.Id,
                ProjectPermission.ValidateTask,
                cancellationToken);
    }

    private static void ValidatePublicIds(
        ClaimProjectTaskValidationCommand command)
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
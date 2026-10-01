using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.PauseProjectTask;

public sealed class PauseProjectTaskHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectTaskRepository _projectTaskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PauseProjectTaskHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
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

        _projectTaskRepository =
            projectTaskRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result<PauseProjectTaskResult>> HandleAsync(
        PauseProjectTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ValidateCommand(
            command);

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<PauseProjectTaskResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<PauseProjectTaskResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<PauseProjectTaskResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<PauseProjectTaskResult>.Failure(
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

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectErrors.NotFound);
            }

            if (project.Status != ProjectStatus.InProgress)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectTaskErrors.PauseBlockedByProjectStatus);
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

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status != ProjectTaskStatus.Todo &&
                projectTask.Status != ProjectTaskStatus.InProgress)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectTaskErrors.PauseBlockedByTaskStatus);
            }

            if (!projectTask.ResponsibleUserId.HasValue)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectTaskErrors.PauseRequiresResponsible);
            }

            if (projectTask.ResponsibleUserId.Value !=
                requestedByUser.Id)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectTaskErrors.PauseNotAllowed);
            }

            var isActiveMember =
                await _projectMemberRepository.IsActiveMemberAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (!isActiveMember)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<PauseProjectTaskResult>.Failure(
                    ProjectTaskErrors.PauseNotAllowed);
            }

            projectTask.Pause(
                command.Reason);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var result =
                new PauseProjectTaskResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    requestedByUser.PublicId,
                    projectTask.Status,
                    projectTask.StatusBeforePause,
                    projectTask.DueDate,
                    projectTask.UpdatedAt);

            return Result<PauseProjectTaskResult>.Success(
                result);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidateCommand(
        PauseProjectTaskCommand command)
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

        if (command.RequestedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(command.RequestedByUserPublicId));
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            throw new ArgumentException(
                "O motivo da pausa não pode estar vazio.",
                nameof(command.Reason));
        }
    }
}
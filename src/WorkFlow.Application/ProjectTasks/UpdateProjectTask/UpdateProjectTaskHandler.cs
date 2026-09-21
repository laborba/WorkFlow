using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.UpdateProjectTask;

public sealed class UpdateProjectTaskHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository
        _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IProjectTaskRepository
        _projectTaskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectTaskHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository
            projectMemberPermissionRepository,
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

    public async Task<Result<UpdateProjectTaskResult>>
        HandleAsync(
            UpdateProjectTaskCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ValidateCommand(
            command);

        var normalizedDueDate =
            NormalizeDueDate(
                command.DueDate);

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<UpdateProjectTaskResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<UpdateProjectTaskResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<UpdateProjectTaskResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<UpdateProjectTaskResult>.Failure(
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

                return Result<UpdateProjectTaskResult>.Failure(
                    ProjectErrors.NotFound);
            }

            var canUpdateTask =
                await ProjectStatusAuthorization.CanExecuteAsync(
                    requestedByUser,
                    project,
                    ProjectPermission.EditTask,
                    _projectMemberRepository,
                    _projectMemberPermissionRepository,
                    cancellationToken);

            if (!canUpdateTask)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<UpdateProjectTaskResult>.Failure(
                    ProjectTaskErrors.UpdateNotAllowed);
            }

            if (project.Status == ProjectStatus.Archived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<UpdateProjectTaskResult>.Failure(
                    ProjectTaskErrors.UpdateBlockedByProjectStatus);
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

                return Result<UpdateProjectTaskResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<UpdateProjectTaskResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            projectTask.ChangeTitle(
                command.Title);

            projectTask.UpdateDescription(
                command.Description);

            projectTask.ChangePriority(
                command.Priority);

            projectTask.ChangeDueDate(
                normalizedDueDate);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response =
                new UpdateProjectTaskResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    projectTask.Title,
                    projectTask.Description,
                    projectTask.Status,
                    projectTask.Priority,
                    projectTask.DueDate,
                    projectTask.UpdatedAt);

            return Result<UpdateProjectTaskResult>.Success(
                response);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidateCommand(
        UpdateProjectTaskCommand command)
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

        if (string.IsNullOrWhiteSpace(
                command.Title))
        {
            throw new ArgumentException(
                "O título da tarefa não pode estar vazio.",
                nameof(command.Title));
        }

        if (!Enum.IsDefined(
                typeof(ProjectTaskPriority),
                command.Priority))
        {
            throw new ArgumentException(
                "A prioridade da tarefa é inválida.",
                nameof(command.Priority));
        }
    }

    private static DateTime? NormalizeDueDate(
        DateTime? dueDate)
    {
        if (!dueDate.HasValue)
            return null;

        return dueDate.Value.Kind switch
        {
            DateTimeKind.Utc =>
                dueDate.Value,

            DateTimeKind.Local =>
                dueDate.Value.ToUniversalTime(),

            DateTimeKind.Unspecified =>
                throw new ArgumentException(
                    "O prazo da tarefa deve informar UTC ou um fuso horário.",
                    nameof(UpdateProjectTaskCommand.DueDate)),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(dueDate))
        };
    }
}
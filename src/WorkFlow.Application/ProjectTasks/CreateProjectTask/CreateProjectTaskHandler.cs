using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.CreateProjectTask;

public sealed class CreateProjectTaskHandler
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

    public CreateProjectTaskHandler(
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

    public async Task<Result<CreateProjectTaskResult>>
        HandleAsync(
            CreateProjectTaskCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ValidatePublicIds(
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
            return Result<CreateProjectTaskResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<CreateProjectTaskResult>.Failure(
                TenantErrors.Inactive);
        }

        var createdByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.CreatedByUserPublicId,
                cancellationToken);

        if (createdByUser is null)
        {
            return Result<CreateProjectTaskResult>.Failure(
                UserErrors.NotFound);
        }

        if (!createdByUser.IsActive)
        {
            return Result<CreateProjectTaskResult>.Failure(
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

                return Result<CreateProjectTaskResult>.Failure(
                    ProjectErrors.NotFound);
            }

            var canCreateTask =
                await ProjectStatusAuthorization.CanExecuteAsync(
                    createdByUser,
                    project,
                    ProjectPermission.CreateTask,
                    _projectMemberRepository,
                    _projectMemberPermissionRepository,
                    cancellationToken);

            if (!canCreateTask)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<CreateProjectTaskResult>.Failure(
                    ProjectTaskErrors.CreationNotAllowed);
            }

            if (project.Status == ProjectStatus.Completed ||
                project.Status == ProjectStatus.Archived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<CreateProjectTaskResult>.Failure(
                    ProjectTaskErrors
                        .CreationBlockedByProjectStatus);
            }

            var projectTask =
                new ProjectTask(
                    project.Id,
                    command.Title,
                    command.Priority,
                    createdByUser.Id,
                    command.Description,
                    dueDate: normalizedDueDate);

            await _projectTaskRepository.AddAsync(
                projectTask,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response =
                new CreateProjectTaskResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    createdByUser.PublicId,
                    null,
                    projectTask.Title,
                    projectTask.Description,
                    projectTask.Status,
                    projectTask.Priority,
                    projectTask.DueDate,
                    projectTask.CreatedAt);

            return Result<CreateProjectTaskResult>.Success(
                response);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidatePublicIds(
        CreateProjectTaskCommand command)
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

        if (command.CreatedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário criador não pode estar vazio.",
                nameof(command.CreatedByUserPublicId));
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
                    nameof(CreateProjectTaskCommand.DueDate)),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(dueDate))
        };
    }
}
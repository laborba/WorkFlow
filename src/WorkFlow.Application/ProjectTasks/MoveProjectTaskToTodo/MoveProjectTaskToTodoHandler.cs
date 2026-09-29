using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;

public sealed class MoveProjectTaskToTodoHandler
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

    public MoveProjectTaskToTodoHandler(
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

    public async Task<Result<MoveProjectTaskToTodoResult>>
        HandleAsync(
            MoveProjectTaskToTodoCommand command,
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
            return Result<MoveProjectTaskToTodoResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<MoveProjectTaskToTodoResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<MoveProjectTaskToTodoResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<MoveProjectTaskToTodoResult>.Failure(
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

                return Result<MoveProjectTaskToTodoResult>.Failure(
                    ProjectErrors.NotFound);
            }

            var canMoveToTodo =
                await ProjectStatusAuthorization.CanExecuteAsync(
                    requestedByUser,
                    project,
                    ProjectPermission.EditTask,
                    _projectMemberRepository,
                    _projectMemberPermissionRepository,
                    cancellationToken);

            if (!canMoveToTodo)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<MoveProjectTaskToTodoResult>.Failure(
                    ProjectTaskErrors.MoveToTodoNotAllowed);
            }

            if (project.Status is
                ProjectStatus.Completed or
                ProjectStatus.Archived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<MoveProjectTaskToTodoResult>.Failure(
                    ProjectTaskErrors.MoveToTodoBlockedByProjectStatus);
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

                return Result<MoveProjectTaskToTodoResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<MoveProjectTaskToTodoResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status !=
                ProjectTaskStatus.Backlog)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<MoveProjectTaskToTodoResult>.Failure(
                    ProjectTaskErrors.MoveToTodoBlockedByTaskStatus);
            }

            projectTask.MoveToTodo();

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response =
                new MoveProjectTaskToTodoResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    projectTask.Status,
                    projectTask.UpdatedAt);

            return Result<MoveProjectTaskToTodoResult>.Success(
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
        MoveProjectTaskToTodoCommand command)
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
    }
}
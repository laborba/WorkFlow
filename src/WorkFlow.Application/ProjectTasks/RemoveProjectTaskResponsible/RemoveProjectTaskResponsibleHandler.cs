using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;

public sealed class RemoveProjectTaskResponsibleHandler
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

    public RemoveProjectTaskResponsibleHandler(
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

    public async Task<Result<RemoveProjectTaskResponsibleResult>>
        HandleAsync(
            RemoveProjectTaskResponsibleCommand command,
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
            return Result<RemoveProjectTaskResponsibleResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<RemoveProjectTaskResponsibleResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<RemoveProjectTaskResponsibleResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<RemoveProjectTaskResponsibleResult>.Failure(
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

                return Result<RemoveProjectTaskResponsibleResult>.Failure(
                    ProjectErrors.NotFound);
            }

            var canRemoveResponsible =
                await ProjectStatusAuthorization.CanExecuteAsync(
                    requestedByUser,
                    project,
                    ProjectPermission.AssignTask,
                    _projectMemberRepository,
                    _projectMemberPermissionRepository,
                    cancellationToken);

            if (!canRemoveResponsible)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<RemoveProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.ResponsibleRemovalNotAllowed);
            }

            if (project.Status == ProjectStatus.Completed ||
                project.Status == ProjectStatus.Archived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<RemoveProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors
                        .ResponsibleRemovalBlockedByProjectStatus);
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

                return Result<RemoveProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<RemoveProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            if (projectTask.Status ==
                ProjectTaskStatus.Validation)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<RemoveProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors
                        .ResponsibleRemovalBlockedByTaskStatus);
            }

            projectTask.RemoveResponsible();

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var result =
                new RemoveProjectTaskResponsibleResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    null,
                    projectTask.Status,
                    projectTask.UpdatedAt);

            return Result<RemoveProjectTaskResponsibleResult>.Success(
                result);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidatePublicIds(
        RemoveProjectTaskResponsibleCommand command)
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
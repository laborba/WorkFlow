using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.AssignProjectTaskResponsible;

public sealed class AssignProjectTaskResponsibleHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IProjectTaskRepository _projectTaskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignProjectTaskResponsibleHandler(
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

    public async Task<Result<AssignProjectTaskResponsibleResult>>
        HandleAsync(
            AssignProjectTaskResponsibleCommand command,
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
            return Result<AssignProjectTaskResponsibleResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<AssignProjectTaskResponsibleResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<AssignProjectTaskResponsibleResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<AssignProjectTaskResponsibleResult>.Failure(
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

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectErrors.NotFound);
            }

            var canAssignTask =
                await ProjectStatusAuthorization.CanExecuteAsync(
                    requestedByUser,
                    project,
                    ProjectPermission.AssignTask,
                    _projectMemberRepository,
                    _projectMemberPermissionRepository,
                    cancellationToken);

            if (!canAssignTask)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.AssignmentNotAllowed);
            }

            if (project.Status == ProjectStatus.Completed ||
                project.Status == ProjectStatus.Archived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors
                        .AssignmentBlockedByProjectStatus);
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

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.NotFound);
            }

            if (projectTask.IsArchived)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.Archived);
            }

            var responsibleUser =
                await _userRepository.GetByPublicIdAsync(
                    tenant.Id,
                    command.ResponsibleUserPublicId,
                    cancellationToken);

            if (responsibleUser is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.ResponsibleUserNotFound);
            }

            if (!responsibleUser.IsActive)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors.ResponsibleUserInactive);
            }

            var responsibleMembership =
                await _projectMemberRepository.GetActiveAsync(
                    project.Id,
                    responsibleUser.Id,
                    cancellationToken);

            if (responsibleMembership is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<AssignProjectTaskResponsibleResult>.Failure(
                    ProjectTaskErrors
                        .ResponsibleUserNotActiveMember);
            }

            projectTask.AssignResponsible(
                responsibleUser.Id);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return Result<AssignProjectTaskResponsibleResult>.Success(
                new AssignProjectTaskResponsibleResult(
                    projectTask.PublicId,
                    tenant.PublicId,
                    project.PublicId,
                    responsibleUser.PublicId,
                    projectTask.Status,
                    projectTask.UpdatedAt));
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidatePublicIds(
        AssignProjectTaskResponsibleCommand command)
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

        if (command.ResponsibleUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do responsável não pode estar vazio.",
                nameof(command.ResponsibleUserPublicId));
        }
    }
}
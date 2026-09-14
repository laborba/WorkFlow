using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.CompleteProject;

public sealed class CompleteProjectHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IProjectTaskRepository _projectTaskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteProjectHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository
            projectMemberPermissionRepository,
        IProjectTaskRepository projectTaskRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _projectMemberPermissionRepository =
            projectMemberPermissionRepository;
        _projectTaskRepository = projectTaskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectStatusResult>> HandleAsync(
        CompleteProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ValidatePublicIds(command);

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
            return Result<ProjectStatusResult>.Failure(
                TenantErrors.NotFound);

        if (!tenant.IsActive)
            return Result<ProjectStatusResult>.Failure(
                TenantErrors.Inactive);

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
            return Result<ProjectStatusResult>.Failure(
                UserErrors.NotFound);

        if (!requestedByUser.IsActive)
            return Result<ProjectStatusResult>.Failure(
                UserErrors.Inactive);

        var project =
            await _projectRepository.GetTrackedByPublicIdAsync(
                tenant.Id,
                command.ProjectPublicId,
                cancellationToken);

        if (project is null)
            return Result<ProjectStatusResult>.Failure(
                ProjectErrors.NotFound);

        var canExecute =
            await ProjectStatusAuthorization.CanExecuteAsync(
                requestedByUser,
                project,
                ProjectPermission.CompleteProject,
                _projectMemberRepository,
                _projectMemberPermissionRepository,
                cancellationToken);

        if (!canExecute)
            return Result<ProjectStatusResult>.Failure(
                ProjectErrors.StatusChangeNotAllowed);

        if (project.Status != ProjectStatus.InProgress)
            return Result<ProjectStatusResult>.Failure(
                ProjectErrors.InvalidStatusTransition);

        var taskStatuses =
            await _projectTaskRepository.GetStatusesByProjectIdAsync(
                project.Id,
                cancellationToken);

        if (taskStatuses.Count == 0)
            return Result<ProjectStatusResult>.Failure(
                ProjectErrors.HasNoTasks);

        var hasOpenTasks =
            taskStatuses.Any(status =>
                status != ProjectTaskStatus.Done &&
                status != ProjectTaskStatus.Cancelled);

        if (hasOpenTasks)
            return Result<ProjectStatusResult>.Failure(
                ProjectErrors.HasOpenTasks);

        project.Complete(
            taskStatuses);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ProjectStatusResult>.Success(
            CreateResult(
                project,
                tenant.PublicId));
    }

    private static void ValidatePublicIds(
        CompleteProjectCommand command)
    {
        if (command.TenantPublicId == Guid.Empty)
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));

        if (command.ProjectPublicId == Guid.Empty)
            throw new ArgumentException(
                "O identificador público do projeto não pode estar vazio.",
                nameof(command.ProjectPublicId));

        if (command.RequestedByUserPublicId == Guid.Empty)
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(command.RequestedByUserPublicId));
    }

    private static ProjectStatusResult CreateResult(
        Domain.Entities.Project project,
        Guid tenantPublicId)
    {
        return new ProjectStatusResult(
            project.PublicId,
            tenantPublicId,
            project.Status,
            project.DueDate,
            project.UpdatedAt,
            project.ArchivedAt);
    }
}
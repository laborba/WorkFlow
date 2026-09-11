using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.StartProject;

public sealed class StartProjectHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartProjectHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository
            projectMemberPermissionRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _projectMemberPermissionRepository =
            projectMemberPermissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectStatusResult>> HandleAsync(
        StartProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ValidatePublicIds(
            command.TenantPublicId,
            command.ProjectPublicId,
            command.RequestedByUserPublicId);

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
                ProjectPermission.EditProject,
                _projectMemberRepository,
                _projectMemberPermissionRepository,
                cancellationToken);

        if (!canExecute)
            return Result<ProjectStatusResult>.Failure(
                ProjectErrors.StatusChangeNotAllowed);

        if (project.Status != ProjectStatus.Planning)
            return Result<ProjectStatusResult>.Failure(
                ProjectErrors.InvalidStatusTransition);

        project.Start();

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ProjectStatusResult>.Success(
            CreateResult(
                project.PublicId,
                tenant.PublicId,
                project.Status,
                project.DueDate,
                project.UpdatedAt,
                project.ArchivedAt));
    }

    private static void ValidatePublicIds(
        Guid tenantPublicId,
        Guid projectPublicId,
        Guid requestedByUserPublicId)
    {
        if (tenantPublicId == Guid.Empty)
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(tenantPublicId));

        if (projectPublicId == Guid.Empty)
            throw new ArgumentException(
                "O identificador público do projeto não pode estar vazio.",
                nameof(projectPublicId));

        if (requestedByUserPublicId == Guid.Empty)
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(requestedByUserPublicId));
    }

    private static ProjectStatusResult CreateResult(
        Guid publicId,
        Guid tenantPublicId,
        ProjectStatus status,
        DateTime? dueDate,
        DateTime? updatedAt,
        DateTime? archivedAt)
    {
        return new ProjectStatusResult(
            publicId,
            tenantPublicId,
            status,
            dueDate,
            updatedAt,
            archivedAt);
    }
}
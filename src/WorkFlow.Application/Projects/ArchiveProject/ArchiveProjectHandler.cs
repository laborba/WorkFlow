using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ArchiveProject;

public sealed class ArchiveProjectHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveProjectHandler(
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
        ArchiveProjectCommand command,
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

                return Result<ProjectStatusResult>.Failure(
                    ProjectErrors.NotFound);
            }

            var canExecute =
                await ProjectStatusAuthorization.CanExecuteAsync(
                    requestedByUser,
                    project,
                    ProjectPermission.ArchiveProject,
                    _projectMemberRepository,
                    _projectMemberPermissionRepository,
                    cancellationToken);

            if (!canExecute)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ProjectStatusResult>.Failure(
                    ProjectErrors.StatusChangeNotAllowed);
            }

            if (project.Status != ProjectStatus.Planning &&
                project.Status != ProjectStatus.Completed)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return Result<ProjectStatusResult>.Failure(
                    ProjectErrors.InvalidStatusTransition);
            }

            project.Archive();

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return Result<ProjectStatusResult>.Success(
                new ProjectStatusResult(
                    project.PublicId,
                    tenant.PublicId,
                    project.Status,
                    project.DueDate,
                    project.UpdatedAt,
                    project.ArchivedAt));
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidatePublicIds(
        ArchiveProjectCommand command)
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
}
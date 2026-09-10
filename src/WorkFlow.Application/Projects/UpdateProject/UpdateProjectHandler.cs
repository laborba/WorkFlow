using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.UpdateProject;

public sealed class UpdateProjectHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IProjectMemberPermissionRepository
            projectMemberPermissionRepository,
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

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result<UpdateProjectResult>> HandleAsync(
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

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

        if (command.RequestedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(command.RequestedByUserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<UpdateProjectResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<UpdateProjectResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<UpdateProjectResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<UpdateProjectResult>.Failure(
                UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetTrackedByPublicIdAsync(
                tenant.Id,
                command.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<UpdateProjectResult>.Failure(
                ProjectErrors.NotFound);
        }

        if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            if (requestedByUser.Role != UserRole.ProjectManager &&
                requestedByUser.Role != UserRole.Member)
            {
                return Result<UpdateProjectResult>.Failure(
                    ProjectErrors.UpdateNotAllowed);
            }

            var requesterProjectMember =
                await _projectMemberRepository.GetActiveAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (requesterProjectMember is null)
            {
                return Result<UpdateProjectResult>.Failure(
                    ProjectErrors.UpdateNotAllowed);
            }

            var canEditProject =
                await _projectMemberPermissionRepository
                    .IsActivePermissionAsync(
                        requesterProjectMember.Id,
                        ProjectPermission.EditProject,
                        cancellationToken);

            if (!canEditProject)
            {
                return Result<UpdateProjectResult>.Failure(
                    ProjectErrors.UpdateNotAllowed);
            }
        }

        if (project.IsArchived)
        {
            return Result<UpdateProjectResult>.Failure(
                ProjectErrors.Archived);
        }

        var createdByUser =
            await _userRepository.GetByIdAsync(
                tenant.Id,
                project.CreatedByUserId,
                cancellationToken);

        if (createdByUser is null)
        {
            throw new InvalidOperationException(
                "O usuário criador associado ao projeto não foi encontrado.");
        }

        Guid? responsibleUserPublicId =
            null;

        if (project.ResponsibleUserId.HasValue)
        {
            var responsibleUser =
                await _userRepository.GetByIdAsync(
                    tenant.Id,
                    project.ResponsibleUserId.Value,
                    cancellationToken);

            if (responsibleUser is null)
            {
                throw new InvalidOperationException(
                    "O usuário responsável associado ao projeto não foi encontrado.");
            }

            responsibleUserPublicId =
                responsibleUser.PublicId;
        }

        project.Rename(
            command.Name);

        project.UpdateDescription(
            command.Description);

        project.ChangeDueDate(
            command.DueDate);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var response =
            new UpdateProjectResult(
                project.PublicId,
                tenant.PublicId,
                createdByUser.PublicId,
                responsibleUserPublicId,
                project.Name,
                project.Description,
                project.Status,
                project.DueDate,
                project.CreatedAt,
                project.UpdatedAt,
                project.ArchivedAt);

        return Result<UpdateProjectResult>.Success(
            response);
    }
}
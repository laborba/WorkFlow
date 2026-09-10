using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.CreateProject;

public sealed class CreateProjectHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectHandler(
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

    public async Task<Result<CreateProjectResult>> HandleAsync(
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (command.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));
        }

        if (command.CreatedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId do usuário criador não pode estar vazio.",
                nameof(command.CreatedByUserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<CreateProjectResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<CreateProjectResult>.Failure(
                TenantErrors.Inactive);
        }

        var createdByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.CreatedByUserPublicId,
                cancellationToken);

        if (createdByUser is null)
        {
            return Result<CreateProjectResult>.Failure(
                UserErrors.NotFound);
        }

        if (!createdByUser.IsActive)
        {
            return Result<CreateProjectResult>.Failure(
                UserErrors.Inactive);
        }

        if (createdByUser.Role != UserRole.TenantAdmin &&
            createdByUser.Role != UserRole.ProjectManager)
        {
            return Result<CreateProjectResult>.Failure(
                ProjectErrors.CreationNotAllowed);
        }

        var project =
            new Project(
                tenant.Id,
                command.Name,
                createdByUser.Id,
                command.Description,
                dueDate: command.DueDate);

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await _projectRepository.AddAsync(
                project,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            var creatorMember =
                new ProjectMember(
                    project.Id,
                    createdByUser.Id,
                    createdByUser.Id);

            await _projectMemberRepository.AddAsync(
                creatorMember,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            if (createdByUser.Role ==
                UserRole.ProjectManager)
            {
                var defaultPermissions =
                    new[]
                    {
                        ProjectPermission.EditProject,
                        ProjectPermission.ManageProjectMembers,
                        ProjectPermission.ManageProjectPermissions
                    };

                foreach (var permission in defaultPermissions)
                {
                    var projectMemberPermission =
                        new ProjectMemberPermission(
                            creatorMember.Id,
                            permission,
                            createdByUser.Id);

                    await _projectMemberPermissionRepository.AddAsync(
                        projectMemberPermission,
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }

        var response =
            new CreateProjectResult(
                project.PublicId,
                tenant.PublicId,
                createdByUser.PublicId,
                project.Name,
                project.Description,
                project.Status,
                project.DueDate,
                project.CreatedAt);

        return Result<CreateProjectResult>.Success(
            response);
    }
}
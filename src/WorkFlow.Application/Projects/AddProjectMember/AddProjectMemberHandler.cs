using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.AddProjectMember;

public sealed class AddProjectMemberHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectMemberPermissionRepository
        _projectMemberPermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddProjectMemberHandler(
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

    public async Task<Result<AddProjectMemberResult>> HandleAsync(
        AddProjectMemberCommand command,
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

        if (command.UserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário a ser adicionado não pode estar vazio.",
                nameof(command.UserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<AddProjectMemberResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<AddProjectMemberResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<AddProjectMemberResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<AddProjectMemberResult>.Failure(
                UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetByPublicIdAsync(
                tenant.Id,
                command.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<AddProjectMemberResult>.Failure(
                ProjectErrors.NotFound);
        }

        if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            if (requestedByUser.Role != UserRole.ProjectManager &&
                requestedByUser.Role != UserRole.Member)
            {
                return Result<AddProjectMemberResult>.Failure(
                    ProjectMemberErrors.AddNotAllowed);
            }

            var requesterProjectMember =
                await _projectMemberRepository.GetActiveAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (requesterProjectMember is null)
            {
                return Result<AddProjectMemberResult>.Failure(
                    ProjectMemberErrors.AddNotAllowed);
            }

            var canManageMembers =
                await _projectMemberPermissionRepository
                    .IsActivePermissionAsync(
                        requesterProjectMember.Id,
                        ProjectPermission.ManageProjectMembers,
                        cancellationToken);

            if (!canManageMembers)
            {
                return Result<AddProjectMemberResult>.Failure(
                    ProjectMemberErrors.AddNotAllowed);
            }
        }

        if (project.IsArchived)
        {
            return Result<AddProjectMemberResult>.Failure(
                ProjectErrors.Archived);
        }

        var userToAdd =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.UserPublicId,
                cancellationToken);

        if (userToAdd is null)
        {
            return Result<AddProjectMemberResult>.Failure(
                UserErrors.NotFound);
        }

        if (!userToAdd.IsActive)
        {
            return Result<AddProjectMemberResult>.Failure(
                UserErrors.Inactive);
        }

        var alreadyActiveMember =
            await _projectMemberRepository.IsActiveMemberAsync(
                project.Id,
                userToAdd.Id,
                cancellationToken);

        if (alreadyActiveMember)
        {
            return Result<AddProjectMemberResult>.Failure(
                ProjectMemberErrors.AlreadyActive);
        }

        var projectMember =
            new ProjectMember(
                project.Id,
                userToAdd.Id,
                requestedByUser.Id);

        await _projectMemberRepository.AddAsync(
            projectMember,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var response =
            new AddProjectMemberResult(
                project.PublicId,
                userToAdd.PublicId,
                requestedByUser.PublicId,
                projectMember.AddedAt);

        return Result<AddProjectMemberResult>.Success(
            response);
    }
}
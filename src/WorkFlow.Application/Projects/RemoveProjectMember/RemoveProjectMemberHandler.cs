using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.RemoveProjectMember;

public sealed class RemoveProjectMemberHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveProjectMemberHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RemoveProjectMemberResult>> HandleAsync(
        RemoveProjectMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

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
                "O identificador público do usuário a ser removido não pode estar vazio.",
                nameof(command.UserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetByPublicIdAsync(
                tenant.Id,
                command.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                ProjectErrors.NotFound);
        }

        if (requestedByUser.Role == UserRole.ProjectManager)
        {
            var isActiveMember =
                await _projectMemberRepository.IsActiveMemberAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (!isActiveMember)
            {
                return Result<RemoveProjectMemberResult>.Failure(
                    ProjectMemberErrors.RemoveNotAllowed);
            }
        }
        else if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                ProjectMemberErrors.RemoveNotAllowed);
        }

        if (project.IsArchived)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                ProjectErrors.Archived);
        }

        var userToRemove =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                command.UserPublicId,
                cancellationToken);

        if (userToRemove is null)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                UserErrors.NotFound);
        }

        var projectMember =
            await _projectMemberRepository.GetActiveForUpdateAsync(
                project.Id,
                userToRemove.Id,
                cancellationToken);

        if (projectMember is null)
        {
            return Result<RemoveProjectMemberResult>.Failure(
                ProjectMemberErrors.NotActive);
        }

        projectMember.Remove();

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<RemoveProjectMemberResult>.Success(
            new RemoveProjectMemberResult(
                project.PublicId,
                userToRemove.PublicId,
                projectMember.RemovedAt!.Value));
    }
}
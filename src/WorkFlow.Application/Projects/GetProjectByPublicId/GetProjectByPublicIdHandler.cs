using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.GetProjectByPublicId;

public sealed class GetProjectByPublicIdHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;

    public GetProjectByPublicIdHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IProjectMemberRepository projectMemberRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
    }

    public async Task<Result<GetProjectByPublicIdResult>> HandleAsync(
        GetProjectByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(query.TenantPublicId));
        }

        if (query.ProjectPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do projeto não pode estar vazio.",
                nameof(query.ProjectPublicId));
        }

        if (query.RequestedByUserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário solicitante não pode estar vazio.",
                nameof(query.RequestedByUserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<GetProjectByPublicIdResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<GetProjectByPublicIdResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                query.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<GetProjectByPublicIdResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<GetProjectByPublicIdResult>.Failure(
                UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetByPublicIdAsync(
                tenant.Id,
                query.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<GetProjectByPublicIdResult>.Failure(
                ProjectErrors.NotFound);
        }

        if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            if (requestedByUser.Role != UserRole.ProjectManager &&
                requestedByUser.Role != UserRole.Member)
            {
                return Result<GetProjectByPublicIdResult>.Failure(
                    ProjectErrors.ViewNotAllowed);
            }

            var isActiveMember =
                await _projectMemberRepository.IsActiveMemberAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (!isActiveMember)
            {
                return Result<GetProjectByPublicIdResult>.Failure(
                    ProjectErrors.ViewNotAllowed);
            }
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

        Guid? responsibleUserPublicId = null;

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

        return Result<GetProjectByPublicIdResult>.Success(
            new GetProjectByPublicIdResult(
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
                project.ArchivedAt));
    }
}
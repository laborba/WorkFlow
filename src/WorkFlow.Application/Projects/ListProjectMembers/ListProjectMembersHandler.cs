using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ListProjectMembers;

public sealed class ListProjectMembersHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;

    public ListProjectMembersHandler(
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

    public async Task<Result<ListProjectMembersResult>> HandleAsync(
        ListProjectMembersQuery query,
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

        if (query.PageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageNumber),
                query.PageNumber,
                "O número da página deve ser maior ou igual a 1.");
        }

        if (query.PageSize < 1 ||
            query.PageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                query.PageSize,
                "O tamanho da página deve estar entre 1 e 100.");
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ListProjectMembersResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ListProjectMembersResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                query.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<ListProjectMembersResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<ListProjectMembersResult>.Failure(
                UserErrors.Inactive);
        }

        var project =
            await _projectRepository.GetByPublicIdAsync(
                tenant.Id,
                query.ProjectPublicId,
                cancellationToken);

        if (project is null)
        {
            return Result<ListProjectMembersResult>.Failure(
                ProjectErrors.NotFound);
        }

        if (requestedByUser.Role != UserRole.TenantAdmin)
        {
            if (requestedByUser.Role != UserRole.ProjectManager &&
                requestedByUser.Role != UserRole.Member)
            {
                return Result<ListProjectMembersResult>.Failure(
                    ProjectErrors.ViewNotAllowed);
            }

            var isActiveMember =
                await _projectMemberRepository.IsActiveMemberAsync(
                    project.Id,
                    requestedByUser.Id,
                    cancellationToken);

            if (!isActiveMember)
            {
                return Result<ListProjectMembersResult>.Failure(
                    ProjectErrors.ViewNotAllowed);
            }
        }

        var pagedData =
            await _projectMemberRepository.GetPagedAsync(
                project.Id,
                query.PageNumber,
                query.PageSize,
                query.Search,
                cancellationToken);

        var items =
            pagedData.Items
                .Select(item =>
                    new ProjectMemberListItemResult(
                        item.UserPublicId,
                        item.Name,
                        item.Email,
                        item.Role,
                        item.AddedAt,
                        item.AddedByUserPublicId))
                .ToArray();

        var totalPages =
            pagedData.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    pagedData.TotalCount /
                    (double)query.PageSize);

        return Result<ListProjectMembersResult>.Success(
            new ListProjectMembersResult(
                items,
                query.PageNumber,
                query.PageSize,
                pagedData.TotalCount,
                totalPages));
    }
}
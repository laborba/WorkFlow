using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ListProjects;

public sealed class ListProjectsHandler
{
    private const int MaximumPageSize = 100;

    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;

    public ListProjectsHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
    }

    public async Task<Result<ListProjectsResult>> HandleAsync(
        ListProjectsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(query.TenantPublicId));
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
            query.PageSize > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                query.PageSize,
                $"O tamanho da página deve estar entre 1 e {MaximumPageSize}.");
        }

        if (query.Status.HasValue &&
            !Enum.IsDefined(query.Status.Value))
        {
            throw new ArgumentException(
                "O status do projeto é inválido.",
                nameof(query.Status));
        }

        if (query.ResponsibleUserPublicId.HasValue &&
            query.ResponsibleUserPublicId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do responsável não pode estar vazio.",
                nameof(query.ResponsibleUserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ListProjectsResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ListProjectsResult>.Failure(
                TenantErrors.Inactive);
        }

        var requestedByUser =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                query.RequestedByUserPublicId,
                cancellationToken);

        if (requestedByUser is null)
        {
            return Result<ListProjectsResult>.Failure(
                UserErrors.NotFound);
        }

        if (!requestedByUser.IsActive)
        {
            return Result<ListProjectsResult>.Failure(
                UserErrors.Inactive);
        }

        long? activeMemberUserId =
            requestedByUser.Role switch
            {
                UserRole.TenantAdmin => null,
                UserRole.ProjectManager => requestedByUser.Id,
                UserRole.Member => requestedByUser.Id,
                _ => requestedByUser.Id
            };

        var pagedData =
            await _projectRepository.GetPagedAsync(
                tenant.Id,
                activeMemberUserId,
                query.PageNumber,
                query.PageSize,
                query.Search,
                query.Status,
                query.ResponsibleUserPublicId,
                cancellationToken);

        var items =
            pagedData.Items
                .Select(project =>
                {
                    if (project.HasResponsibleUser &&
                        (!project.ResponsibleUserPublicId.HasValue ||
                         project.ResponsibleUserName is null))
                    {
                        throw new InvalidOperationException(
                            "O usuário responsável associado ao projeto não foi encontrado.");
                    }

                    return new ProjectListItemResult(
                        project.PublicId,
                        project.Name,
                        project.Description,
                        project.Status,
                        project.ResponsibleUserPublicId,
                        project.ResponsibleUserName,
                        project.DueDate,
                        project.CreatedAt,
                        project.UpdatedAt,
                        project.ArchivedAt);
                })
                .ToArray();

        var totalPages =
            (int)Math.Ceiling(
                pagedData.TotalCount /
                (double)query.PageSize);

        var response =
            new ListProjectsResult(
                items,
                query.PageNumber,
                query.PageSize,
                pagedData.TotalCount,
                totalPages);

        return Result<ListProjectsResult>.Success(
            response);
    }
}
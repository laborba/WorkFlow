using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;

namespace WorkFlow.Application.Users.ListUsers;

public sealed class ListUsersHandler
{
    private const int MaximumPageSize = 100;

    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;

    public ListUsersHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<ListUsersResult>> HandleAsync(
        ListUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(query.TenantPublicId));
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

        if (query.Role.HasValue &&
            !Enum.IsDefined(query.Role.Value))
        {
            throw new ArgumentException(
                "O perfil do usuário é inválido.",
                nameof(query.Role));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ListUsersResult>.Failure(
                TenantErrors.NotFound);
        }

        var pagedData =
            await _userRepository.GetPagedAsync(
                tenant.Id,
                query.PageNumber,
                query.PageSize,
                query.Role,
                query.IsActive,
                query.Search,
                cancellationToken);

        var items =
            pagedData.Items
                .Select(user =>
                    new UserListItemResult(
                        user.PublicId,
                        user.Name,
                        user.Email,
                        user.Role,
                        user.IsActive,
                        user.CreatedAt,
                        user.UpdatedAt))
                .ToArray();

        var totalPages =
            (int)Math.Ceiling(
                pagedData.TotalCount /
                (double)query.PageSize);

        var response =
            new ListUsersResult(
                items,
                query.PageNumber,
                query.PageSize,
                pagedData.TotalCount,
                totalPages);

        return Result<ListUsersResult>.Success(
            response);
    }
}
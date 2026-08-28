using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;

namespace WorkFlow.Application.Users.GetUserByPublicId;

public sealed class GetUserByPublicIdHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;

    public GetUserByPublicIdHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<GetUserByPublicIdResult>> HandleAsync(
        GetUserByPublicIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público da empresa não pode estar vazio.",
                nameof(query.TenantPublicId));
        }

        if (query.UserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador público do usuário não pode estar vazio.",
                nameof(query.UserPublicId));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                query.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<GetUserByPublicIdResult>.Failure(
                TenantErrors.NotFound);
        }

        var user =
            await _userRepository.GetByPublicIdAsync(
                tenant.Id,
                query.UserPublicId,
                cancellationToken);

        if (user is null)
        {
            return Result<GetUserByPublicIdResult>.Failure(
                UserErrors.NotFound);
        }

        return Result<GetUserByPublicIdResult>.Success(
            new GetUserByPublicIdResult(
                user.PublicId,
                tenant.PublicId,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt));
    }
}
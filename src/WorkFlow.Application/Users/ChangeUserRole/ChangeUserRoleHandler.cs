using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.ChangeUserRole;

public sealed class ChangeUserRoleHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserRoleHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChangeUserRoleResult>> HandleAsync(
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));
        }

        if (command.UserPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId do usuário não pode estar vazio.",
                nameof(command.UserPublicId));
        }

        if (!Enum.IsDefined(
                typeof(UserRole),
                command.Role))
        {
            throw new ArgumentException(
                "O perfil do usuário é inválido.",
                nameof(command.Role));
        }

        if (command.Role == UserRole.SystemAdmin)
        {
            return Result<ChangeUserRoleResult>.Failure(
                UserErrors.SystemAdminCannotBelongToTenant);
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<ChangeUserRoleResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<ChangeUserRoleResult>.Failure(
                TenantErrors.Inactive);
        }

        var user =
            await _userRepository.GetTrackedByPublicIdAsync(
                tenant.Id,
                command.UserPublicId,
                cancellationToken);

        if (user is null)
        {
            return Result<ChangeUserRoleResult>.Failure(
                UserErrors.NotFound);
        }

        user.ChangeRole(
            command.Role);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ChangeUserRoleResult>.Success(
            new ChangeUserRoleResult(
                user.PublicId,
                tenant.PublicId,
                user.Role,
                user.UpdatedAt));
    }
}
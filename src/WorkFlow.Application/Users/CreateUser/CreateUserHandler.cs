using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Security;
using WorkFlow.Application.Common.Security;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.CreateUser;

public sealed class CreateUserHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateUserResult>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));
        }

        PasswordPolicy.Validate(
            command.Password,
            nameof(command.Password));

        if (command.Role == UserRole.SystemAdmin)
        {
            return Result<CreateUserResult>.Failure(
                UserErrors.SystemAdminCannotBelongToTenant);
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId,
                cancellationToken);

        if (tenant is null)
        {
            return Result<CreateUserResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<CreateUserResult>.Failure(
                TenantErrors.Inactive);
        }

        var emailAlreadyExists =
            await _userRepository.ExistsByEmailAsync(
                tenant.Id,
                command.Email,
                cancellationToken);

        if (emailAlreadyExists)
        {
            return Result<CreateUserResult>.Failure(
                UserErrors.EmailAlreadyExists);
        }

        var passwordHash =
            _passwordHasher.Hash(
                command.Password);

        var user = new User(
            tenant.Id,
            command.Name,
            command.Email,
            passwordHash,
            command.Role);

        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var response =
            new CreateUserResult(
                user.PublicId,
                tenant.PublicId,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt);

        return Result<CreateUserResult>.Success(
            response);
    }
}
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Abstractions.Security;
using WorkFlow.Application.Common.Results;
using WorkFlow.Application.Tenants;

namespace WorkFlow.Application.Authentication.Login;

public sealed class LoginHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public LoginHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TenantPublicId.HasValue &&
            command.TenantPublicId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId da empresa não pode estar vazio.",
                nameof(command.TenantPublicId));
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new ArgumentException(
                "O e-mail não pode estar vazio.",
                nameof(command.Email));
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ArgumentException(
                "A senha não pode estar vazia.",
                nameof(command.Password));
        }

        if (!command.TenantPublicId.HasValue)
        {
            var systemAdmin =
                await _userRepository.GetSystemAdminByEmailAsync(
                    command.Email,
                    cancellationToken);

            if (systemAdmin is null)
            {
                return Result<LoginResult>.Failure(
                    AuthenticationErrors.InvalidCredentials);
            }

            var passwordIsValid =
                _passwordHasher.Verify(
                    command.Password,
                    systemAdmin.PasswordHash);

            if (!passwordIsValid)
            {
                return Result<LoginResult>.Failure(
                    AuthenticationErrors.InvalidCredentials);
            }

            if (!systemAdmin.IsActive)
            {
                return Result<LoginResult>.Failure(
                    AuthenticationErrors.UserInactive);
            }

            var token =
                _accessTokenGenerator.Generate(
                    systemAdmin,
                    null);

            return Result<LoginResult>.Success(
                new LoginResult(
                    token.AccessToken,
                    token.ExpiresAt,
                    systemAdmin.PublicId,
                    null,
                    systemAdmin.Name,
                    systemAdmin.Email,
                    systemAdmin.Role));
        }

        var tenant =
            await _tenantRepository.GetByPublicIdAsync(
                command.TenantPublicId.Value,
                cancellationToken);

        if (tenant is null)
        {
            return Result<LoginResult>.Failure(
                TenantErrors.NotFound);
        }

        if (!tenant.IsActive)
        {
            return Result<LoginResult>.Failure(
                TenantErrors.Inactive);
        }

        var user =
            await _userRepository.GetByEmailAsync(
                tenant.Id,
                command.Email,
                cancellationToken);

        if (user is null)
        {
            return Result<LoginResult>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        var userPasswordIsValid =
            _passwordHasher.Verify(
                command.Password,
                user.PasswordHash);

        if (!userPasswordIsValid)
        {
            return Result<LoginResult>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result<LoginResult>.Failure(
                AuthenticationErrors.UserInactive);
        }

        var userToken =
            _accessTokenGenerator.Generate(
                user,
                tenant.PublicId);

        return Result<LoginResult>.Success(
            new LoginResult(
                userToken.AccessToken,
                userToken.ExpiresAt,
                user.PublicId,
                tenant.PublicId,
                user.Name,
                user.Email,
                user.Role));
    }
}
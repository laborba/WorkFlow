using WorkFlow.Domain.Enums;
using WorkFlow.Domain.Common;

namespace WorkFlow.Domain.Entities;

public class User
{
    public long Id { get; private set; }

    public Guid PublicId { get; private set; }

    public long? TenantId { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string NormalizedEmail { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public User(
        long? tenantId,
        string name,
        string email,
        string passwordHash,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome do usuário não pode estar vazio.",
                nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "O e-mail não pode estar vazio.",
                nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "O hash da senha não pode estar vazio.",
                nameof(passwordHash));

        if (!Enum.IsDefined(typeof(UserRole), role))
            throw new ArgumentException(
                "O perfil do usuário é inválido.",
                nameof(role));

        if (tenantId.HasValue && tenantId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tenantId),
                "O TenantId deve ser maior que zero " +
                "quando informado.");
        }

        if (role == UserRole.SystemAdmin &&
            tenantId is not null)
        {
            throw new ArgumentException(
                "Um SystemAdmin não pode pertencer " +
                "a um Tenant.",
                nameof(tenantId));
        }

        if (role != UserRole.SystemAdmin &&
            tenantId is null)
        {
            throw new ArgumentException(
                "Um Tenant é obrigatório para este " +
                "perfil de usuário.",
                nameof(tenantId));
        }

        TenantId = tenantId;
        PublicId = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim();
        NormalizedEmail = EmailNormalizer.Normalize(email);
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome do usuário não pode estar vazio.",
                nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "O e-mail não pode estar vazio.",
                nameof(email));

        Email = email.Trim();
        NormalizedEmail = EmailNormalizer.Normalize(email);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "O hash da senha não pode estar vazio.",
                nameof(passwordHash));

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole role)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
            throw new ArgumentException(
                "O perfil do usuário é inválido.",
                nameof(role));

        if (role == UserRole.SystemAdmin &&
            TenantId is not null)
        {
            throw new ArgumentException(
                "Um usuário vinculado a um Tenant não " +
                "pode assumir o perfil SystemAdmin.",
                nameof(role));
        }

        if (role != UserRole.SystemAdmin &&
            TenantId is null)
        {
            throw new ArgumentException(
                "Um Tenant é obrigatório para este " +
                "perfil de usuário.",
                nameof(role));
        }

        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }
}
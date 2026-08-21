using WorkFlow.Domain.Enums;

namespace WorkFlow.Domain.Entities;

public class ProjectMemberPermission
{
    public long Id { get; private set; }

    public long ProjectMemberId { get; private set; }

    public ProjectPermission Permission { get; private set; }

    public DateTime GrantedAt { get; private set; }

    public long GrantedByUserId { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public long? RevokedByUserId { get; private set; }

    public bool IsActive => RevokedAt is null;

    public ProjectMemberPermission(
        long projectMemberId,
        ProjectPermission permission,
        long grantedByUserId)
    {
        if (projectMemberId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectMemberId),
                "O identificador do membro deve ser maior que zero.");
        }

        if (!Enum.IsDefined(permission))
        {
            throw new ArgumentOutOfRangeException(
                nameof(permission),
                "A permissão informada é inválida.");
        }

        if (grantedByUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(grantedByUserId),
                "O identificador do usuário que concedeu a permissão deve ser maior que zero.");
        }

        ProjectMemberId = projectMemberId;
        Permission = permission;
        GrantedByUserId = grantedByUserId;
        GrantedAt = DateTime.UtcNow;
    }

    public void Revoke(long revokedByUserId)
    {
        if (revokedByUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revokedByUserId),
                "O identificador do usuário que revogou a permissão deve ser maior que zero.");
        }

        if (!IsActive)
        {
            throw new InvalidOperationException(
                "A permissão já foi revogada.");
        }

        RevokedByUserId = revokedByUserId;
        RevokedAt = DateTime.UtcNow;
    }
}
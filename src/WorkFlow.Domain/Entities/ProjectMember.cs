namespace WorkFlow.Domain.Entities;

public class ProjectMember
{
    public long Id { get; private set; }

    public long ProjectId { get; private set; }

    public long UserId { get; private set; }

    public DateTime AddedAt { get; private set; }

    public long AddedByUserId { get; private set; }

    public DateTime? RemovedAt { get; private set; }

    public bool IsActive => RemovedAt is null;

    public ProjectMember(
        long projectId,
        long userId,
        long addedByUserId)
    {
        if (projectId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(projectId),
                "O identificador do projeto deve ser maior que zero.");

        if (userId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "O identificador do usuário deve ser maior que zero.");

        if (addedByUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(addedByUserId),
                "O identificador do usuário que adicionou o membro deve ser maior que zero.");

        ProjectId = projectId;
        UserId = userId;
        AddedByUserId = addedByUserId;
        AddedAt = DateTime.UtcNow;
    }

    public void Remove()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "O membro já foi removido do projeto.");

        RemovedAt = DateTime.UtcNow;
    }
}
namespace WorkFlow.Domain.Entities;

public class TaskCollaborator
{
    public long Id { get; private set; }
    public long TaskId { get; private set; }
    public long UserId { get; private set; }
    public DateTime AddedAt { get; private set; }
    public long AddedByUserId { get; private set; }
    public DateTime? RemovedAt { get; private set; }
    public long? RemovedByUserId { get; private set; }

    public bool IsActive => RemovedAt is null;

    public TaskCollaborator(
        long taskId,
        long userId,
        long addedByUserId)
    {
        if (taskId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taskId),
                "O identificador da tarefa deve ser maior que zero.");
        }

        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "O identificador do usuário deve ser maior que zero.");
        }

        if (addedByUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(addedByUserId),
                "O identificador do usuário que adicionou o colaborador deve ser maior que zero.");
        }

        TaskId = taskId;
        UserId = userId;
        AddedByUserId = addedByUserId;
        AddedAt = DateTime.UtcNow;
    }

    public void Remove(long removedByUserId)
    {
        if (removedByUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(removedByUserId),
                "O identificador do usuário que removeu o colaborador deve ser maior que zero.");
        }

        if (!IsActive)
        {
            throw new InvalidOperationException(
                "O colaborador já foi removido da tarefa.");
        }

        RemovedAt = DateTime.UtcNow;
        RemovedByUserId = removedByUserId;
    }
}
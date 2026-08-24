using WorkFlow.Domain.Enums;

namespace WorkFlow.Domain.Entities;

public class TaskHistory
{
    public long Id { get; private set; }

    public long TaskId { get; private set; }

    public long ActorUserId { get; private set; }

    public TaskHistoryAction Action { get; private set; }

    public string? Reason { get; private set; }

    public string? OldValue { get; private set; }

    public string? NewValue { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public TaskHistory(
        long taskId,
        long actorUserId,
        TaskHistoryAction action,
        string? reason = null,
        string? oldValue = null,
        string? newValue = null)
    {
        if (taskId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taskId),
                "O identificador da tarefa deve ser maior que zero.");
        }

        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actorUserId),
                "O identificador do usuário responsável pela ação deve ser maior que zero.");
        }

        if (!Enum.IsDefined(typeof(TaskHistoryAction), action))
        {
            throw new ArgumentException(
                "A ação do histórico da tarefa é inválida.",
                nameof(action));
        }

        TaskId = taskId;
        ActorUserId = actorUserId;
        Action = action;
        Reason = NormalizeOptionalText(reason);
        OldValue = NormalizeOptionalText(oldValue);
        NewValue = NormalizeOptionalText(newValue);
        CreatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
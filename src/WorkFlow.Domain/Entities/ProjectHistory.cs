using WorkFlow.Domain.Enums;

namespace WorkFlow.Domain.Entities;

public class ProjectHistory
{
    public long Id { get; private set; }

    public long ProjectId { get; private set; }

    public long ActorUserId { get; private set; }

    public ProjectHistoryAction Action { get; private set; }

    public string? Reason { get; private set; }

    public string? OldValue { get; private set; }

    public string? NewValue { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ProjectHistory(
        long projectId,
        long actorUserId,
        ProjectHistoryAction action,
        string? reason = null,
        string? oldValue = null,
        string? newValue = null)
    {
        if (projectId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectId),
                "O identificador do projeto deve ser maior que zero.");
        }

        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actorUserId),
                "O identificador do usuário responsável pela ação deve ser maior que zero.");
        }

        if (!Enum.IsDefined(
                typeof(ProjectHistoryAction),
                action))
        {
            throw new ArgumentException(
                "A ação do histórico do projeto é inválida.",
                nameof(action));
        }

        ProjectId = projectId;
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
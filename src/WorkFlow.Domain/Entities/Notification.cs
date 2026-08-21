using WorkFlow.Domain.Enums;

namespace WorkFlow.Domain.Entities;

public class Notification
{
    public static TimeSpan RetentionPeriod { get; } =
    TimeSpan.FromDays(90);
    public long Id { get; private set; }

    public long RecipientUserId { get; private set; }

    public long? ActorUserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Title { get; private set; }

    public string Message { get; private set; }

    public NotificationResourceType? ResourceType { get; private set; }

    public long? ResourceId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public DateTime? DismissedAt { get; private set; }

    public bool IsRead => ReadAt.HasValue;

    public bool IsDismissed => DismissedAt.HasValue;

    public Notification(
        long recipientUserId,
        NotificationType type,
        string title,
        string message,
        long? actorUserId = null,
        NotificationResourceType? resourceType = null,
        long? resourceId = null)
    {
        if (recipientUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recipientUserId),
                "O identificador do destinatário deve ser maior que zero.");
        }

        if (actorUserId.HasValue &&
            actorUserId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actorUserId),
                "O identificador do autor da ação deve ser maior que zero quando informado.");
        }

        if (!Enum.IsDefined(typeof(NotificationType), type))
        {
            throw new ArgumentException(
                "O tipo da notificação é inválido.",
                nameof(type));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "O título da notificação não pode estar vazio.",
                nameof(title));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "A mensagem da notificação não pode estar vazia.",
                nameof(message));
        }

        if (resourceType.HasValue &&
            !Enum.IsDefined(
                typeof(NotificationResourceType),
                resourceType.Value))
        {
            throw new ArgumentException(
                "O tipo do recurso relacionado é inválido.",
                nameof(resourceType));
        }

        if (resourceId.HasValue &&
            resourceId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resourceId),
                "O identificador do recurso deve ser maior que zero quando informado.");
        }

        if (resourceType.HasValue &&
            !resourceId.HasValue)
        {
            throw new ArgumentException(
                "O identificador do recurso deve ser informado quando o tipo do recurso for informado.",
                nameof(resourceId));
        }

        if (!resourceType.HasValue &&
            resourceId.HasValue)
        {
            throw new ArgumentException(
                "O tipo do recurso deve ser informado quando o identificador do recurso for informado.",
                nameof(resourceType));
        }

        RecipientUserId = recipientUserId;
        ActorUserId = actorUserId;
        Type = type;
        Title = title.Trim();
        Message = message.Trim();
        ResourceType = resourceType;
        ResourceId = resourceId;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsRead(
    long userId,
    DateTime readAtUtc)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "O identificador do usuário que leu a notificação deve ser maior que zero.");
        }

        if (readAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "A data da leitura deve estar em UTC.",
                nameof(readAtUtc));
        }

        if (readAtUtc < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(readAtUtc),
                "A data da leitura não pode ser anterior à criação da notificação.");
        }

        if (userId != RecipientUserId)
        {
            throw new InvalidOperationException(
                "Somente o destinatário pode marcar a notificação como lida.");
        }

        if (IsRead)
            return;

        ReadAt = readAtUtc;
    }

    public void Dismiss(
    long userId,
    DateTime dismissedAtUtc)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "O identificador do usuário que dispensou a notificação deve ser maior que zero.");
        }

        if (dismissedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "A data da dispensa deve estar em UTC.",
                nameof(dismissedAtUtc));
        }

        if (dismissedAtUtc < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dismissedAtUtc),
                "A data da dispensa não pode ser anterior à criação da notificação.");
        }

        if (userId != RecipientUserId)
        {
            throw new InvalidOperationException(
                "Somente o destinatário pode dispensar a notificação.");
        }

        if (IsDismissed)
            return;

        DismissedAt = dismissedAtUtc;
    }

    public bool IsRetentionExpiredAt(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "A data de verificação deve estar em UTC.",
                nameof(utcNow));
        }

        if (utcNow < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(utcNow),
                "A data de verificação não pode ser anterior à criação da notificação.");
        }

        return utcNow >= CreatedAt.Add(RetentionPeriod);
    }
}
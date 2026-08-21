namespace WorkFlow.Domain.Entities;

public class ChatMessage
{
    public static TimeSpan RetentionPeriod { get; } =
        TimeSpan.FromDays(7);

    public static TimeSpan EditWindow { get; } =
        TimeSpan.FromMinutes(5);

    public long Id { get; private set; }

    public long TenantId { get; private set; }

    public long AuthorUserId { get; private set; }

    public string Content { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public long? DeletedByUserId { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public ChatMessage(
        long tenantId,
        long authorUserId,
        string content)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tenantId),
                "O identificador da empresa deve ser maior que zero.");
        }

        if (authorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(authorUserId),
                "O identificador do autor deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "O conteúdo da mensagem não pode estar vazio.",
                nameof(content));
        }

        var createdAtUtc = DateTime.UtcNow;

        TenantId = tenantId;
        AuthorUserId = authorUserId;
        Content = content.Trim();
        CreatedAt = createdAtUtc;
        ExpiresAt = createdAtUtc.Add(RetentionPeriod);
    }

    public bool IsExpiredAt(DateTime utcNow)
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
                "A data de verificação não pode ser anterior à criação da mensagem.");
        }

        return utcNow >= ExpiresAt;
    }

    public void Edit(
    string content,
    long editorUserId,
    DateTime editedAtUtc)
    {
        if (editorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(editorUserId),
                "O identificador do usuário que editou a mensagem deve ser maior que zero.");
        }

        if (editedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "A data da edição deve estar em UTC.",
                nameof(editedAtUtc));
        }

        if (editedAtUtc < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(editedAtUtc),
                "A data da edição não pode ser anterior à criação da mensagem.");
        }

        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "Uma mensagem removida não pode ser editada.");
        }

        if (IsExpiredAt(editedAtUtc))
        {
            throw new InvalidOperationException(
                "Uma mensagem expirada não pode ser editada.");
        }

        if (editorUserId != AuthorUserId)
        {
            throw new InvalidOperationException(
                "Somente o autor pode editar a mensagem.");
        }

        if (editedAtUtc > CreatedAt.Add(EditWindow))
        {
            throw new InvalidOperationException(
                "O prazo de 5 minutos para editar a mensagem foi encerrado.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "O conteúdo da mensagem não pode estar vazio.",
                nameof(content));
        }

        Content = content.Trim();
        UpdatedAt = editedAtUtc;
    }

    public void Delete(
    long deletedByUserId,
    DateTime deletedAtUtc)
    {
        EnsureCanBeDeleted(
            deletedByUserId,
            deletedAtUtc);

        if (IsExpiredAt(deletedAtUtc))
        {
            throw new InvalidOperationException(
                "Uma mensagem expirada não pode ser removida pelo autor.");
        }

        if (deletedByUserId != AuthorUserId)
        {
            throw new InvalidOperationException(
                "Somente o autor pode remover a mensagem.");
        }

        if (deletedAtUtc > CreatedAt.Add(EditWindow))
        {
            throw new InvalidOperationException(
                "O prazo de 5 minutos para remover a mensagem foi encerrado.");
        }

        MarkAsDeleted(
            deletedByUserId,
            deletedAtUtc);
    }

    public void DeleteByModerator(
    long moderatorUserId,
    DateTime deletedAtUtc)
    {
        EnsureCanBeDeleted(
            moderatorUserId,
            deletedAtUtc);

        MarkAsDeleted(
            moderatorUserId,
            deletedAtUtc);
    }

    private void EnsureCanBeDeleted(
        long deletedByUserId,
        DateTime deletedAtUtc)
    {
        if (deletedByUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deletedByUserId),
                "O identificador do usuário que removeu a mensagem deve ser maior que zero.");
        }

        if (deletedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "A data da remoção deve estar em UTC.",
                nameof(deletedAtUtc));
        }

        if (deletedAtUtc < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deletedAtUtc),
                "A data da remoção não pode ser anterior à criação da mensagem.");
        }

        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "Uma mensagem removida não pode ser removida novamente.");
        }
    }

    private void MarkAsDeleted(
        long deletedByUserId,
        DateTime deletedAtUtc)
    {
        DeletedAt = deletedAtUtc;
        DeletedByUserId = deletedByUserId;
    }
}
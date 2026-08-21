namespace WorkFlow.Domain.Entities;

public class TaskComment
{
    public static TimeSpan EditWindow { get; } =
        TimeSpan.FromMinutes(15);

    public long Id { get; private set; }

    public long TaskId { get; private set; }

    public long AuthorUserId { get; private set; }

    public string Content { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public long? DeletedByUserId { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public TaskComment(
        long taskId,
        long authorUserId,
        string content)
    {
        if (taskId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taskId),
                "O identificador da tarefa deve ser maior que zero.");
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
                "O conteúdo do comentário não pode estar vazio.",
                nameof(content));
        }

        TaskId = taskId;
        AuthorUserId = authorUserId;
        Content = content.Trim();
        CreatedAt = DateTime.UtcNow;
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
                "O identificador do usuário que editou o comentário deve ser maior que zero.");
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
                "A data da edição não pode ser anterior à criação do comentário.");
        }

        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "Um comentário removido não pode ser editado.");
        }

        if (editorUserId != AuthorUserId)
        {
            throw new InvalidOperationException(
                "Somente o autor pode editar o comentário.");
        }

        if (editedAtUtc > CreatedAt.Add(EditWindow))
        {
            throw new InvalidOperationException(
                "O prazo de 15 minutos para editar o comentário foi encerrado.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "O conteúdo do comentário não pode estar vazio.",
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

        if (deletedByUserId != AuthorUserId)
        {
            throw new InvalidOperationException(
                "Somente o autor pode remover o comentário.");
        }

        if (deletedAtUtc > CreatedAt.Add(EditWindow))
        {
            throw new InvalidOperationException(
                "O prazo de 15 minutos para remover o comentário foi encerrado.");
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
                "O identificador do usuário que removeu o comentário deve ser maior que zero.");
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
                "A data da remoção não pode ser anterior à criação do comentário.");
        }

        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "Um comentário removido não pode ser removido novamente.");
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
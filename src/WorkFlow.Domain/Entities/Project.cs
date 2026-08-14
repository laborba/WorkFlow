using WorkFlow.Domain.Enums;

namespace WorkFlow.Domain.Entities;

public class Project
{
    public long Id { get; private set; }

    public Guid PublicId { get; private set; }

    public long TenantId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public long? ResponsibleUserId { get; private set; }

    public long CreatedByUserId { get; private set; }

    public ProjectStatus Status { get; private set; }

    public ProjectStatus? StatusBeforeArchive { get; private set; }

    public DateTime? DueDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? ArchivedAt { get; private set; }

    public bool IsArchived => Status == ProjectStatus.Archived;

    public Project(
        long tenantId,
        string name,
        long createdByUserId,
        string? description = null,
        long? responsibleUserId = null,
        DateTime? dueDate = null)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(tenantId),
                "O TenantId deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome do projeto não pode estar vazio.",
                nameof(name));

        if (createdByUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(createdByUserId),
                "O identificador do criador deve ser maior que zero.");

        if (responsibleUserId.HasValue && responsibleUserId.Value <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(responsibleUserId),
                "O identificador do responsável deve ser maior que zero quando informado.");

        TenantId = tenantId;
        PublicId = Guid.NewGuid();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        CreatedByUserId = createdByUserId;
        ResponsibleUserId = responsibleUserId;
        Status = ProjectStatus.Planning;
        StatusBeforeArchive = null;
        DueDate = dueDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome do projeto não pode estar vazio.",
                nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        EnsureNotArchived();

        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeDueDate(DateTime? dueDate)
    {
        EnsureNotArchived();

        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignResponsible(long responsibleUserId)
    {
        EnsureNotArchived();

        if (responsibleUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(responsibleUserId),
                "O identificador do responsável deve ser maior que zero.");

        ResponsibleUserId = responsibleUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveResponsible()
    {
        EnsureNotArchived();

        ResponsibleUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        EnsureNotArchived();

        if (Status != ProjectStatus.Planning)
            throw new InvalidOperationException(
                "Somente projetos em planejamento podem ser iniciados.");

        Status = ProjectStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pause(string reason)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "O motivo da pausa não pode estar vazio.",
                nameof(reason));

        if (Status != ProjectStatus.InProgress)
            throw new InvalidOperationException(
                "Somente projetos em andamento podem ser pausados.");

        Status = ProjectStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resume(DateTime? newDueDate = null)
    {
        EnsureNotArchived();

        if (Status != ProjectStatus.Paused)
            throw new InvalidOperationException(
                "Somente projetos pausados podem ser retomados.");

        if (newDueDate.HasValue)
            DueDate = newDueDate.Value;

        Status = ProjectStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(
    IReadOnlyCollection<ProjectTaskStatus> taskStatuses)
    {
        EnsureNotArchived();

        if (taskStatuses is null)
            throw new ArgumentNullException(nameof(taskStatuses));

        if (Status != ProjectStatus.InProgress)
            throw new InvalidOperationException(
                "Somente projetos em andamento podem ser concluídos.");

        if (taskStatuses.Count == 0)
            throw new InvalidOperationException(
                "Um projeto sem tarefas não pode ser concluído.");

        var hasOpenTask = taskStatuses.Any(status =>
            status != ProjectTaskStatus.Done &&
            status != ProjectTaskStatus.Cancelled);

        if (hasOpenTask)
            throw new InvalidOperationException(
                "O projeto possui tarefas que ainda não foram concluídas ou canceladas.");

        Status = ProjectStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen(string reason)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "O motivo da reabertura não pode estar vazio.",
                nameof(reason));

        if (Status != ProjectStatus.Completed)
            throw new InvalidOperationException(
                "Somente projetos concluídos podem ser reabertos.");

        Status = ProjectStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "O projeto já está arquivado.");

        if (Status != ProjectStatus.Planning &&
            Status != ProjectStatus.Completed)
        {
            throw new InvalidOperationException(
                "Somente projetos em planejamento ou concluídos podem ser arquivados.");
        }

        var archivedAt = DateTime.UtcNow;

        StatusBeforeArchive = Status;
        Status = ProjectStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    public void Restore()
    {
        if (!IsArchived)
            throw new InvalidOperationException(
                "O projeto não está arquivado.");

        if (StatusBeforeArchive != ProjectStatus.Planning &&
            StatusBeforeArchive != ProjectStatus.Completed)
        {
            throw new InvalidOperationException(
                "O projeto não possui um estado anterior válido para restauração.");
        }

        Status = StatusBeforeArchive.Value;
        StatusBeforeArchive = null;
        ArchivedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "Um projeto arquivado não pode ser alterado.");
    }
}
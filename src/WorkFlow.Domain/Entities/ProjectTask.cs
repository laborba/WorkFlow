using WorkFlow.Domain.Enums;

namespace WorkFlow.Domain.Entities;

public class ProjectTask
{
    public long Id { get; private set; }

    public Guid PublicId { get; private set; }

    public long ProjectId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public ProjectTaskStatus Status { get; private set; }

    public ProjectTaskPriority Priority { get; private set; }

    public long? ResponsibleUserId { get; private set; }

    public long? ValidatorUserId { get; private set; }

    public DateTime? DueDate { get; private set; }

    public long CreatedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? ArchivedAt { get; private set; }

    public ProjectTaskStatus? StatusBeforePause { get; private set; }

    public bool IsArchived => ArchivedAt.HasValue;

    public ProjectTask(
        long projectId,
        string title,
        ProjectTaskPriority priority,
        long createdByUserId,
        string? description = null,
        long? responsibleUserId = null,
        DateTime? dueDate = null)
    {
        if (projectId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(projectId),
                "O ProjectId deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "O título da tarefa não pode estar vazio.",
                nameof(title));

        if (!Enum.IsDefined(typeof(ProjectTaskPriority), priority))
            throw new ArgumentException(
                "A prioridade da tarefa é inválida.",
                nameof(priority));

        if (createdByUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(createdByUserId),
                "O identificador do criador deve ser maior que zero.");

        if (responsibleUserId.HasValue && responsibleUserId.Value <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(responsibleUserId),
                "O identificador do responsável deve ser maior que zero quando informado.");

        ProjectId = projectId;
        PublicId = Guid.NewGuid();
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        Status = ProjectTaskStatus.Backlog;
        Priority = priority;
        ResponsibleUserId = responsibleUserId;
        ValidatorUserId = null;
        DueDate = dueDate;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
        Status = ProjectTaskStatus.Backlog;
        StatusBeforePause = null;
    }

    public void ChangeTitle(string title)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "O título da tarefa não pode estar vazio.",
                nameof(title));

        Title = title.Trim();
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

    public void ChangePriority(ProjectTaskPriority priority)
    {
        EnsureNotArchived();

        if (!Enum.IsDefined(typeof(ProjectTaskPriority), priority))
            throw new ArgumentException(
                "A prioridade da tarefa é inválida.",
                nameof(priority));

        Priority = priority;
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

        if (Status == ProjectTaskStatus.Validation)
            throw new InvalidOperationException(
                "Não é possível remover o responsável enquanto a tarefa está em validação.");

        ResponsibleUserId = null;

        if (Status == ProjectTaskStatus.InProgress)
            Status = ProjectTaskStatus.Todo;

        if (Status == ProjectTaskStatus.Paused &&
            StatusBeforePause == ProjectTaskStatus.InProgress)
        {
            StatusBeforePause = ProjectTaskStatus.Todo;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToTodo()
    {
        EnsureNotArchived();

        if (Status != ProjectTaskStatus.Backlog)
            throw new InvalidOperationException(
                "Somente tarefas no backlog podem ser movidas para pendentes.");

        Status = ProjectTaskStatus.Todo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        EnsureNotArchived();

        if (Status != ProjectTaskStatus.Todo)
            throw new InvalidOperationException(
                "Somente tarefas pendentes podem ser iniciadas.");

        if (ResponsibleUserId is null)
            throw new InvalidOperationException(
                "A tarefa precisa possuir um responsável para ser iniciada.");

        Status = ProjectTaskStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SendToValidation()
    {
        EnsureNotArchived();

        if (Status != ProjectTaskStatus.InProgress)
            throw new InvalidOperationException(
                "Somente tarefas em andamento podem ser enviadas para validação.");

        if (ResponsibleUserId is null)
            throw new InvalidOperationException(
                "A tarefa precisa possuir um responsável para ser enviada à validação.");

        Status = ProjectTaskStatus.Validation;
        ValidatorUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClaimValidation(long validatorUserId)
    {
        EnsureNotArchived();

        if (validatorUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(validatorUserId),
                "O identificador do validador deve ser maior que zero.");

        if (Status != ProjectTaskStatus.Validation)
            throw new InvalidOperationException(
                "Somente tarefas em validação podem possuir um validador.");

        if (ValidatorUserId is not null)
            throw new InvalidOperationException(
                "A validação desta tarefa já possui um responsável.");

        ValidatorUserId = validatorUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseValidation(long validatorUserId)
    {
        EnsureNotArchived();

        EnsureCurrentValidator(validatorUserId);

        ValidatorUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureCurrentValidator(long validatorUserId)
    {
        EnsureNotArchived();

        if (validatorUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(validatorUserId),
                "O identificador do validador deve ser maior que zero.");

        if (Status != ProjectTaskStatus.Validation)
            throw new InvalidOperationException(
                "A tarefa não está em validação.");

        if (ValidatorUserId is null)
            throw new InvalidOperationException(
                "A validação ainda não possui responsável.");

        if (ValidatorUserId != validatorUserId)
            throw new InvalidOperationException(
                "Somente o validador atual pode realizar esta operação.");
    }

    public void ApproveValidation(long validatorUserId)
    {
        EnsureNotArchived();

        EnsureCurrentValidator(validatorUserId);

        Status = ProjectTaskStatus.Done;
        ValidatorUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RejectValidation(long validatorUserId, string reason)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "O motivo da rejeição não pode estar vazio.",
                nameof(reason));

        EnsureCurrentValidator(validatorUserId);

        Status = ProjectTaskStatus.InProgress;
        ValidatorUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pause(string reason)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "O motivo da pausa não pode estar vazio.",
                nameof(reason));

        if (Status != ProjectTaskStatus.Todo &&
            Status != ProjectTaskStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Somente tarefas pendentes ou em andamento podem ser pausadas.");
        }

        StatusBeforePause = Status;
        Status = ProjectTaskStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resume()
    {
        EnsureNotArchived();

        if (Status != ProjectTaskStatus.Paused)
            throw new InvalidOperationException(
                "Somente tarefas pausadas podem ser retomadas.");

        if (StatusBeforePause != ProjectTaskStatus.Todo &&
            StatusBeforePause != ProjectTaskStatus.InProgress)
        {
            throw new InvalidOperationException(
                "A tarefa não possui um estado anterior válido para retomada.");
        }

        if (StatusBeforePause == ProjectTaskStatus.InProgress &&
            ResponsibleUserId is null)
        {
            throw new InvalidOperationException(
                "A tarefa precisa possuir um responsável para retornar ao andamento.");
        }

        Status = StatusBeforePause.Value;
        StatusBeforePause = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        EnsureNotArchived();

        if (Status != ProjectTaskStatus.Backlog &&
            Status != ProjectTaskStatus.Todo &&
            Status != ProjectTaskStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Somente tarefas no backlog, pendentes ou em andamento podem ser canceladas.");
        }

        Status = ProjectTaskStatus.Cancelled;
        ValidatorUserId = null;
        StatusBeforePause = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen(string reason)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "O motivo da reabertura não pode estar vazio.",
                nameof(reason));

        if (Status == ProjectTaskStatus.Done)
        {
            if (ResponsibleUserId is null)
                throw new InvalidOperationException(
                    "Uma tarefa concluída precisa possuir responsável para retornar ao andamento.");

            Status = ProjectTaskStatus.InProgress;
        }
        else if (Status == ProjectTaskStatus.Cancelled)
        {
            Status = ProjectTaskStatus.Todo;
        }
        else
        {
            throw new InvalidOperationException(
                "Somente tarefas concluídas ou canceladas podem ser reabertas.");
        }

        ValidatorUserId = null;
        StatusBeforePause = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void WithdrawFromValidation(long responsibleUserId)
    {
        EnsureNotArchived();

        if (responsibleUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(responsibleUserId),
                "O identificador do responsável deve ser maior que zero.");

        if (Status != ProjectTaskStatus.Validation)
            throw new InvalidOperationException(
                "Somente tarefas em validação podem ser retiradas da validação.");

        if (ResponsibleUserId != responsibleUserId)
            throw new InvalidOperationException(
                "Somente o responsável atual pode retirar a tarefa da validação.");

        Status = ProjectTaskStatus.InProgress;
        ValidatorUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReassignValidation(long newValidatorUserId)
    {
        EnsureNotArchived();

        if (newValidatorUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(newValidatorUserId),
                "O identificador do novo validador deve ser maior que zero.");

        if (Status != ProjectTaskStatus.Validation)
            throw new InvalidOperationException(
                "A tarefa não está em validação.");

        if (ValidatorUserId is null)
            throw new InvalidOperationException(
                "A validação ainda não possui responsável.");

        ValidatorUserId = newValidatorUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "A tarefa já está arquivada.");

        if (Status != ProjectTaskStatus.Done &&
            Status != ProjectTaskStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Somente tarefas concluídas ou canceladas podem ser arquivadas.");
        }

        var archivedAt = DateTime.UtcNow;

        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    public void Restore()
    {
        if (!IsArchived)
            throw new InvalidOperationException(
                "A tarefa não está arquivada.");

        ArchivedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }




    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "Uma tarefa arquivada não pode ser alterada.");
    }
}

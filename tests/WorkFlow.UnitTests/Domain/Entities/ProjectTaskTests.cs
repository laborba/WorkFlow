using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Entities;

public class ProjectTaskTests
{
    [Fact]
    public void Constructor_ShouldCreateBacklogTask_WhenDataIsValid()
    {
        var dueDate = DateTime.UtcNow.AddDays(7);

        var task = new ProjectTask(
            projectId: 1,
            title: "  Criar autenticação  ",
            priority: ProjectTaskPriority.High,
            createdByUserId: 10,
            description: "  Implementar o login  ",
            responsibleUserId: 20,
            dueDate: dueDate);

        Assert.Equal(1, task.ProjectId);
        Assert.NotEqual(Guid.Empty, task.PublicId);
        Assert.Equal("Criar autenticação", task.Title);
        Assert.Equal("Implementar o login", task.Description);
        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Equal(ProjectTaskPriority.High, task.Priority);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.Null(task.ValidatorUserId);
        Assert.Equal(dueDate, task.DueDate);
        Assert.Equal(10, task.CreatedByUserId);
        Assert.NotEqual(default, task.CreatedAt);
        Assert.Null(task.UpdatedAt);
        Assert.Null(task.ArchivedAt);
        Assert.Null(task.StatusBeforePause);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenTitleIsInvalid(string? invalidTitle)
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectTask(
                1,
                invalidTitle!,
                ProjectTaskPriority.Medium,
                10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenProjectIdIsNotPositive(
        long invalidProjectId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectTask(
                invalidProjectId,
                "Criar autenticação",
                ProjectTaskPriority.Medium,
                10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenCreatedByUserIdIsNotPositive(
        long invalidCreatedByUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectTask(
                1,
                "Criar autenticação",
                ProjectTaskPriority.Medium,
                invalidCreatedByUserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenResponsibleUserIdIsNotPositive(
        long invalidResponsibleUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectTask(
                projectId: 1,
                title: "Criar autenticação",
                priority: ProjectTaskPriority.Medium,
                createdByUserId: 10,
                responsibleUserId: invalidResponsibleUserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_ShouldThrow_WhenPriorityIsInvalid(
        int invalidPriorityValue)
    {
        var invalidPriority =
            (ProjectTaskPriority)invalidPriorityValue;

        Assert.Throws<ArgumentException>(() =>
            new ProjectTask(
                1,
                "Criar autenticação",
                invalidPriority,
                10));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldNormalizeEmptyDescriptionToNull(
        string? description)
    {
        var task = new ProjectTask(
            projectId: 1,
            title: "Criar autenticação",
            priority: ProjectTaskPriority.Medium,
            createdByUserId: 10,
            description: description);

        Assert.Null(task.Description);
    }

    [Fact]
    public void ChangeTitle_ShouldChangeTitleAndSetUpdatedAt_WhenTitleIsValid()
    {
        var task = CreateTask();

        task.ChangeTitle("  Novo título  ");

        Assert.Equal("Novo título", task.Title);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeTitle_ShouldThrowAndPreserveTask_WhenTitleIsInvalid(
        string? invalidTitle)
    {
        var task = CreateTask();

        Assert.Throws<ArgumentException>(() =>
            task.ChangeTitle(invalidTitle!));

        Assert.Equal("Tarefa original", task.Title);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void UpdateDescription_ShouldTrimDescription_WhenDescriptionIsValid()
    {
        var task = CreateTask();

        task.UpdateDescription("  Nova descrição  ");

        Assert.Equal("Nova descrição", task.Description);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDescription_ShouldSetNull_WhenDescriptionIsEmpty(
        string? description)
    {
        var task = CreateTask(description: "Descrição original");

        task.UpdateDescription(description);

        Assert.Null(task.Description);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void ChangePriority_ShouldChangePriorityAndSetUpdatedAt()
    {
        var task = CreateTask();

        task.ChangePriority(ProjectTaskPriority.Critical);

        Assert.Equal(ProjectTaskPriority.Critical, task.Priority);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void ChangePriority_ShouldThrowAndPreserveTask_WhenPriorityIsInvalid(
        int invalidPriorityValue)
    {
        var task = CreateTask();
        var invalidPriority =
            (ProjectTaskPriority)invalidPriorityValue;

        Assert.Throws<ArgumentException>(() =>
            task.ChangePriority(invalidPriority));

        Assert.Equal(ProjectTaskPriority.Medium, task.Priority);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void ChangeDueDate_ShouldSetDueDateAndUpdatedAt()
    {
        var task = CreateTask();
        var dueDate = DateTime.UtcNow.AddDays(10);

        task.ChangeDueDate(dueDate);

        Assert.Equal(dueDate, task.DueDate);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void ChangeDueDate_ShouldAllowRemovingDueDate()
    {
        var task = CreateTask(dueDate: DateTime.UtcNow.AddDays(10));

        task.ChangeDueDate(null);

        Assert.Null(task.DueDate);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void AssignResponsible_ShouldSetResponsibleAndUpdatedAt()
    {
        var task = CreateTask();

        task.AssignResponsible(20);

        Assert.Equal(20, task.ResponsibleUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AssignResponsible_ShouldThrowAndPreserveTask_WhenIdIsInvalid(
        long invalidResponsibleUserId)
    {
        var task = CreateTask(responsibleUserId: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            task.AssignResponsible(invalidResponsibleUserId));

        Assert.Equal(20, task.ResponsibleUserId);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void RemoveResponsible_ShouldRemoveResponsibleAndSetUpdatedAt()
    {
        var task = CreateTask(responsibleUserId: 20);

        task.RemoveResponsible();

        Assert.Null(task.ResponsibleUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void MoveToTodo_ShouldChangeStatusToTodo_WhenTaskIsInBacklog()
    {
        var task = CreateTask();

        task.MoveToTodo();

        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void MoveToTodo_ShouldThrowAndPreserveTask_WhenTaskIsNotInBacklog()
    {
        var task = CreateTask();
        task.MoveToTodo();

        var updatedAtBeforeInvalidOperation = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.MoveToTodo());

        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.Equal(
            updatedAtBeforeInvalidOperation,
            task.UpdatedAt);
    }

    [Fact]
    public void Start_ShouldChangeStatusToInProgress_WhenTaskIsTodoAndHasResponsible()
    {
        var task = CreateTask(responsibleUserId: 20);
        task.MoveToTodo();

        task.Start();

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Start_ShouldThrowAndPreserveTask_WhenTaskHasNoResponsible()
    {
        var task = CreateTask();
        task.MoveToTodo();

        var updatedAtBeforeStart = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Start());

        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.Null(task.ResponsibleUserId);
        Assert.Equal(updatedAtBeforeStart, task.UpdatedAt);
    }

    [Fact]
    public void Start_ShouldThrow_WhenTaskIsNotTodo()
    {
        var task = CreateTask(responsibleUserId: 20);

        Assert.Throws<InvalidOperationException>(() =>
            task.Start());

        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void RemoveResponsible_ShouldReturnTaskToTodo_WhenTaskIsInProgress()
    {
        var task = CreateTask(responsibleUserId: 20);
        task.MoveToTodo();
        task.Start();

        task.RemoveResponsible();

        Assert.Null(task.ResponsibleUserId);
        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void SendToValidation_ShouldChangeStatusAndClearValidator()
    {
        var task = CreateInProgressTask();

        task.SendToValidation();

        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.Null(task.ValidatorUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void SendToValidation_ShouldThrow_WhenTaskIsNotInProgress()
    {
        var task = CreateTask(responsibleUserId: 20);

        Assert.Throws<InvalidOperationException>(() =>
            task.SendToValidation());

        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void ClaimValidation_ShouldAssignValidator_WhenValidationIsAvailable()
    {
        var task = CreateTaskInValidation();

        task.ClaimValidation(30);

        Assert.Equal(30, task.ValidatorUserId);
        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ClaimValidation_ShouldThrow_WhenValidatorIdIsNotPositive(
        long invalidValidatorUserId)
    {
        var task = CreateTaskInValidation();
        var updatedAtBeforeClaim = task.UpdatedAt;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            task.ClaimValidation(invalidValidatorUserId));

        Assert.Null(task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeClaim, task.UpdatedAt);
    }

    [Fact]
    public void ClaimValidation_ShouldThrow_WhenValidationAlreadyHasValidator()
    {
        var task = CreateTaskInValidation();
        task.ClaimValidation(30);

        var updatedAtBeforeSecondClaim = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.ClaimValidation(40));

        Assert.Equal(30, task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeSecondClaim, task.UpdatedAt);
    }

    [Fact]
    public void ReleaseValidation_ShouldRemoveCurrentValidator()
    {
        var task = CreateTaskInValidation();
        task.ClaimValidation(30);

        task.ReleaseValidation(30);

        Assert.Null(task.ValidatorUserId);
        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void ReleaseValidation_ShouldThrow_WhenUserIsNotCurrentValidator()
    {
        var task = CreateTaskInValidation();
        task.ClaimValidation(30);

        var updatedAtBeforeRelease = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.ReleaseValidation(40));

        Assert.Equal(30, task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeRelease, task.UpdatedAt);
    }

    [Fact]
    public void ApproveValidation_ShouldCompleteTaskAndClearValidator()
    {
        var task = CreateClaimedValidationTask(30);

        task.ApproveValidation(30);

        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.Null(task.ValidatorUserId);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void ApproveValidation_ShouldThrow_WhenUserIsNotCurrentValidator()
    {
        var task = CreateClaimedValidationTask(30);
        var updatedAtBeforeApproval = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.ApproveValidation(40));

        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.Equal(30, task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeApproval, task.UpdatedAt);
    }

    [Fact]
    public void ApproveValidation_ShouldThrow_WhenValidationHasNoValidator()
    {
        var task = CreateTaskInValidation();
        var updatedAtBeforeApproval = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.ApproveValidation(30));

        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.Null(task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeApproval, task.UpdatedAt);
    }

    [Fact]
    public void RejectValidation_ShouldReturnTaskToInProgressAndClearValidator()
    {
        var task = CreateClaimedValidationTask(30);

        task.RejectValidation(
            30,
            "É necessário corrigir os testes.");

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Null(task.ValidatorUserId);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectValidation_ShouldThrowAndPreserveTask_WhenReasonIsInvalid(
        string? invalidReason)
    {
        var task = CreateClaimedValidationTask(30);
        var updatedAtBeforeRejection = task.UpdatedAt;

        Assert.Throws<ArgumentException>(() =>
            task.RejectValidation(30, invalidReason!));

        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.Equal(30, task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeRejection, task.UpdatedAt);
    }

    [Fact]
    public void Pause_ShouldRememberTodo_WhenTaskWasTodo()
    {
        var task = CreateTask();
        task.MoveToTodo();

        task.Pause("Aguardando informações.");

        Assert.Equal(ProjectTaskStatus.Paused, task.Status);
        Assert.Equal(ProjectTaskStatus.Todo, task.StatusBeforePause);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Pause_ShouldRememberInProgress_WhenTaskWasInProgress()
    {
        var task = CreateInProgressTask();

        task.Pause("Aguardando resposta do cliente.");

        Assert.Equal(ProjectTaskStatus.Paused, task.Status);
        Assert.Equal(
            ProjectTaskStatus.InProgress,
            task.StatusBeforePause);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Pause_ShouldThrowAndPreserveTask_WhenReasonIsInvalid(
        string? invalidReason)
    {
        var task = CreateInProgressTask();
        var updatedAtBeforePause = task.UpdatedAt;

        Assert.Throws<ArgumentException>(() =>
            task.Pause(invalidReason!));

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Null(task.StatusBeforePause);
        Assert.Equal(updatedAtBeforePause, task.UpdatedAt);
    }

    [Fact]
    public void Pause_ShouldThrow_WhenTaskIsNotTodoOrInProgress()
    {
        var task = CreateTask();

        Assert.Throws<InvalidOperationException>(() =>
            task.Pause("Pausa necessária."));

        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Null(task.StatusBeforePause);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void Resume_ShouldReturnTaskToTodoAndClearPreviousStatus()
    {
        var task = CreateTask();
        task.MoveToTodo();
        task.Pause("Aguardando informações.");

        task.Resume();

        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.Null(task.StatusBeforePause);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Resume_ShouldReturnTaskToInProgressAndClearPreviousStatus()
    {
        var task = CreateInProgressTask();
        task.Pause("Aguardando cliente.");

        task.Resume();

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Null(task.StatusBeforePause);
        Assert.Equal(20, task.ResponsibleUserId);
    }

    [Fact]
    public void Resume_ShouldThrow_WhenTaskIsNotPaused()
    {
        var task = CreateTask();

        Assert.Throws<InvalidOperationException>(() =>
            task.Resume());

        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void RemoveResponsible_ShouldMakePausedTaskResumeToTodo()
    {
        var task = CreateInProgressTask();
        task.Pause("Aguardando cliente.");

        task.RemoveResponsible();
        task.Resume();

        Assert.Null(task.ResponsibleUserId);
        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.Null(task.StatusBeforePause);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    public void Cancel_ShouldChangeStatusToCancelled_WhenTransitionIsAllowed(
    ProjectTaskStatus initialStatus)
    {
        var task = CreateTaskInStatus(initialStatus);

        task.Cancel();

        Assert.Equal(ProjectTaskStatus.Cancelled, task.Status);
        Assert.Null(task.ValidatorUserId);
        Assert.Null(task.StatusBeforePause);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenTaskIsInValidation()
    {
        var task = CreateTaskInValidation();
        var updatedAtBeforeCancellation = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Cancel());

        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.Equal(
            updatedAtBeforeCancellation,
            task.UpdatedAt);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenTaskIsPaused()
    {
        var task = CreateInProgressTask();
        task.Pause("Aguardando cliente.");

        var updatedAtBeforeCancellation = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Cancel());

        Assert.Equal(ProjectTaskStatus.Paused, task.Status);
        Assert.Equal(
            ProjectTaskStatus.InProgress,
            task.StatusBeforePause);
        Assert.Equal(
            updatedAtBeforeCancellation,
            task.UpdatedAt);
    }

    [Fact]
    public void Reopen_ShouldMoveCancelledTaskToTodo()
    {
        var task = CreateTask();
        task.Cancel();

        task.Reopen("A tarefa voltou a ser necessária.");

        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.Null(task.ValidatorUserId);
        Assert.Null(task.StatusBeforePause);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Reopen_ShouldMoveDoneTaskToInProgress()
    {
        var task = CreateClaimedValidationTask(30);
        task.ApproveValidation(30);

        task.Reopen("Foram solicitados novos ajustes.");

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.Null(task.ValidatorUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reopen_ShouldThrowAndPreserveTask_WhenReasonIsInvalid(
    string? invalidReason)
    {
        var task = CreateTask();
        task.Cancel();

        var updatedAtBeforeReopening = task.UpdatedAt;

        Assert.Throws<ArgumentException>(() =>
            task.Reopen(invalidReason!));

        Assert.Equal(ProjectTaskStatus.Cancelled, task.Status);
        Assert.Equal(
            updatedAtBeforeReopening,
            task.UpdatedAt);
    }

    [Fact]
    public void Reopen_ShouldThrow_WhenTaskIsNotDoneOrCancelled()
    {
        var task = CreateTask();

        Assert.Throws<InvalidOperationException>(() =>
            task.Reopen("Tentativa de reabertura."));

        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void Reopen_ShouldThrow_WhenDoneTaskHasNoResponsible()
    {
        var task = CreateClaimedValidationTask(30);
        task.ApproveValidation(30);
        task.RemoveResponsible();

        var updatedAtBeforeReopening = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Reopen("É necessário corrigir a tarefa."));

        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.Null(task.ResponsibleUserId);
        Assert.Equal(
            updatedAtBeforeReopening,
            task.UpdatedAt);
    }

    [Fact]
    public void WithdrawFromValidation_ShouldReturnTaskToInProgressAndClearValidator()
    {
        var task = CreateClaimedValidationTask(30);

        task.WithdrawFromValidation(20);

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.Null(task.ValidatorUserId);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void WithdrawFromValidation_ShouldThrow_WhenUserIsNotResponsible()
    {
        var task = CreateClaimedValidationTask(30);
        var updatedAtBeforeWithdrawal = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.WithdrawFromValidation(40));

        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.Equal(30, task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeWithdrawal, task.UpdatedAt);
    }

    [Fact]
    public void WithdrawFromValidation_ShouldThrow_WhenTaskIsNotInValidation()
    {
        var task = CreateInProgressTask();
        var updatedAtBeforeWithdrawal = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.WithdrawFromValidation(20));

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Equal(updatedAtBeforeWithdrawal, task.UpdatedAt);
    }

    [Fact]
    public void ReassignValidation_ShouldChangeValidator()
    {
        var task = CreateClaimedValidationTask(30);

        task.ReassignValidation(40);

        Assert.Equal(40, task.ValidatorUserId);
        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.NotNull(task.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReassignValidation_ShouldThrow_WhenNewValidatorIdIsNotPositive(
        long invalidValidatorUserId)
    {
        var task = CreateClaimedValidationTask(30);
        var updatedAtBeforeReassignment = task.UpdatedAt;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            task.ReassignValidation(invalidValidatorUserId));

        Assert.Equal(30, task.ValidatorUserId);
        Assert.Equal(updatedAtBeforeReassignment, task.UpdatedAt);
    }

    [Fact]
    public void ReassignValidation_ShouldThrow_WhenValidationHasNoValidator()
    {
        var task = CreateTaskInValidation();
        var updatedAtBeforeReassignment = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.ReassignValidation(40));

        Assert.Null(task.ValidatorUserId);
        Assert.Equal(
            updatedAtBeforeReassignment,
            task.UpdatedAt);
    }

    [Fact]
    public void RemoveResponsible_ShouldThrow_WhenTaskIsInValidation()
    {
        var task = CreateTaskInValidation();
        var updatedAtBeforeRemoval = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.RemoveResponsible());

        Assert.Equal(ProjectTaskStatus.Validation, task.Status);
        Assert.Equal(20, task.ResponsibleUserId);
        Assert.Equal(updatedAtBeforeRemoval, task.UpdatedAt);
    }

    [Fact]
    public void Archive_ShouldArchiveDoneTask()
    {
        var task = CreateDoneTask();

        task.Archive();

        Assert.True(task.IsArchived);
        Assert.NotNull(task.ArchivedAt);
        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.Equal(task.ArchivedAt, task.UpdatedAt);
    }

    [Fact]
    public void Archive_ShouldArchiveCancelledTask()
    {
        var task = CreateTask();
        task.Cancel();

        task.Archive();

        Assert.True(task.IsArchived);
        Assert.NotNull(task.ArchivedAt);
        Assert.Equal(ProjectTaskStatus.Cancelled, task.Status);
    }

    [Fact]
    public void Archive_ShouldThrow_WhenTaskIsActive()
    {
        var task = CreateInProgressTask();
        var updatedAtBeforeArchiving = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Archive());

        Assert.False(task.IsArchived);
        Assert.Null(task.ArchivedAt);
        Assert.Equal(
            updatedAtBeforeArchiving,
            task.UpdatedAt);
    }

    [Fact]
    public void Archive_ShouldThrow_WhenTaskIsAlreadyArchived()
    {
        var task = CreateDoneTask();
        task.Archive();

        var archivedAt = task.ArchivedAt;
        var updatedAt = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Archive());

        Assert.Equal(archivedAt, task.ArchivedAt);
        Assert.Equal(updatedAt, task.UpdatedAt);
    }

    [Fact]
    public void Restore_ShouldRestoreTaskAndPreserveStatus()
    {
        var task = CreateDoneTask();
        task.Archive();

        task.Restore();

        Assert.False(task.IsArchived);
        Assert.Null(task.ArchivedAt);
        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Restore_ShouldThrow_WhenTaskIsNotArchived()
    {
        var task = CreateDoneTask();
        var updatedAtBeforeRestoration = task.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Restore());

        Assert.False(task.IsArchived);
        Assert.Equal(
            updatedAtBeforeRestoration,
            task.UpdatedAt);
    }

    [Fact]
    public void ArchivedTask_ShouldRejectEditing()
    {
        var task = CreateDoneTask();
        task.Archive();

        var archivedAt = task.ArchivedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.ChangeTitle("Novo título"));

        Assert.Equal("Tarefa original", task.Title);
        Assert.Equal(archivedAt, task.ArchivedAt);
    }

    [Fact]
    public void ArchivedTask_ShouldRejectStatusTransitions()
    {
        var task = CreateDoneTask();
        task.Archive();

        var archivedAt = task.ArchivedAt;

        Assert.Throws<InvalidOperationException>(() =>
            task.Reopen("Novos ajustes necessários."));

        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.Equal(archivedAt, task.ArchivedAt);
    }


    private static ProjectTask CreateTask(
    string? description = null,
    long? responsibleUserId = null,
    DateTime? dueDate = null)
    {
        return new ProjectTask(
            projectId: 1,
            title: "Tarefa original",
            priority: ProjectTaskPriority.Medium,
            createdByUserId: 10,
            description: description,
            responsibleUserId: responsibleUserId,
            dueDate: dueDate);
    }

    private static ProjectTask CreateInProgressTask()
    {
        var task = CreateTask(responsibleUserId: 20);
        task.MoveToTodo();
        task.Start();

        return task;
    }

    private static ProjectTask CreateTaskInValidation()
    {
        var task = CreateInProgressTask();
        task.SendToValidation();

        return task;
    }

    private static ProjectTask CreateClaimedValidationTask(
    long validatorUserId)
    {
        var task = CreateTaskInValidation();
        task.ClaimValidation(validatorUserId);

        return task;
    }

    private static ProjectTask CreateTaskInStatus(
    ProjectTaskStatus status)
    {
        var task = CreateTask(responsibleUserId: 20);

        if (status == ProjectTaskStatus.Backlog)
            return task;

        task.MoveToTodo();

        if (status == ProjectTaskStatus.Todo)
            return task;

        task.Start();

        if (status == ProjectTaskStatus.InProgress)
            return task;

        throw new ArgumentException(
            "Estado não suportado por este helper.",
            nameof(status));
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Validation)]
    public void Complete_ShouldThrow_WhenProjectHasOpenTask(
    ProjectTaskStatus openStatus)
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        ProjectTaskStatus[] taskStatuses =
        [
            ProjectTaskStatus.Done,
        openStatus
        ];

        var updatedAtBeforeCompletion = project.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            project.Complete(taskStatuses));

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Equal(
            updatedAtBeforeCompletion,
            project.UpdatedAt);
    }

    [Fact]
    public void Complete_ShouldThrow_WhenProjectIsNotInProgress()
    {
        var project = new Project(1, "WorkFlow", 10);

        ProjectTaskStatus[] taskStatuses =
        [
            ProjectTaskStatus.Done
        ];

        Assert.Throws<InvalidOperationException>(() =>
            project.Complete(taskStatuses));

        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.Null(project.UpdatedAt);
    }

    [Fact]
    public void Complete_ShouldThrow_WhenTaskStatusesIsNull()
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        Assert.Throws<ArgumentNullException>(() =>
            project.Complete(null!));

        Assert.Equal(ProjectStatus.InProgress, project.Status);
    }

    private static ProjectTask CreateDoneTask()
    {
        var task = CreateClaimedValidationTask(30);
        task.ApproveValidation(30);

        return task;
    }
}


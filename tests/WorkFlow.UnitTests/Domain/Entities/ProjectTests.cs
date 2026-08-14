using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_ShouldCreatePlanningProject_WhenDataIsValid()
    {
        var dueDate = DateTime.UtcNow.AddDays(7);

        var project = new Project(
            tenantId: 1,
            name: "  WorkFlow  ",
            createdByUserId: 10,
            description: "  Sistema de gerenciamento  ",
            responsibleUserId: 20,
            dueDate: dueDate);

        Assert.Equal(1, project.TenantId);
        Assert.NotEqual(Guid.Empty, project.PublicId);
        Assert.Equal("WorkFlow", project.Name);
        Assert.Equal("Sistema de gerenciamento", project.Description);
        Assert.Equal(10, project.CreatedByUserId);
        Assert.Equal(20, project.ResponsibleUserId);
        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.Equal(dueDate, project.DueDate);
        Assert.NotEqual(default, project.CreatedAt);
        Assert.Null(project.UpdatedAt);
        Assert.Null(project.ArchivedAt);
        Assert.False(project.IsArchived);
        Assert.Null(project.StatusBeforeArchive);
        Assert.Null(project.ArchivedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() =>
            new Project(
                1,
                invalidName!,
                10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenTenantIdIsNotPositive(
        long invalidTenantId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Project(
                invalidTenantId,
                "WorkFlow",
                10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenCreatedByUserIdIsNotPositive(
        long invalidCreatedByUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Project(
                1,
                "WorkFlow",
                invalidCreatedByUserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenResponsibleUserIdIsNotPositive(
        long invalidResponsibleUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Project(
                tenantId: 1,
                name: "WorkFlow",
                createdByUserId: 10,
                responsibleUserId: invalidResponsibleUserId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldNormalizeEmptyDescriptionToNull(
        string? description)
    {
        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            description: description);

        Assert.Null(project.Description);
    }

    [Fact]
    public void Rename_ShouldChangeNameAndSetUpdatedAt_WhenNameIsValid()
    {
        var project = new Project(1, "Nome antigo", 10);

        project.Rename("  Nome novo  ");

        Assert.Equal("Nome novo", project.Name);
        Assert.NotNull(project.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ShouldThrowAndPreserveProject_WhenNameIsInvalid(
        string? invalidName)
    {
        var project = new Project(1, "Nome original", 10);

        Assert.Throws<ArgumentException>(() =>
            project.Rename(invalidName!));

        Assert.Equal("Nome original", project.Name);
        Assert.Null(project.UpdatedAt);
    }

    [Fact]
    public void UpdateDescription_ShouldTrimDescription_WhenDescriptionIsValid()
    {
        var project = new Project(1, "WorkFlow", 10);

        project.UpdateDescription("  Nova descrição  ");

        Assert.Equal("Nova descrição", project.Description);
        Assert.NotNull(project.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDescription_ShouldSetNull_WhenDescriptionIsEmpty(
        string? description)
    {
        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            description: "Descrição original");

        project.UpdateDescription(description);

        Assert.Null(project.Description);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void ChangeDueDate_ShouldSetDueDateAndUpdatedAt()
    {
        var project = new Project(1, "WorkFlow", 10);
        var dueDate = DateTime.UtcNow.AddDays(30);

        project.ChangeDueDate(dueDate);

        Assert.Equal(dueDate, project.DueDate);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void ChangeDueDate_ShouldAllowRemovingDueDate()
    {
        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            dueDate: DateTime.UtcNow.AddDays(30));

        project.ChangeDueDate(null);

        Assert.Null(project.DueDate);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void AssignResponsible_ShouldSetResponsibleAndUpdatedAt()
    {
        var project = new Project(1, "WorkFlow", 10);

        project.AssignResponsible(20);

        Assert.Equal(20, project.ResponsibleUserId);
        Assert.NotNull(project.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AssignResponsible_ShouldThrowAndPreserveProject_WhenIdIsInvalid(
        long invalidResponsibleUserId)
    {
        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            responsibleUserId: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            project.AssignResponsible(invalidResponsibleUserId));

        Assert.Equal(20, project.ResponsibleUserId);
        Assert.Null(project.UpdatedAt);
    }

    [Fact]
    public void RemoveResponsible_ShouldRemoveResponsibleAndSetUpdatedAt()
    {
        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            responsibleUserId: 20);

        project.RemoveResponsible();

        Assert.Null(project.ResponsibleUserId);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void Start_ShouldChangeStatusToInProgress_WhenProjectIsPlanning()
    {
        var project = new Project(1, "WorkFlow", 10);

        project.Start();

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void Start_ShouldThrowAndPreserveProject_WhenProjectIsNotPlanning()
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        var updatedAtBeforeInvalidOperation = project.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            project.Start());

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Equal(updatedAtBeforeInvalidOperation, project.UpdatedAt);
    }

    [Fact]
    public void Pause_ShouldChangeStatusToPausedAndPreserveDueDate()
    {
        var dueDate = DateTime.UtcNow.AddDays(30);

        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            dueDate: dueDate);

        project.Start();
        project.Pause("Aguardando definição do cliente.");

        Assert.Equal(ProjectStatus.Paused, project.Status);
        Assert.Equal(dueDate, project.DueDate);
        Assert.NotNull(project.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Pause_ShouldThrowAndPreserveProject_WhenReasonIsInvalid(
        string? invalidReason)
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        var updatedAtBeforePause = project.UpdatedAt;

        Assert.Throws<ArgumentException>(() =>
            project.Pause(invalidReason!));

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Equal(updatedAtBeforePause, project.UpdatedAt);
    }

    [Fact]
    public void Pause_ShouldThrow_WhenProjectIsNotInProgress()
    {
        var project = new Project(1, "WorkFlow", 10);

        Assert.Throws<InvalidOperationException>(() =>
            project.Pause("Pausa necessária."));

        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.Null(project.UpdatedAt);
    }

    [Fact]
    public void Resume_ShouldReturnToInProgressAndPreserveDueDate()
    {
        var dueDate = DateTime.UtcNow.AddDays(30);

        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            dueDate: dueDate);

        project.Start();
        project.Pause("Aguardando cliente.");
        project.Resume();

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Equal(dueDate, project.DueDate);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void Resume_ShouldChangeDueDate_WhenNewDueDateIsProvided()
    {
        var originalDueDate = DateTime.UtcNow.AddDays(10);
        var newDueDate = DateTime.UtcNow.AddDays(30);

        var project = new Project(
            tenantId: 1,
            name: "WorkFlow",
            createdByUserId: 10,
            dueDate: originalDueDate);

        project.Start();
        project.Pause("Prazo precisa ser revisto.");
        project.Resume(newDueDate);

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Equal(newDueDate, project.DueDate);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void Resume_ShouldThrow_WhenProjectIsNotPaused()
    {
        var project = new Project(1, "WorkFlow", 10);

        Assert.Throws<InvalidOperationException>(() =>
            project.Resume());

        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.Null(project.UpdatedAt);
    }

    [Fact]
    public void Complete_ShouldCompleteProject_WhenAllTasksAreTerminal()
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        ProjectTaskStatus[] taskStatuses =
        [
            ProjectTaskStatus.Done,
        ProjectTaskStatus.Cancelled,
        ProjectTaskStatus.Done
        ];

        project.Complete(taskStatuses);

        Assert.Equal(ProjectStatus.Completed, project.Status);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void Complete_ShouldThrow_WhenProjectHasNoTasks()
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        var updatedAtBeforeCompletion = project.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            project.Complete(Array.Empty<ProjectTaskStatus>()));

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Equal(
            updatedAtBeforeCompletion,
            project.UpdatedAt);
    }

    [Fact]
    public void Reopen_ShouldReturnCompletedProjectToInProgress()
    {
        var project = CreateCompletedProject();

        project.Reopen("Novas tarefas foram solicitadas.");

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.NotNull(project.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reopen_ShouldThrowAndPreserveProject_WhenReasonIsInvalid(
        string? invalidReason)
    {
        var project = CreateCompletedProject();
        var updatedAtBeforeReopening = project.UpdatedAt;

        Assert.Throws<ArgumentException>(() =>
            project.Reopen(invalidReason!));

        Assert.Equal(ProjectStatus.Completed, project.Status);
        Assert.Equal(
            updatedAtBeforeReopening,
            project.UpdatedAt);
    }

    [Fact]
    public void Reopen_ShouldThrow_WhenProjectIsNotCompleted()
    {
        var project = new Project(1, "WorkFlow", 10);

        Assert.Throws<InvalidOperationException>(() =>
            project.Reopen("Tentativa de reabertura."));

        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.Null(project.UpdatedAt);
    }

    [Fact]
    public void Archive_ShouldArchivePlanningProject()
    {
        var project = new Project(1, "WorkFlow", 10);

        project.Archive();

        Assert.Equal(ProjectStatus.Archived, project.Status);
        Assert.Equal(
            ProjectStatus.Planning,
            project.StatusBeforeArchive);
        Assert.True(project.IsArchived);
        Assert.NotNull(project.ArchivedAt);
        Assert.Equal(project.ArchivedAt, project.UpdatedAt);
    }

    [Fact]
    public void Archive_ShouldArchiveCompletedProject()
    {
        var project = CreateCompletedProject();

        project.Archive();

        Assert.Equal(ProjectStatus.Archived, project.Status);
        Assert.Equal(
            ProjectStatus.Completed,
            project.StatusBeforeArchive);
        Assert.True(project.IsArchived);
        Assert.NotNull(project.ArchivedAt);
    }

    [Fact]
    public void Archive_ShouldThrow_WhenProjectIsInProgress()
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        var updatedAtBeforeArchiving = project.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            project.Archive());

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Null(project.StatusBeforeArchive);
        Assert.Null(project.ArchivedAt);
        Assert.Equal(
            updatedAtBeforeArchiving,
            project.UpdatedAt);
    }

    [Fact]
    public void Archive_ShouldThrow_WhenProjectIsAlreadyArchived()
    {
        var project = CreateCompletedProject();
        project.Archive();

        var archivedAtBeforeSecondAttempt = project.ArchivedAt;
        var updatedAtBeforeSecondAttempt = project.UpdatedAt;

        Assert.Throws<InvalidOperationException>(() =>
            project.Archive());

        Assert.Equal(ProjectStatus.Archived, project.Status);
        Assert.Equal(
            ProjectStatus.Completed,
            project.StatusBeforeArchive);
        Assert.Equal(
            archivedAtBeforeSecondAttempt,
            project.ArchivedAt);
        Assert.Equal(
            updatedAtBeforeSecondAttempt,
            project.UpdatedAt);
    }

    [Fact]
    public void Restore_ShouldReturnPlanningProjectToPlanning()
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Archive();

        project.Restore();

        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.Null(project.StatusBeforeArchive);
        Assert.False(project.IsArchived);
        Assert.Null(project.ArchivedAt);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void Restore_ShouldReturnCompletedProjectToCompleted()
    {
        var project = CreateCompletedProject();
        project.Archive();

        project.Restore();

        Assert.Equal(ProjectStatus.Completed, project.Status);
        Assert.Null(project.StatusBeforeArchive);
        Assert.False(project.IsArchived);
        Assert.Null(project.ArchivedAt);
        Assert.NotNull(project.UpdatedAt);
    }

    [Fact]
    public void Restore_ShouldThrow_WhenProjectIsNotArchived()
    {
        var project = new Project(1, "WorkFlow", 10);

        Assert.Throws<InvalidOperationException>(() =>
            project.Restore());

        Assert.Equal(ProjectStatus.Planning, project.Status);
        Assert.Null(project.StatusBeforeArchive);
        Assert.Null(project.ArchivedAt);
        Assert.Null(project.UpdatedAt);
    }

    [Fact]
    public void RestoredCompletedProject_ShouldRemainCompletedUntilReopened()
    {
        var project = CreateCompletedProject();
        project.Archive();

        project.Restore();

        Assert.Equal(ProjectStatus.Completed, project.Status);

        project.Reopen("Novas tarefas foram solicitadas.");

        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.False(project.IsArchived);
        Assert.Null(project.ArchivedAt);
    }

    [Fact]
    public void ArchivedProject_ShouldRejectEditing()
    {
        var project = CreateCompletedProject();
        project.Archive();

        var archivedAt = project.ArchivedAt;

        Assert.Throws<InvalidOperationException>(() =>
            project.Rename("Novo nome"));

        Assert.Equal("WorkFlow", project.Name);
        Assert.Equal(ProjectStatus.Archived, project.Status);
        Assert.Equal(archivedAt, project.ArchivedAt);
    }

    [Fact]
    public void ArchivedProject_ShouldRejectReopeningBeforeRestoration()
    {
        var project = CreateCompletedProject();
        project.Archive();

        var archivedAt = project.ArchivedAt;

        Assert.Throws<InvalidOperationException>(() =>
            project.Reopen("Novas tarefas foram solicitadas."));

        Assert.Equal(ProjectStatus.Archived, project.Status);
        Assert.Equal(
            ProjectStatus.Completed,
            project.StatusBeforeArchive);
        Assert.Equal(archivedAt, project.ArchivedAt);
    }





    private static Project CreateCompletedProject()
    {
        var project = new Project(1, "WorkFlow", 10);
        project.Start();

        ProjectTaskStatus[] taskStatuses =
        [
            ProjectTaskStatus.Done,
        ProjectTaskStatus.Cancelled
        ];

        project.Complete(taskStatuses);

        return project;
    }
}
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Entities;

public class ProjectHistoryTests
{
    [Fact]
    public void Constructor_ShouldCreateHistory_WhenDataIsValid()
    {
        var beforeCreation = DateTime.UtcNow;

        var history = new ProjectHistory(
            projectId: 10,
            actorUserId: 20,
            action: ProjectHistoryAction.DueDateChanged,
            reason: "  Prazo ajustado com o cliente.  ",
            oldValue: "  2026-08-30  ",
            newValue: "  2026-09-15  ");

        var afterCreation = DateTime.UtcNow;

        Assert.Equal(10, history.ProjectId);
        Assert.Equal(20, history.ActorUserId);

        Assert.Equal(
            ProjectHistoryAction.DueDateChanged,
            history.Action);

        Assert.Equal(
            "Prazo ajustado com o cliente.",
            history.Reason);

        Assert.Equal("2026-08-30", history.OldValue);
        Assert.Equal("2026-09-15", history.NewValue);

        Assert.InRange(
            history.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Fact]
    public void Constructor_ShouldNormalizeEmptyOptionalTextsToNull()
    {
        var history = new ProjectHistory(
            projectId: 10,
            actorUserId: 20,
            action: ProjectHistoryAction.Created,
            reason: "   ",
            oldValue: "",
            newValue: null);

        Assert.Null(history.Reason);
        Assert.Null(history.OldValue);
        Assert.Null(history.NewValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenProjectIdIsNotPositive(
        long invalidProjectId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProjectHistory(
                invalidProjectId,
                actorUserId: 20,
                action: ProjectHistoryAction.Created));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenActorUserIdIsNotPositive(
        long invalidActorUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProjectHistory(
                projectId: 10,
                invalidActorUserId,
                action: ProjectHistoryAction.Created));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_ShouldThrow_WhenActionIsInvalid(
        int invalidActionValue)
    {
        var invalidAction =
            (ProjectHistoryAction)invalidActionValue;

        Assert.Throws<ArgumentException>(
            () => new ProjectHistory(
                projectId: 10,
                actorUserId: 20,
                action: invalidAction));
    }
}
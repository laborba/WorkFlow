using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Entities;

public class TaskHistoryTests
{
    [Fact]
    public void Constructor_ShouldCreateHistory_WhenDataIsValid()
    {
        var beforeCreation = DateTime.UtcNow;

        var history = new TaskHistory(
            taskId: 10,
            actorUserId: 20,
            action: TaskHistoryAction.PriorityChanged,
            reason: "  Prioridade necessária.  ",
            oldValue: "  Medium  ",
            newValue: "  High  ");

        var afterCreation = DateTime.UtcNow;

        Assert.Equal(10, history.TaskId);
        Assert.Equal(20, history.ActorUserId);
        Assert.Equal(
            TaskHistoryAction.PriorityChanged,
            history.Action);

        Assert.Equal(
            "Prioridade necessária.",
            history.Reason);

        Assert.Equal("Medium", history.OldValue);
        Assert.Equal("High", history.NewValue);

        Assert.InRange(
            history.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Fact]
    public void Constructor_ShouldNormalizeEmptyOptionalTextsToNull()
    {
        var history = new TaskHistory(
            taskId: 10,
            actorUserId: 20,
            action: TaskHistoryAction.Created,
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
    public void Constructor_ShouldThrow_WhenTaskIdIsNotPositive(
        long invalidTaskId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TaskHistory(
                invalidTaskId,
                actorUserId: 20,
                action: TaskHistoryAction.Created));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenActorUserIdIsNotPositive(
        long invalidActorUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TaskHistory(
                taskId: 10,
                invalidActorUserId,
                action: TaskHistoryAction.Created));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_ShouldThrow_WhenActionIsInvalid(
        int invalidActionValue)
    {
        var invalidAction =
            (TaskHistoryAction)invalidActionValue;

        Assert.Throws<ArgumentException>(
            () => new TaskHistory(
                taskId: 10,
                actorUserId: 20,
                action: invalidAction));
    }
}
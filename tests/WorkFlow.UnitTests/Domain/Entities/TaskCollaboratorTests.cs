using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Domain.Entities;

public class TaskCollaboratorTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveTaskCollaborator_WhenDataIsValid()
    {
        // Arrange
        const long taskId = 10;
        const long userId = 20;
        const long addedByUserId = 30;

        var beforeCreation = DateTime.UtcNow;

        // Act
        var collaborator = new TaskCollaborator(
            taskId,
            userId,
            addedByUserId);

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(taskId, collaborator.TaskId);
        Assert.Equal(userId, collaborator.UserId);
        Assert.Equal(addedByUserId, collaborator.AddedByUserId);
        Assert.InRange(
            collaborator.AddedAt,
            beforeCreation,
            afterCreation);

        Assert.Null(collaborator.RemovedAt);
        Assert.Null(collaborator.RemovedByUserId);
        Assert.True(collaborator.IsActive);
    }

    [Theory]
    [InlineData(
        0,
        20,
        30,
        "taskId",
        "O identificador da tarefa deve ser maior que zero.")]
    [InlineData(
        -1,
        20,
        30,
        "taskId",
        "O identificador da tarefa deve ser maior que zero.")]
    [InlineData(
        10,
        0,
        30,
        "userId",
        "O identificador do usuário deve ser maior que zero.")]
    [InlineData(
        10,
        -1,
        30,
        "userId",
        "O identificador do usuário deve ser maior que zero.")]
    [InlineData(
        10,
        20,
        0,
        "addedByUserId",
        "O identificador do usuário que adicionou o colaborador deve ser maior que zero.")]
    [InlineData(
        10,
        20,
        -1,
        "addedByUserId",
        "O identificador do usuário que adicionou o colaborador deve ser maior que zero.")]
    public void Constructor_ShouldThrow_WhenAnyIdIsInvalid(
        long taskId,
        long userId,
        long addedByUserId,
        string expectedParameter,
        string expectedMessage)
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new TaskCollaborator(
                taskId,
                userId,
                addedByUserId));

        // Assert
        Assert.Equal(expectedParameter, exception.ParamName);
        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void Remove_ShouldDeactivateCollaborator_WhenCollaboratorIsActive()
    {
        // Arrange
        var collaborator = new TaskCollaborator(
            taskId: 10,
            userId: 20,
            addedByUserId: 30);

        const long removedByUserId = 40;

        var beforeRemoval = DateTime.UtcNow;

        // Act
        collaborator.Remove(removedByUserId);

        var afterRemoval = DateTime.UtcNow;

        // Assert
        Assert.False(collaborator.IsActive);
        Assert.Equal(
            removedByUserId,
            collaborator.RemovedByUserId);

        Assert.NotNull(collaborator.RemovedAt);
        Assert.InRange(
            collaborator.RemovedAt.Value,
            beforeRemoval,
            afterRemoval);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Remove_ShouldThrow_WhenRemovedByUserIdIsInvalid(
        long removedByUserId)
    {
        // Arrange
        var collaborator = new TaskCollaborator(
            taskId: 10,
            userId: 20,
            addedByUserId: 30);

        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => collaborator.Remove(removedByUserId));

        // Assert
        Assert.Equal(
            "removedByUserId",
            exception.ParamName);

        Assert.Contains(
            "O identificador do usuário que removeu o colaborador deve ser maior que zero.",
            exception.Message);

        Assert.True(collaborator.IsActive);
        Assert.Null(collaborator.RemovedAt);
        Assert.Null(collaborator.RemovedByUserId);
    }

    [Fact]
    public void Remove_ShouldThrow_WhenCollaboratorWasAlreadyRemoved()
    {
        // Arrange
        var collaborator = new TaskCollaborator(
            taskId: 10,
            userId: 20,
            addedByUserId: 30);

        collaborator.Remove(removedByUserId: 40);

        var firstRemovedAt = collaborator.RemovedAt;
        var firstRemovedByUserId = collaborator.RemovedByUserId;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => collaborator.Remove(removedByUserId: 50));

        // Assert
        Assert.Equal(
            "O colaborador já foi removido da tarefa.",
            exception.Message);

        Assert.False(collaborator.IsActive);
        Assert.Equal(firstRemovedAt, collaborator.RemovedAt);
        Assert.Equal(
            firstRemovedByUserId,
            collaborator.RemovedByUserId);
    }
}
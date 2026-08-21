using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Domain.Entities;

public class TaskCommentTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveComment_WhenDataIsValid()
    {
        // Arrange
        const long taskId = 10;
        const long authorUserId = 20;

        var beforeCreation = DateTime.UtcNow;

        // Act
        var comment = new TaskComment(
            taskId,
            authorUserId,
            "  Comentário da tarefa.  ");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(taskId, comment.TaskId);
        Assert.Equal(authorUserId, comment.AuthorUserId);
        Assert.Equal(
            "Comentário da tarefa.",
            comment.Content);

        Assert.InRange(
            comment.CreatedAt,
            beforeCreation,
            afterCreation);

        Assert.Null(comment.UpdatedAt);
        Assert.Null(comment.DeletedAt);
        Assert.Null(comment.DeletedByUserId);
        Assert.False(comment.IsDeleted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenTaskIdIsNotPositive(
        long invalidTaskId)
    {
        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new TaskComment(
                    invalidTaskId,
                    authorUserId: 20,
                    content: "Comentário válido."));

        // Assert
        Assert.Equal("taskId", exception.ParamName);
        Assert.Contains(
            "O identificador da tarefa deve ser maior que zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenAuthorUserIdIsNotPositive(
        long invalidAuthorUserId)
    {
        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new TaskComment(
                    taskId: 10,
                    invalidAuthorUserId,
                    content: "Comentário válido."));

        // Assert
        Assert.Equal("authorUserId", exception.ParamName);
        Assert.Contains(
            "O identificador do autor deve ser maior que zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenContentIsInvalid(
        string? invalidContent)
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => new TaskComment(
                taskId: 10,
                authorUserId: 20,
                content: invalidContent!));

        // Assert
        Assert.Equal("content", exception.ParamName);
        Assert.Contains(
            "O conteúdo do comentário não pode estar vazio.",
            exception.Message);
    }

    [Fact]
    public void Edit_ShouldUpdateContent_WhenAuthorEditsWithinWindow()
    {
        // Arrange
        var comment = CreateComment();
        var editedAtUtc = comment.CreatedAt.AddMinutes(10);

        // Act
        comment.Edit(
            "  Conteúdo atualizado.  ",
            editorUserId: 20,
            editedAtUtc);

        // Assert
        Assert.Equal(
            "Conteúdo atualizado.",
            comment.Content);

        Assert.Equal(editedAtUtc, comment.UpdatedAt);
        Assert.False(comment.IsDeleted);
    }

    [Fact]
    public void Edit_ShouldAllowEditingExactlyAtWindowLimit()
    {
        // Arrange
        var comment = CreateComment();

        var editedAtUtc =
            comment.CreatedAt.Add(TaskComment.EditWindow);

        // Act
        comment.Edit(
            "Conteúdo atualizado no limite.",
            editorUserId: 20,
            editedAtUtc);

        // Assert
        Assert.Equal(
            "Conteúdo atualizado no limite.",
            comment.Content);

        Assert.Equal(editedAtUtc, comment.UpdatedAt);
    }

    [Fact]
    public void Edit_ShouldThrowAndPreserveComment_WhenWindowHasExpired()
    {
        // Arrange
        var comment = CreateComment();

        var editedAtUtc = comment.CreatedAt
            .Add(TaskComment.EditWindow)
            .AddTicks(1);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => comment.Edit(
                "Conteúdo atualizado.",
                editorUserId: 20,
                editedAtUtc));

        // Assert
        Assert.Equal(
            "O prazo de 15 minutos para editar o comentário foi encerrado.",
            exception.Message);

        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public void Edit_ShouldThrowAndPreserveComment_WhenUserIsNotAuthor()
    {
        // Arrange
        var comment = CreateComment();
        var editedAtUtc = comment.CreatedAt.AddMinutes(5);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => comment.Edit(
                "Conteúdo atualizado.",
                editorUserId: 30,
                editedAtUtc));

        // Assert
        Assert.Equal(
            "Somente o autor pode editar o comentário.",
            exception.Message);

        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Edit_ShouldThrow_WhenEditorUserIdIsNotPositive(
        long invalidEditorUserId)
    {
        // Arrange
        var comment = CreateComment();
        var editedAtUtc = comment.CreatedAt.AddMinutes(5);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => comment.Edit(
                    "Conteúdo atualizado.",
                    invalidEditorUserId,
                    editedAtUtc));

        // Assert
        Assert.Equal("editorUserId", exception.ParamName);
        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Edit_ShouldThrowAndPreserveComment_WhenContentIsInvalid(
        string? invalidContent)
    {
        // Arrange
        var comment = CreateComment();
        var editedAtUtc = comment.CreatedAt.AddMinutes(5);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => comment.Edit(
                invalidContent!,
                editorUserId: 20,
                editedAtUtc));

        // Assert
        Assert.Equal("content", exception.ParamName);
        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public void Edit_ShouldThrow_WhenEditedAtIsBeforeCreatedAt()
    {
        // Arrange
        var comment = CreateComment();
        var editedAtUtc = comment.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => comment.Edit(
                    "Conteúdo atualizado.",
                    editorUserId: 20,
                    editedAtUtc));

        // Assert
        Assert.Equal("editedAtUtc", exception.ParamName);
        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public void Edit_ShouldThrow_WhenEditedAtIsNotUtc()
    {
        // Arrange
        var comment = CreateComment();

        var editedAtLocal = DateTime.SpecifyKind(
            comment.CreatedAt.AddMinutes(5),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => comment.Edit(
                "Conteúdo atualizado.",
                editorUserId: 20,
                editedAtLocal));

        // Assert
        Assert.Equal("editedAtUtc", exception.ParamName);
        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    private static TaskComment CreateComment()
    {
        return new TaskComment(
            taskId: 10,
            authorUserId: 20,
            content: "Comentário original.");
    }

    [Fact]
    public void Delete_ShouldMarkCommentAsDeleted_WhenAuthorDeletesWithinWindow()
    {
        // Arrange
        var comment = CreateComment();
        var deletedAtUtc = comment.CreatedAt.AddMinutes(10);

        // Act
        comment.Delete(
            deletedByUserId: 20,
            deletedAtUtc: deletedAtUtc);

        // Assert
        Assert.True(comment.IsDeleted);
        Assert.Equal(deletedAtUtc, comment.DeletedAt);
        Assert.Equal(20, comment.DeletedByUserId);
        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public void Delete_ShouldAllowDeletionExactlyAtWindowLimit()
    {
        // Arrange
        var comment = CreateComment();

        var deletedAtUtc =
            comment.CreatedAt.Add(TaskComment.EditWindow);

        // Act
        comment.Delete(
            deletedByUserId: 20,
            deletedAtUtc: deletedAtUtc);

        // Assert
        Assert.True(comment.IsDeleted);
        Assert.Equal(deletedAtUtc, comment.DeletedAt);
        Assert.Equal(20, comment.DeletedByUserId);
    }

    [Fact]
    public void Delete_ShouldThrowAndPreserveComment_WhenWindowHasExpired()
    {
        // Arrange
        var comment = CreateComment();

        var deletedAtUtc = comment.CreatedAt
            .Add(TaskComment.EditWindow)
            .AddTicks(1);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => comment.Delete(
                deletedByUserId: 20,
                deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal(
            "O prazo de 15 minutos para remover o comentário foi encerrado.",
            exception.Message);

        Assert.False(comment.IsDeleted);
        Assert.Null(comment.DeletedAt);
        Assert.Null(comment.DeletedByUserId);
    }

    [Fact]
    public void Delete_ShouldThrowAndPreserveComment_WhenUserIsNotAuthor()
    {
        // Arrange
        var comment = CreateComment();
        var deletedAtUtc = comment.CreatedAt.AddMinutes(5);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => comment.Delete(
                deletedByUserId: 30,
                deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal(
            "Somente o autor pode remover o comentário.",
            exception.Message);

        Assert.False(comment.IsDeleted);
        Assert.Null(comment.DeletedAt);
        Assert.Null(comment.DeletedByUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Delete_ShouldThrow_WhenDeletedByUserIdIsNotPositive(
        long invalidDeletedByUserId)
    {
        // Arrange
        var comment = CreateComment();
        var deletedAtUtc = comment.CreatedAt.AddMinutes(5);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => comment.Delete(
                    invalidDeletedByUserId,
                    deletedAtUtc));

        // Assert
        Assert.Equal("deletedByUserId", exception.ParamName);
        Assert.False(comment.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldThrow_WhenDeletedAtIsBeforeCreatedAt()
    {
        // Arrange
        var comment = CreateComment();
        var deletedAtUtc = comment.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => comment.Delete(
                    deletedByUserId: 20,
                    deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(comment.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldThrow_WhenDeletedAtIsNotUtc()
    {
        // Arrange
        var comment = CreateComment();

        var deletedAtLocal = DateTime.SpecifyKind(
            comment.CreatedAt.AddMinutes(5),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => comment.Delete(
                deletedByUserId: 20,
                deletedAtUtc: deletedAtLocal));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(comment.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldThrowAndPreserveFirstDeletion_WhenCommentIsAlreadyDeleted()
    {
        // Arrange
        var comment = CreateComment();

        var firstDeletedAtUtc =
            comment.CreatedAt.AddMinutes(5);

        comment.Delete(
            deletedByUserId: 20,
            deletedAtUtc: firstDeletedAtUtc);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => comment.Delete(
                deletedByUserId: 20,
                deletedAtUtc: comment.CreatedAt.AddMinutes(6)));

        // Assert
        Assert.Equal(
            "Um comentário removido não pode ser removido novamente.",
            exception.Message);

        Assert.Equal(firstDeletedAtUtc, comment.DeletedAt);
        Assert.Equal(20, comment.DeletedByUserId);
    }

    [Fact]
    public void Edit_ShouldThrow_WhenCommentIsDeleted()
    {
        // Arrange
        var comment = CreateComment();

        comment.Delete(
            deletedByUserId: 20,
            deletedAtUtc: comment.CreatedAt.AddMinutes(5));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => comment.Edit(
                "Tentativa de edição.",
                editorUserId: 20,
                editedAtUtc: comment.CreatedAt.AddMinutes(6)));

        // Assert
        Assert.Equal(
            "Um comentário removido não pode ser editado.",
            exception.Message);

        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public void DeleteByModerator_ShouldDeleteComment_WhenWindowHasExpired()
    {
        // Arrange
        var comment = CreateComment();

        var deletedAtUtc =
            comment.CreatedAt.AddDays(1);

        // Act
        comment.DeleteByModerator(
            moderatorUserId: 30,
            deletedAtUtc: deletedAtUtc);

        // Assert
        Assert.True(comment.IsDeleted);
        Assert.Equal(deletedAtUtc, comment.DeletedAt);
        Assert.Equal(30, comment.DeletedByUserId);
        Assert.Equal("Comentário original.", comment.Content);
        Assert.Null(comment.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DeleteByModerator_ShouldThrow_WhenModeratorUserIdIsNotPositive(
        long invalidModeratorUserId)
    {
        // Arrange
        var comment = CreateComment();
        var deletedAtUtc = comment.CreatedAt.AddDays(1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => comment.DeleteByModerator(
                    invalidModeratorUserId,
                    deletedAtUtc));

        // Assert
        Assert.Equal("deletedByUserId", exception.ParamName);
        Assert.False(comment.IsDeleted);
    }

    [Fact]
    public void DeleteByModerator_ShouldThrow_WhenDeletedAtIsBeforeCreatedAt()
    {
        // Arrange
        var comment = CreateComment();
        var deletedAtUtc = comment.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => comment.DeleteByModerator(
                    moderatorUserId: 30,
                    deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(comment.IsDeleted);
    }

    [Fact]
    public void DeleteByModerator_ShouldThrow_WhenDeletedAtIsNotUtc()
    {
        // Arrange
        var comment = CreateComment();

        var deletedAtLocal = DateTime.SpecifyKind(
            comment.CreatedAt.AddDays(1),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => comment.DeleteByModerator(
                moderatorUserId: 30,
                deletedAtUtc: deletedAtLocal));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(comment.IsDeleted);
    }

    [Fact]
    public void DeleteByModerator_ShouldPreserveFirstDeletion_WhenCommentIsAlreadyDeleted()
    {
        // Arrange
        var comment = CreateComment();

        var firstDeletedAtUtc =
            comment.CreatedAt.AddMinutes(5);

        comment.Delete(
            deletedByUserId: 20,
            deletedAtUtc: firstDeletedAtUtc);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => comment.DeleteByModerator(
                moderatorUserId: 30,
                deletedAtUtc: comment.CreatedAt.AddDays(1)));

        // Assert
        Assert.Equal(
            "Um comentário removido não pode ser removido novamente.",
            exception.Message);

        Assert.Equal(firstDeletedAtUtc, comment.DeletedAt);
        Assert.Equal(20, comment.DeletedByUserId);
    }
}
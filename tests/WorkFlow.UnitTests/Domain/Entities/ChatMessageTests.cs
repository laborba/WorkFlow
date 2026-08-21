using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Domain.Entities;

public class ChatMessageTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveMessage_WhenDataIsValid()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var message = new ChatMessage(
            tenantId: 10,
            authorUserId: 20,
            content: "  Olá, equipe!  ");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(10, message.TenantId);
        Assert.Equal(20, message.AuthorUserId);
        Assert.Equal("Olá, equipe!", message.Content);

        Assert.InRange(
            message.CreatedAt,
            beforeCreation,
            afterCreation);

        Assert.Equal(
            message.CreatedAt.Add(ChatMessage.RetentionPeriod),
            message.ExpiresAt);

        Assert.Null(message.UpdatedAt);
        Assert.Null(message.DeletedAt);
        Assert.Null(message.DeletedByUserId);
        Assert.False(message.IsDeleted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenTenantIdIsNotPositive(
        long invalidTenantId)
    {
        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChatMessage(
                    invalidTenantId,
                    authorUserId: 20,
                    content: "Mensagem válida."));

        // Assert
        Assert.Equal("tenantId", exception.ParamName);
        Assert.Contains(
            "O identificador da empresa deve ser maior que zero.",
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
                () => new ChatMessage(
                    tenantId: 10,
                    invalidAuthorUserId,
                    content: "Mensagem válida."));

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
            () => new ChatMessage(
                tenantId: 10,
                authorUserId: 20,
                content: invalidContent!));

        // Assert
        Assert.Equal("content", exception.ParamName);
        Assert.Contains(
            "O conteúdo da mensagem não pode estar vazio.",
            exception.Message);
    }
    [Fact]
    public void DeleteByModerator_ShouldDeleteMessage_WhenEditWindowHasExpired()
    {
        // Arrange
        var message = CreateMessage();

        var deletedAtUtc =
            message.CreatedAt.AddHours(1);

        // Act
        message.DeleteByModerator(
            moderatorUserId: 30,
            deletedAtUtc: deletedAtUtc);

        // Assert
        Assert.True(message.IsDeleted);
        Assert.Equal(deletedAtUtc, message.DeletedAt);
        Assert.Equal(30, message.DeletedByUserId);
        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }

    [Fact]
    public void DeleteByModerator_ShouldDeleteMessage_WhenMessageIsExpired()
    {
        // Arrange
        var message = CreateMessage();

        // Act
        message.DeleteByModerator(
            moderatorUserId: 30,
            deletedAtUtc: message.ExpiresAt);

        // Assert
        Assert.True(message.IsDeleted);
        Assert.Equal(message.ExpiresAt, message.DeletedAt);
        Assert.Equal(30, message.DeletedByUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DeleteByModerator_ShouldThrow_WhenModeratorUserIdIsNotPositive(
        long invalidModeratorUserId)
    {
        // Arrange
        var message = CreateMessage();
        var deletedAtUtc = message.CreatedAt.AddHours(1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => message.DeleteByModerator(
                    invalidModeratorUserId,
                    deletedAtUtc));

        // Assert
        Assert.Equal("deletedByUserId", exception.ParamName);
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void DeleteByModerator_ShouldThrow_WhenDeletedAtIsBeforeCreatedAt()
    {
        // Arrange
        var message = CreateMessage();
        var deletedAtUtc = message.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => message.DeleteByModerator(
                    moderatorUserId: 30,
                    deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void DeleteByModerator_ShouldThrow_WhenDeletedAtIsNotUtc()
    {
        // Arrange
        var message = CreateMessage();

        var deletedAtLocal = DateTime.SpecifyKind(
            message.CreatedAt.AddHours(1),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => message.DeleteByModerator(
                moderatorUserId: 30,
                deletedAtUtc: deletedAtLocal));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void DeleteByModerator_ShouldPreserveFirstDeletion_WhenMessageIsAlreadyDeleted()
    {
        // Arrange
        var message = CreateMessage();

        var firstDeletedAtUtc =
            message.CreatedAt.AddMinutes(2);

        message.Delete(
            deletedByUserId: 20,
            deletedAtUtc: firstDeletedAtUtc);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.DeleteByModerator(
                moderatorUserId: 30,
                deletedAtUtc: message.CreatedAt.AddHours(1)));

        // Assert
        Assert.Equal(
            "Uma mensagem removida não pode ser removida novamente.",
            exception.Message);

        Assert.Equal(firstDeletedAtUtc, message.DeletedAt);
        Assert.Equal(20, message.DeletedByUserId);
    }

    [Fact]
    public void IsExpiredAt_ShouldReturnFalse_WhenRetentionPeriodHasNotEnded()
    {
        // Arrange
        var message = CreateMessage();

        var utcNow =
            message.ExpiresAt.AddTicks(-1);

        // Act
        var isExpired = message.IsExpiredAt(utcNow);

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void IsExpiredAt_ShouldReturnTrue_WhenRetentionPeriodHasEnded()
    {
        // Arrange
        var message = CreateMessage();

        // Act
        var isExpired =
            message.IsExpiredAt(message.ExpiresAt);

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void IsExpiredAt_ShouldThrow_WhenDateIsNotUtc()
    {
        // Arrange
        var message = CreateMessage();

        var localDate = DateTime.SpecifyKind(
            message.CreatedAt.AddDays(1),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => message.IsExpiredAt(localDate));

        // Assert
        Assert.Equal("utcNow", exception.ParamName);
    }

    [Fact]
    public void IsExpiredAt_ShouldThrow_WhenDateIsBeforeCreatedAt()
    {
        // Arrange
        var message = CreateMessage();
        var utcNow = message.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => message.IsExpiredAt(utcNow));

        // Assert
        Assert.Equal("utcNow", exception.ParamName);
    }

    private static ChatMessage CreateMessage()
    {
        return new ChatMessage(
            tenantId: 10,
            authorUserId: 20,
            content: "Mensagem original.");
    }

    [Fact]
    public void Edit_ShouldUpdateContent_WhenAuthorEditsWithinWindow()
    {
        // Arrange
        var message = CreateMessage();
        var editedAtUtc = message.CreatedAt.AddMinutes(3);

        // Act
        message.Edit(
            "  Mensagem atualizada.  ",
            editorUserId: 20,
            editedAtUtc: editedAtUtc);

        // Assert
        Assert.Equal("Mensagem atualizada.", message.Content);
        Assert.Equal(editedAtUtc, message.UpdatedAt);

        Assert.Equal(
            message.CreatedAt.Add(ChatMessage.RetentionPeriod),
            message.ExpiresAt);

        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void Edit_ShouldAllowEditingExactlyAtWindowLimit()
    {
        // Arrange
        var message = CreateMessage();

        var editedAtUtc =
            message.CreatedAt.Add(ChatMessage.EditWindow);

        // Act
        message.Edit(
            "Mensagem atualizada no limite.",
            editorUserId: 20,
            editedAtUtc: editedAtUtc);

        // Assert
        Assert.Equal(
            "Mensagem atualizada no limite.",
            message.Content);

        Assert.Equal(editedAtUtc, message.UpdatedAt);
    }

    [Fact]
    public void Edit_ShouldThrowAndPreserveMessage_WhenWindowHasExpired()
    {
        // Arrange
        var message = CreateMessage();

        var editedAtUtc = message.CreatedAt
            .Add(ChatMessage.EditWindow)
            .AddTicks(1);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Edit(
                "Mensagem atualizada.",
                editorUserId: 20,
                editedAtUtc: editedAtUtc));

        // Assert
        Assert.Equal(
            "O prazo de 5 minutos para editar a mensagem foi encerrado.",
            exception.Message);

        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }

    [Fact]
    public void Edit_ShouldThrowAndPreserveMessage_WhenUserIsNotAuthor()
    {
        // Arrange
        var message = CreateMessage();
        var editedAtUtc = message.CreatedAt.AddMinutes(3);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Edit(
                "Mensagem atualizada.",
                editorUserId: 30,
                editedAtUtc: editedAtUtc));

        // Assert
        Assert.Equal(
            "Somente o autor pode editar a mensagem.",
            exception.Message);

        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Edit_ShouldThrow_WhenEditorUserIdIsNotPositive(
        long invalidEditorUserId)
    {
        // Arrange
        var message = CreateMessage();
        var editedAtUtc = message.CreatedAt.AddMinutes(3);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => message.Edit(
                    "Mensagem atualizada.",
                    invalidEditorUserId,
                    editedAtUtc));

        // Assert
        Assert.Equal("editorUserId", exception.ParamName);
        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Edit_ShouldThrowAndPreserveMessage_WhenContentIsInvalid(
        string? invalidContent)
    {
        // Arrange
        var message = CreateMessage();
        var editedAtUtc = message.CreatedAt.AddMinutes(3);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => message.Edit(
                invalidContent!,
                editorUserId: 20,
                editedAtUtc: editedAtUtc));

        // Assert
        Assert.Equal("content", exception.ParamName);
        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }

    [Fact]
    public void Edit_ShouldThrow_WhenEditedAtIsBeforeCreatedAt()
    {
        // Arrange
        var message = CreateMessage();
        var editedAtUtc = message.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => message.Edit(
                    "Mensagem atualizada.",
                    editorUserId: 20,
                    editedAtUtc: editedAtUtc));

        // Assert
        Assert.Equal("editedAtUtc", exception.ParamName);
        Assert.Equal("Mensagem original.", message.Content);
    }

    [Fact]
    public void Edit_ShouldThrow_WhenEditedAtIsNotUtc()
    {
        // Arrange
        var message = CreateMessage();

        var editedAtLocal = DateTime.SpecifyKind(
            message.CreatedAt.AddMinutes(3),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => message.Edit(
                "Mensagem atualizada.",
                editorUserId: 20,
                editedAtUtc: editedAtLocal));

        // Assert
        Assert.Equal("editedAtUtc", exception.ParamName);
        Assert.Equal("Mensagem original.", message.Content);
    }

    [Fact]
    public void Edit_ShouldThrow_WhenMessageIsExpired()
    {
        // Arrange
        var message = CreateMessage();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Edit(
                "Mensagem atualizada.",
                editorUserId: 20,
                editedAtUtc: message.ExpiresAt));

        // Assert
        Assert.Equal(
            "Uma mensagem expirada não pode ser editada.",
            exception.Message);

        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }

    [Fact]
    public void Delete_ShouldMarkMessageAsDeleted_WhenAuthorDeletesWithinWindow()
    {
        // Arrange
        var message = CreateMessage();
        var deletedAtUtc = message.CreatedAt.AddMinutes(3);

        // Act
        message.Delete(
            deletedByUserId: 20,
            deletedAtUtc: deletedAtUtc);

        // Assert
        Assert.True(message.IsDeleted);
        Assert.Equal(deletedAtUtc, message.DeletedAt);
        Assert.Equal(20, message.DeletedByUserId);
        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }

    [Fact]
    public void Delete_ShouldAllowDeletionExactlyAtWindowLimit()
    {
        // Arrange
        var message = CreateMessage();

        var deletedAtUtc =
            message.CreatedAt.Add(ChatMessage.EditWindow);

        // Act
        message.Delete(
            deletedByUserId: 20,
            deletedAtUtc: deletedAtUtc);

        // Assert
        Assert.True(message.IsDeleted);
        Assert.Equal(deletedAtUtc, message.DeletedAt);
        Assert.Equal(20, message.DeletedByUserId);
    }

    [Fact]
    public void Delete_ShouldThrowAndPreserveMessage_WhenWindowHasExpired()
    {
        // Arrange
        var message = CreateMessage();

        var deletedAtUtc = message.CreatedAt
            .Add(ChatMessage.EditWindow)
            .AddTicks(1);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Delete(
                deletedByUserId: 20,
                deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal(
            "O prazo de 5 minutos para remover a mensagem foi encerrado.",
            exception.Message);

        Assert.False(message.IsDeleted);
        Assert.Null(message.DeletedAt);
        Assert.Null(message.DeletedByUserId);
    }

    [Fact]
    public void Delete_ShouldThrowAndPreserveMessage_WhenUserIsNotAuthor()
    {
        // Arrange
        var message = CreateMessage();
        var deletedAtUtc = message.CreatedAt.AddMinutes(3);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Delete(
                deletedByUserId: 30,
                deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal(
            "Somente o autor pode remover a mensagem.",
            exception.Message);

        Assert.False(message.IsDeleted);
        Assert.Null(message.DeletedAt);
        Assert.Null(message.DeletedByUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Delete_ShouldThrow_WhenDeletedByUserIdIsNotPositive(
        long invalidDeletedByUserId)
    {
        // Arrange
        var message = CreateMessage();
        var deletedAtUtc = message.CreatedAt.AddMinutes(3);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => message.Delete(
                    invalidDeletedByUserId,
                    deletedAtUtc));

        // Assert
        Assert.Equal("deletedByUserId", exception.ParamName);
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldThrow_WhenDeletedAtIsBeforeCreatedAt()
    {
        // Arrange
        var message = CreateMessage();
        var deletedAtUtc = message.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => message.Delete(
                    deletedByUserId: 20,
                    deletedAtUtc: deletedAtUtc));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldThrow_WhenDeletedAtIsNotUtc()
    {
        // Arrange
        var message = CreateMessage();

        var deletedAtLocal = DateTime.SpecifyKind(
            message.CreatedAt.AddMinutes(3),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => message.Delete(
                deletedByUserId: 20,
                deletedAtUtc: deletedAtLocal));

        // Assert
        Assert.Equal("deletedAtUtc", exception.ParamName);
        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldThrow_WhenMessageIsExpired()
    {
        // Arrange
        var message = CreateMessage();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Delete(
                deletedByUserId: 20,
                deletedAtUtc: message.ExpiresAt));

        // Assert
        Assert.Equal(
            "Uma mensagem expirada não pode ser removida pelo autor.",
            exception.Message);

        Assert.False(message.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldPreserveFirstDeletion_WhenMessageIsAlreadyDeleted()
    {
        // Arrange
        var message = CreateMessage();

        var firstDeletedAtUtc =
            message.CreatedAt.AddMinutes(2);

        message.Delete(
            deletedByUserId: 20,
            deletedAtUtc: firstDeletedAtUtc);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Delete(
                deletedByUserId: 20,
                deletedAtUtc: message.CreatedAt.AddMinutes(3)));

        // Assert
        Assert.Equal(
            "Uma mensagem removida não pode ser removida novamente.",
            exception.Message);

        Assert.Equal(firstDeletedAtUtc, message.DeletedAt);
        Assert.Equal(20, message.DeletedByUserId);
    }

    [Fact]
    public void Edit_ShouldThrow_WhenMessageIsDeleted()
    {
        // Arrange
        var message = CreateMessage();

        message.Delete(
            deletedByUserId: 20,
            deletedAtUtc: message.CreatedAt.AddMinutes(2));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => message.Edit(
                "Tentativa de edição.",
                editorUserId: 20,
                editedAtUtc: message.CreatedAt.AddMinutes(3)));

        // Assert
        Assert.Equal(
            "Uma mensagem removida não pode ser editada.",
            exception.Message);

        Assert.Equal("Mensagem original.", message.Content);
        Assert.Null(message.UpdatedAt);
    }
}
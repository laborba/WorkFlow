using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Entities;

public class NotificationTests
{
    [Fact]
    public void Constructor_ShouldCreateUnreadNotification_WhenDataIsValid()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var notification = new Notification(
            recipientUserId: 10,
            type: NotificationType.TaskAssigned,
            title: "  Nova tarefa  ",
            message: "  Uma tarefa foi atribuída a você.  ",
            actorUserId: 20,
            resourceType: NotificationResourceType.ProjectTask,
            resourceId: 30);

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(10, notification.RecipientUserId);
        Assert.Equal(20, notification.ActorUserId);
        Assert.Equal(
            NotificationType.TaskAssigned,
            notification.Type);

        Assert.Equal("Nova tarefa", notification.Title);
        Assert.Equal(
            "Uma tarefa foi atribuída a você.",
            notification.Message);

        Assert.Equal(
            NotificationResourceType.ProjectTask,
            notification.ResourceType);

        Assert.Equal(30, notification.ResourceId);

        Assert.InRange(
            notification.CreatedAt,
            beforeCreation,
            afterCreation);

        Assert.Null(notification.ReadAt);
        Assert.Null(notification.DismissedAt);
        Assert.False(notification.IsRead);
        Assert.False(notification.IsDismissed);
    }

    [Fact]
    public void Constructor_ShouldAllowNotificationWithoutActorAndResource()
    {
        // Act
        var notification = new Notification(
            recipientUserId: 10,
            type: NotificationType.TaskOverdue,
            title: "Tarefa atrasada",
            message: "Existe uma tarefa atrasada.");

        // Assert
        Assert.Null(notification.ActorUserId);
        Assert.Null(notification.ResourceType);
        Assert.Null(notification.ResourceId);
        Assert.False(notification.IsRead);
        Assert.False(notification.IsDismissed);
    }

    [Fact]
    public void Constructor_ShouldAllowActorToBeRecipient()
    {
        // Act
        var notification = new Notification(
            recipientUserId: 10,
            type: NotificationType.TaskAssigned,
            title: "Tarefa atribuída",
            message: "A tarefa foi atribuída.",
            actorUserId: 10,
            resourceType: NotificationResourceType.ProjectTask,
            resourceId: 30);

        // Assert
        Assert.Equal(
            notification.RecipientUserId,
            notification.ActorUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenRecipientUserIdIsNotPositive(
        long invalidRecipientUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Notification(
                invalidRecipientUserId,
                NotificationType.TaskAssigned,
                "Título",
                "Mensagem"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenActorUserIdIsNotPositive(
        long invalidActorUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Notification(
                recipientUserId: 10,
                type: NotificationType.TaskAssigned,
                title: "Título",
                message: "Mensagem",
                actorUserId: invalidActorUserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_ShouldThrow_WhenNotificationTypeIsInvalid(
        int invalidTypeValue)
    {
        var invalidType =
            (NotificationType)invalidTypeValue;

        Assert.Throws<ArgumentException>(
            () => new Notification(
                recipientUserId: 10,
                type: invalidType,
                title: "Título",
                message: "Mensagem"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenTitleIsInvalid(
        string? invalidTitle)
    {
        Assert.Throws<ArgumentException>(
            () => new Notification(
                recipientUserId: 10,
                type: NotificationType.TaskAssigned,
                title: invalidTitle!,
                message: "Mensagem"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenMessageIsInvalid(
        string? invalidMessage)
    {
        Assert.Throws<ArgumentException>(
            () => new Notification(
                recipientUserId: 10,
                type: NotificationType.TaskAssigned,
                title: "Título",
                message: invalidMessage!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_ShouldThrow_WhenResourceTypeIsInvalid(
        int invalidResourceTypeValue)
    {
        var invalidResourceType =
            (NotificationResourceType)invalidResourceTypeValue;

        Assert.Throws<ArgumentException>(
            () => new Notification(
                recipientUserId: 10,
                type: NotificationType.TaskAssigned,
                title: "Título",
                message: "Mensagem",
                resourceType: invalidResourceType,
                resourceId: 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenResourceIdIsNotPositive(
        long invalidResourceId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Notification(
                recipientUserId: 10,
                type: NotificationType.TaskAssigned,
                title: "Título",
                message: "Mensagem",
                resourceType:
                    NotificationResourceType.ProjectTask,
                resourceId: invalidResourceId));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenResourceTypeHasNoResourceId()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Notification(
                recipientUserId: 10,
                type: NotificationType.TaskAssigned,
                title: "Título",
                message: "Mensagem",
                resourceType:
                    NotificationResourceType.ProjectTask));

        Assert.Equal("resourceId", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenResourceIdHasNoResourceType()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Notification(
                recipientUserId: 10,
                type: NotificationType.TaskAssigned,
                title: "Título",
                message: "Mensagem",
                resourceId: 30));

        Assert.Equal("resourceType", exception.ParamName);
    }

    [Fact]
    public void MarkAsRead_ShouldSetReadAt_WhenRecipientReadsNotification()
    {
        // Arrange
        var notification = CreateNotification();
        var readAtUtc = notification.CreatedAt.AddMinutes(5);

        // Act
        notification.MarkAsRead(
            userId: 10,
            readAtUtc: readAtUtc);

        // Assert
        Assert.True(notification.IsRead);
        Assert.Equal(readAtUtc, notification.ReadAt);
        Assert.False(notification.IsDismissed);
    }

    [Fact]
    public void MarkAsRead_ShouldThrow_WhenUserIsNotRecipient()
    {
        // Arrange
        var notification = CreateNotification();
        var readAtUtc = notification.CreatedAt.AddMinutes(5);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => notification.MarkAsRead(
                userId: 20,
                readAtUtc: readAtUtc));

        // Assert
        Assert.Equal(
            "Somente o destinatário pode marcar a notificação como lida.",
            exception.Message);

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MarkAsRead_ShouldThrow_WhenUserIdIsNotPositive(
        long invalidUserId)
    {
        // Arrange
        var notification = CreateNotification();
        var readAtUtc = notification.CreatedAt.AddMinutes(5);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => notification.MarkAsRead(
                    invalidUserId,
                    readAtUtc));

        // Assert
        Assert.Equal("userId", exception.ParamName);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_ShouldThrow_WhenReadAtIsBeforeCreatedAt()
    {
        // Arrange
        var notification = CreateNotification();
        var readAtUtc = notification.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => notification.MarkAsRead(
                    userId: 10,
                    readAtUtc: readAtUtc));

        // Assert
        Assert.Equal("readAtUtc", exception.ParamName);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_ShouldThrow_WhenReadAtIsNotUtc()
    {
        // Arrange
        var notification = CreateNotification();

        var readAtLocal = DateTime.SpecifyKind(
            notification.CreatedAt.AddMinutes(5),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => notification.MarkAsRead(
                userId: 10,
                readAtUtc: readAtLocal));

        // Assert
        Assert.Equal("readAtUtc", exception.ParamName);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_ShouldPreserveFirstReadAt_WhenCalledAgain()
    {
        // Arrange
        var notification = CreateNotification();

        var firstReadAtUtc =
            notification.CreatedAt.AddMinutes(5);

        notification.MarkAsRead(
            userId: 10,
            readAtUtc: firstReadAtUtc);

        // Act
        notification.MarkAsRead(
            userId: 10,
            readAtUtc: notification.CreatedAt.AddMinutes(10));

        // Assert
        Assert.True(notification.IsRead);
        Assert.Equal(firstReadAtUtc, notification.ReadAt);
    }

    [Fact]
    public void Dismiss_ShouldSetDismissedAt_WhenRecipientDismissesNotification()
    {
        // Arrange
        var notification = CreateNotification();

        var dismissedAtUtc =
            notification.CreatedAt.AddMinutes(5);

        // Act
        notification.Dismiss(
            userId: 10,
            dismissedAtUtc: dismissedAtUtc);

        // Assert
        Assert.True(notification.IsDismissed);
        Assert.Equal(
            dismissedAtUtc,
            notification.DismissedAt);

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public void Dismiss_ShouldPreserveReadState_WhenNotificationWasAlreadyRead()
    {
        // Arrange
        var notification = CreateNotification();

        var readAtUtc =
            notification.CreatedAt.AddMinutes(2);

        var dismissedAtUtc =
            notification.CreatedAt.AddMinutes(5);

        notification.MarkAsRead(
            userId: 10,
            readAtUtc: readAtUtc);

        // Act
        notification.Dismiss(
            userId: 10,
            dismissedAtUtc: dismissedAtUtc);

        // Assert
        Assert.True(notification.IsRead);
        Assert.Equal(readAtUtc, notification.ReadAt);

        Assert.True(notification.IsDismissed);
        Assert.Equal(
            dismissedAtUtc,
            notification.DismissedAt);
    }

    [Fact]
    public void Dismiss_ShouldThrow_WhenUserIsNotRecipient()
    {
        // Arrange
        var notification = CreateNotification();

        var dismissedAtUtc =
            notification.CreatedAt.AddMinutes(5);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => notification.Dismiss(
                userId: 20,
                dismissedAtUtc: dismissedAtUtc));

        // Assert
        Assert.Equal(
            "Somente o destinatário pode dispensar a notificação.",
            exception.Message);

        Assert.False(notification.IsDismissed);
        Assert.Null(notification.DismissedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Dismiss_ShouldThrow_WhenUserIdIsNotPositive(
        long invalidUserId)
    {
        // Arrange
        var notification = CreateNotification();

        var dismissedAtUtc =
            notification.CreatedAt.AddMinutes(5);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => notification.Dismiss(
                    invalidUserId,
                    dismissedAtUtc));

        // Assert
        Assert.Equal("userId", exception.ParamName);
        Assert.False(notification.IsDismissed);
    }

    [Fact]
    public void Dismiss_ShouldThrow_WhenDismissedAtIsBeforeCreatedAt()
    {
        // Arrange
        var notification = CreateNotification();

        var dismissedAtUtc =
            notification.CreatedAt.AddTicks(-1);

        // Act
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => notification.Dismiss(
                    userId: 10,
                    dismissedAtUtc: dismissedAtUtc));

        // Assert
        Assert.Equal(
            "dismissedAtUtc",
            exception.ParamName);

        Assert.False(notification.IsDismissed);
    }

    [Fact]
    public void Dismiss_ShouldThrow_WhenDismissedAtIsNotUtc()
    {
        // Arrange
        var notification = CreateNotification();

        var dismissedAtLocal = DateTime.SpecifyKind(
            notification.CreatedAt.AddMinutes(5),
            DateTimeKind.Local);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => notification.Dismiss(
                userId: 10,
                dismissedAtUtc: dismissedAtLocal));

        // Assert
        Assert.Equal(
            "dismissedAtUtc",
            exception.ParamName);

        Assert.False(notification.IsDismissed);
    }

    [Fact]
    public void Dismiss_ShouldPreserveFirstDismissedAt_WhenCalledAgain()
    {
        // Arrange
        var notification = CreateNotification();

        var firstDismissedAtUtc =
            notification.CreatedAt.AddMinutes(5);

        notification.Dismiss(
            userId: 10,
            dismissedAtUtc: firstDismissedAtUtc);

        // Act
        notification.Dismiss(
            userId: 10,
            dismissedAtUtc:
                notification.CreatedAt.AddMinutes(10));

        // Assert
        Assert.True(notification.IsDismissed);
        Assert.Equal(
            firstDismissedAtUtc,
            notification.DismissedAt);
    }

    [Fact]
    public void IsRetentionExpiredAt_ShouldReturnFalse_BeforeRetentionLimit()
    {
        var notification = CreateNotification();

        var utcNow = notification.CreatedAt
            .Add(Notification.RetentionPeriod)
            .AddTicks(-1);

        var result =
            notification.IsRetentionExpiredAt(utcNow);

        Assert.False(result);
    }

    [Fact]
    public void IsRetentionExpiredAt_ShouldReturnTrue_AtRetentionLimit()
    {
        var notification = CreateNotification();

        var utcNow = notification.CreatedAt
            .Add(Notification.RetentionPeriod);

        var result =
            notification.IsRetentionExpiredAt(utcNow);

        Assert.True(result);
    }

    [Fact]
    public void IsRetentionExpiredAt_ShouldReturnTrue_AfterRetentionLimit()
    {
        var notification = CreateNotification();

        var utcNow = notification.CreatedAt
            .Add(Notification.RetentionPeriod)
            .AddDays(1);

        var result =
            notification.IsRetentionExpiredAt(utcNow);

        Assert.True(result);
    }

    [Fact]
    public void IsRetentionExpiredAt_ShouldThrow_WhenDateIsNotUtc()
    {
        var notification = CreateNotification();

        var localDate = DateTime.SpecifyKind(
            notification.CreatedAt.AddDays(30),
            DateTimeKind.Local);

        var exception = Assert.Throws<ArgumentException>(
            () => notification.IsRetentionExpiredAt(localDate));

        Assert.Equal("utcNow", exception.ParamName);
    }

    [Fact]
    public void IsRetentionExpiredAt_ShouldThrow_WhenDateIsBeforeCreatedAt()
    {
        var notification = CreateNotification();
        var utcNow = notification.CreatedAt.AddTicks(-1);

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => notification.IsRetentionExpiredAt(utcNow));

        Assert.Equal("utcNow", exception.ParamName);
    }

    private static Notification CreateNotification()
    {
        return new Notification(
            recipientUserId: 10,
            type: NotificationType.TaskAssigned,
            title: "Nova tarefa",
            message: "Uma tarefa foi atribuída a você.",
            actorUserId: 20,
            resourceType: NotificationResourceType.ProjectTask,
            resourceId: 30);
    }


}
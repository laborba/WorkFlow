using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Domain.Entities;

public class ProjectMemberTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveMember_WhenIdsAreValid()
    {
        var member = new ProjectMember(
            projectId: 1,
            userId: 20,
            addedByUserId: 10);

        Assert.Equal(1, member.ProjectId);
        Assert.Equal(20, member.UserId);
        Assert.Equal(10, member.AddedByUserId);
        Assert.True(member.IsActive);
        Assert.Null(member.RemovedAt);
        Assert.NotEqual(default, member.AddedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenProjectIdIsNotPositive(
        long invalidProjectId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectMember(
                projectId: invalidProjectId,
                userId: 20,
                addedByUserId: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenUserIdIsNotPositive(
        long invalidUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectMember(
                projectId: 1,
                userId: invalidUserId,
                addedByUserId: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenAddedByUserIdIsNotPositive(
        long invalidAddedByUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectMember(
                projectId: 1,
                userId: 20,
                addedByUserId: invalidAddedByUserId));
    }

    [Fact]
    public void Remove_ShouldDeactivateMemberAndSetRemovedAt()
    {
        var member = CreateMember();

        member.Remove();

        Assert.False(member.IsActive);
        Assert.NotNull(member.RemovedAt);
    }

    [Fact]
    public void Remove_ShouldThrowAndPreserveRemovedAt_WhenMemberIsAlreadyRemoved()
    {
        var member = CreateMember();
        member.Remove();

        var removedAtBeforeSecondRemoval = member.RemovedAt;

        Assert.Throws<InvalidOperationException>(() =>
            member.Remove());

        Assert.False(member.IsActive);
        Assert.Equal(
            removedAtBeforeSecondRemoval,
            member.RemovedAt);
    }

    private static ProjectMember CreateMember()
    {
        return new ProjectMember(
            projectId: 1,
            userId: 20,
            addedByUserId: 10);
    }
}
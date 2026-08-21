using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Entities;

public class ProjectMemberPermissionTests
{
    [Fact]
    public void Constructor_ShouldCreateActivePermission_WhenDataIsValid()
    {
        var memberPermission = new ProjectMemberPermission(
            projectMemberId: 25,
            permission: ProjectPermission.EditProject,
            grantedByUserId: 10);

        Assert.Equal(25, memberPermission.ProjectMemberId);
        Assert.Equal(
            ProjectPermission.EditProject,
            memberPermission.Permission);
        Assert.Equal(10, memberPermission.GrantedByUserId);
        Assert.NotEqual(default, memberPermission.GrantedAt);
        Assert.True(memberPermission.IsActive);
        Assert.Null(memberPermission.RevokedAt);
        Assert.Null(memberPermission.RevokedByUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenProjectMemberIdIsNotPositive(
        long invalidProjectMemberId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectMemberPermission(
                projectMemberId: invalidProjectMemberId,
                permission: ProjectPermission.EditProject,
                grantedByUserId: 10));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(999)]
    public void Constructor_ShouldThrow_WhenPermissionIsInvalid(
        int invalidPermissionValue)
    {
        var invalidPermission =
            (ProjectPermission)invalidPermissionValue;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectMemberPermission(
                projectMemberId: 25,
                permission: invalidPermission,
                grantedByUserId: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenGrantedByUserIdIsNotPositive(
        long invalidGrantedByUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProjectMemberPermission(
                projectMemberId: 25,
                permission: ProjectPermission.EditProject,
                grantedByUserId: invalidGrantedByUserId));
    }

    [Fact]
    public void Revoke_ShouldDeactivatePermissionAndRegisterRevocation()
    {
        var memberPermission = CreatePermission();

        memberPermission.Revoke(revokedByUserId: 15);

        Assert.False(memberPermission.IsActive);
        Assert.NotNull(memberPermission.RevokedAt);
        Assert.Equal(15, memberPermission.RevokedByUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Revoke_ShouldThrowAndPreservePermission_WhenUserIdIsNotPositive(
        long invalidRevokedByUserId)
    {
        var memberPermission = CreatePermission();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            memberPermission.Revoke(invalidRevokedByUserId));

        Assert.True(memberPermission.IsActive);
        Assert.Null(memberPermission.RevokedAt);
        Assert.Null(memberPermission.RevokedByUserId);
    }

    [Fact]
    public void Revoke_ShouldThrowAndPreserveRevocation_WhenAlreadyRevoked()
    {
        var memberPermission = CreatePermission();
        memberPermission.Revoke(revokedByUserId: 15);

        var revokedAtBeforeSecondAttempt =
            memberPermission.RevokedAt;

        var revokedByUserIdBeforeSecondAttempt =
            memberPermission.RevokedByUserId;

        Assert.Throws<InvalidOperationException>(() =>
            memberPermission.Revoke(revokedByUserId: 20));

        Assert.False(memberPermission.IsActive);
        Assert.Equal(
            revokedAtBeforeSecondAttempt,
            memberPermission.RevokedAt);
        Assert.Equal(
            revokedByUserIdBeforeSecondAttempt,
            memberPermission.RevokedByUserId);
    }

    private static ProjectMemberPermission CreatePermission()
    {
        return new ProjectMemberPermission(
            projectMemberId: 25,
            permission: ProjectPermission.EditProject,
            grantedByUserId: 10);
    }
}
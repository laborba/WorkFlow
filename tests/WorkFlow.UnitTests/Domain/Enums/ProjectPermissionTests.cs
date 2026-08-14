using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.Domain.Enums;

public class ProjectPermissionTests
{
    [Fact]
    public void Zero_ShouldNotRepresentValidPermission()
    {
        var permission = (ProjectPermission)0;

        Assert.False(
            Enum.IsDefined(typeof(ProjectPermission), permission));
    }

    [Fact]
    public void Values_ShouldBePositiveAndUnique()
    {
        var values = Enum
            .GetValues<ProjectPermission>()
            .Select(permission => (int)permission)
            .ToArray();

        Assert.All(
            values,
            value => Assert.True(value > 0));

        Assert.Equal(
            values.Length,
            values.Distinct().Count());
    }
}
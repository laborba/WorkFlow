using WorkFlow.Application.Abstractions.Security;
using WorkFlow.Domain.Entities;

namespace WorkFlow.UnitTests.Application.Authentication.Fakes;

internal sealed class FakeAccessTokenGenerator :
    IAccessTokenGenerator
{
    public string AccessTokenToReturn { get; set; } =
        "fake-access-token";

    public DateTime ExpiresAtToReturn { get; set; } =
        DateTime.UtcNow.AddHours(1);

    public User? UserReceived { get; private set; }

    public Guid? TenantPublicIdReceived { get; private set; }

    public (string AccessToken, DateTime ExpiresAt) Generate(
        User user,
        Guid tenantPublicId)
    {
        UserReceived =
            user;

        TenantPublicIdReceived =
            tenantPublicId;

        return (
            AccessTokenToReturn,
            ExpiresAtToReturn);
    }
}
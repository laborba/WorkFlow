using WorkFlow.Domain.Entities;

namespace WorkFlow.Application.Abstractions.Security;

public interface IAccessTokenGenerator
{
    (string AccessToken, DateTime ExpiresAt) Generate(
        User user,
        Guid? tenantPublicId);
}
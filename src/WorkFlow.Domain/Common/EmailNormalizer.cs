namespace WorkFlow.Domain.Common;

public static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "O e-mail não pode estar vazio.",
                nameof(email));
        }

        return email
            .Trim()
            .ToUpperInvariant();
    }
}
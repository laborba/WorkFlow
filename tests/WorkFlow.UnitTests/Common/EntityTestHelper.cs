using System.Reflection;

namespace WorkFlow.UnitTests.Common;

internal static class EntityTestHelper
{
    public static void SetId<TEntity>(
        TEntity entity,
        long id)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "O identificador deve ser maior que zero.");
        }

        var idProperty =
            typeof(TEntity).GetProperty(
                "Id",
                BindingFlags.Instance |
                BindingFlags.Public);

        if (idProperty is null)
        {
            throw new InvalidOperationException(
                $"A entidade {typeof(TEntity).Name} não possui a propriedade Id.");
        }

        idProperty.SetValue(
            entity,
            id);
    }
}
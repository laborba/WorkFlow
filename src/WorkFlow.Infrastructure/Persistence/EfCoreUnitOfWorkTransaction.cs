using Microsoft.EntityFrameworkCore.Storage;
using WorkFlow.Application.Abstractions.Persistence;

namespace WorkFlow.Infrastructure.Persistence;

public sealed class EfCoreUnitOfWorkTransaction :
    IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfCoreUnitOfWorkTransaction(
        IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(
            cancellationToken);
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}
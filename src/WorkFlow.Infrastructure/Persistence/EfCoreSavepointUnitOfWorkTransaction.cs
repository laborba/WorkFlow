using Microsoft.EntityFrameworkCore.Storage;
using WorkFlow.Application.Abstractions.Persistence;

namespace WorkFlow.Infrastructure.Persistence;

public sealed class EfCoreSavepointUnitOfWorkTransaction :
    IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;
    private readonly string _savepointName;

    private bool _completed;

    public EfCoreSavepointUnitOfWorkTransaction(
        IDbContextTransaction transaction,
        string savepointName)
    {
        _transaction =
            transaction;

        _savepointName =
            savepointName;
    }

    public async Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        if (_completed)
            return;

        await _transaction.ReleaseSavepointAsync(
            _savepointName,
            cancellationToken);

        _completed = true;
    }

    public async Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        if (_completed)
            return;

        await _transaction.RollbackToSavepointAsync(
            _savepointName,
            cancellationToken);

        await _transaction.ReleaseSavepointAsync(
            _savepointName,
            cancellationToken);

        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_completed)
            return;

        await _transaction.RollbackToSavepointAsync(
            _savepointName);

        await _transaction.ReleaseSavepointAsync(
            _savepointName);

        _completed = true;
    }
}
using WorkFlow.Application.Abstractions.Persistence;

namespace WorkFlow.UnitTests.Common.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int BeginTransactionCallCount { get; private set; }

    public int SaveChangesCallCount { get; private set; }

    public int CommitCallCount { get; private set; }

    public int RollbackCallCount { get; private set; }

    public Action<int>? OnSaveChanges { get; set; }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        BeginTransactionCallCount++;

        IUnitOfWorkTransaction transaction =
            new FakeTransaction(this);

        return Task.FromResult(transaction);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        OnSaveChanges?.Invoke(
            SaveChangesCallCount);

        return Task.FromResult(1);
    }

    private sealed class FakeTransaction :
        IUnitOfWorkTransaction
    {
        private readonly FakeUnitOfWork _unitOfWork;

        public FakeTransaction(
            FakeUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            _unitOfWork.CommitCallCount++;

            return Task.CompletedTask;
        }

        public Task RollbackAsync(
            CancellationToken cancellationToken = default)
        {
            _unitOfWork.RollbackCallCount++;

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
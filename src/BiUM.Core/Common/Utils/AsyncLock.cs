public class AsyncLock
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public async ValueTask<AsyncLockReleaser> LockAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new AsyncLockReleaser(_semaphore);
    }

    public struct AsyncLockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public AsyncLockReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
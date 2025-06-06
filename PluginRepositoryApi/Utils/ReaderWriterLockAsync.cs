namespace PluginRepositoryApi.Utils;

public class AsyncReaderWriterLock {
    private readonly SemaphoreSlim _readLock = new(1, 1);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private int _readers = 0;

    public async ValueTask<IAsyncDisposable> ReaderLockAsync() {
        await _readLock.WaitAsync();
        try {
            if (_readers++ == 0) {
                await _writeLock.WaitAsync();
            }
        } finally {
            _readLock.Release();
        }

        return new AsyncReleaser(this, isWriter: false);
    }

    public async ValueTask<IAsyncDisposable> WriterLockAsync() {
        await _writeLock.WaitAsync();
        return new AsyncReleaser(this, isWriter: true);
    }

    private sealed class AsyncReleaser : IAsyncDisposable {
        private readonly AsyncReaderWriterLock _lock;
        private readonly bool _isWriter;
        private bool _disposed;

        public AsyncReleaser(AsyncReaderWriterLock l, bool isWriter) {
            _lock = l;
            _isWriter = isWriter;
        }

        public async ValueTask DisposeAsync() {
            if (_disposed) return;
            _disposed = true;

            if (_isWriter) {
                _lock._writeLock.Release();
            } else {
                await _lock._readLock.WaitAsync();
                try {
                    if (--_lock._readers == 0) {
                        _lock._writeLock.Release();
                    }
                } finally {
                    _lock._readLock.Release();
                }
            }
        }
    }
}

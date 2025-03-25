// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Async.Threading
{
    /// <summary>
    /// Provides an asynchronous lock mechanism.
    /// </summary>
    public class AsyncLock
    {
        private readonly Task<Releaser> _releaserTask;
        private TaskCompletionSource<Releaser> _releaserTcs;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLock"/> class.
        /// </summary>
        public AsyncLock()
        {
            _releaserTcs = new TaskCompletionSource<Releaser>();
            _releaserTcs.SetResult(new Releaser(this));
            _releaserTask = _releaserTcs.Task;
        }

        /// <summary>
        /// Acquires the lock asynchronously.
        /// </summary>
        /// <returns>A task that represents the acquisition of the lock. The result of the task is a <see cref="Releaser"/> that releases the lock when disposed.</returns>
        public Task<Releaser> LockAsync()
        {
            lock (_lock)
            {
                if (_releaserTcs != null)
                {
                    var previousTcs = _releaserTcs;
                    _releaserTcs = new TaskCompletionSource<Releaser>();
                    return previousTcs.Task;
                }

                return _releaserTask;
            }
        }

        /// <summary>
        /// Represents a releaser that releases the lock when disposed.
        /// </summary>
        public struct Releaser : IDisposable
        {
            private readonly AsyncLock _asyncLock;

            /// <summary>
            /// Initializes a new instance of the <see cref="Releaser"/> struct.
            /// </summary>
            /// <param name="asyncLock">The <see cref="AsyncLock"/> to release.</param>
            internal Releaser(AsyncLock asyncLock)
            {
                _asyncLock = asyncLock;
            }

            /// <summary>
            /// Releases the lock.
            /// </summary>
            public void Dispose()
            {
                if (_asyncLock != null)
                {
                    _asyncLock.Release();
                }
            }
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        private void Release()
        {
            lock (_lock)
            {
                if (_releaserTcs != null)
                {
                    _releaserTcs.SetResult(new Releaser(this));
                    _releaserTcs = null;
                }
            }
        }
    }
}

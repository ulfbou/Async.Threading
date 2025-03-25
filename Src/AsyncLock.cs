// Copyright (c) Async Framework projects. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using static Async.Threading.AsyncLock;
using System.Threading;

namespace Async.Threading
{
    /// <summary>
    /// Provides an asynchronous lock mechanism.
    /// </summary>
    public class AsyncLock
    {
        private Task<Releaser> _releaserTask;
        private TaskCompletionSource<Releaser> _releaserTcs;
        private readonly Queue<TaskCompletionSource<Releaser>> _pendingReleasers = new Queue<TaskCompletionSource<Releaser>>();
        private readonly object _lock = new object();
        private bool _isLocked = false;

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
        public async Task<Releaser> LockAsync(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<Releaser> tcs = new TaskCompletionSource<Releaser>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_lock)
            {
                if (!_isLocked)
                {
                    _isLocked = true;
                    return new Releaser(this);
                }
                else
                {
                    _pendingReleasers.Enqueue(tcs);
                }
            }

            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return await tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Represents a releaser that releases the lock when disposed.
        /// </summary>
        public struct Releaser : IAsyncDisposable
        {
            private readonly AsyncLock? _asyncLock;

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
            public ValueTask DisposeAsync()
            {
                _asyncLock?.Release();
                return ValueTask.CompletedTask;
            }
        }

        /// <summary>
        /// Releases the lock 
        /// </summary>
        internal void Release()
        {
            lock (_lock)
            {
                if (_pendingReleasers.Count > 0)
                {
                    var nextReleaserTcs = _pendingReleasers.Peek(); // Peek instead of dequeue
                    if (!nextReleaserTcs.Task.IsCompleted)
                    {
                        _pendingReleasers.Dequeue(); // remove from queue now that it is used.
                        nextReleaserTcs.SetResult(new Releaser(this));
                    }
                    else
                    {
                        _pendingReleasers.Dequeue(); // remove canceled task from queue.
                        Release(); // recursively call release to handle next one.
                        return; // exit current release call.
                    }
                }
                else
                {
                    _isLocked = false;
                }
            }
        }
    }
}

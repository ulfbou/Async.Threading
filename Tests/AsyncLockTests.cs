// Copyright (c) Async Framework projects. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;

using Xunit;

namespace Async.Threading.Tests
{
    public class AsyncLockTests
    {
        [Fact]
        public async Task LockAsync_ShouldAcquireAndReleaseLock()
        {
            var asyncLock = new AsyncLock();

            // Acquire the lock
            var releaser = await asyncLock.LockAsync();

            // Try to acquire the lock again, should not be able to until released
            var lockTask = asyncLock.LockAsync();
            lockTask.IsCompleted.Should().BeFalse();

            // Release the lock
            await releaser.DisposeAsync();

            // Now the lock should be acquired
            var newReleaser = await lockTask;
            newReleaser.Should().NotBeNull();
        }

        [Fact]
        public async Task LockAsync_ShouldAllowMultipleSequentialLocks()
        {
            var asyncLock = new AsyncLock();

            // Acquire the lock
            var releaser1 = await asyncLock.LockAsync();

            // Try to acquire the lock again, should not be able to until released
            var lockTask1 = asyncLock.LockAsync();
            lockTask1.IsCompleted.Should().BeFalse();

            // Release the first lock
            await releaser1.DisposeAsync();

            // Now the lock should be acquired by the second task
            var releaser2 = await lockTask1;
            releaser2.Should().NotBeNull();

            // Try to acquire the lock again, should not be able to until released
            var lockTask2 = asyncLock.LockAsync();
            lockTask2.IsCompleted.Should().BeFalse();

            // Release the second lock
            await releaser2.DisposeAsync();

            // Now the lock should be acquired by the third task
            var releaser3 = await lockTask2;
            releaser3.Should().NotBeNull();
        }

        [Fact]
        public async Task LockAsync_ShouldBeCancelable()
        {
            var asyncLock = new AsyncLock();
            var cts = new CancellationTokenSource();

            // Acquire the lock
            var releaser = await asyncLock.LockAsync();

            // Try to acquire the lock again with cancellation token
            var lockTask = asyncLock.LockAsync(cts.Token);
            lockTask.IsCompleted.Should().BeFalse();

            // Cancel the lock acquisition
            cts.Cancel();

            // The lock task should be canceled
            await Assert.ThrowsAsync<TaskCanceledException>(() => lockTask);

            // Release the lock
            await releaser.DisposeAsync();

            // Now the lock should be acquired by a new task
            var newReleaser = await asyncLock.LockAsync();
            newReleaser.Should().NotBeNull();
        }
    }
}

using System;
using System.Threading.Tasks;

namespace TorrentSwifter.Tasks
{
    /// <summary>
    /// An asynchronous delegate task.
    /// </summary>
    public sealed class DelegateTaskAsync : AsyncWorkTask
    {
        private readonly Func<Task> action;

        /// <summary>
        /// Creates a new asynchronous delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        public DelegateTaskAsync(Func<Task> action)
        {
            this.action = action;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        /// <returns>The work task.</returns>
        public override Task ExecuteAsync()
        {
            return action.Invoke();
        }
    }

    /// <summary>
    /// An asynchronous delegate task.
    /// </summary>
    public sealed class DelegateTaskAsync<T> : AsyncWorkTask
    {
        private readonly Func<T, Task> action;
        private readonly T arg;

        /// <summary>
        /// Creates a new asynchronous delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        /// <param name="arg">The argument.</param>
        public DelegateTaskAsync(Func<T, Task> action, T arg)
        {
            this.action = action;
            this.arg = arg;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        /// <returns>The work task.</returns>
        public override Task ExecuteAsync()
        {
            return action.Invoke(arg);
        }
    }

    /// <summary>
    /// An asynchronous delegate task.
    /// </summary>
    public sealed class DelegateTaskAsync<T1, T2> : AsyncWorkTask
    {
        private readonly Func<T1, T2, Task> action;
        private readonly T1 arg1;
        private readonly T2 arg2;

        /// <summary>
        /// Creates a new asynchronous delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        public DelegateTaskAsync(Func<T1, T2, Task> action, T1 arg1, T2 arg2)
        {
            this.action = action;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        /// <returns>The work task.</returns>
        public override Task ExecuteAsync()
        {
            return action.Invoke(arg1, arg2);
        }
    }

    /// <summary>
    /// An asynchronous delegate task.
    /// </summary>
    public sealed class DelegateTaskAsync<T1, T2, T3> : AsyncWorkTask
    {
        private readonly Func<T1, T2, T3, Task> action;
        private readonly T1 arg1;
        private readonly T2 arg2;
        private readonly T3 arg3;

        /// <summary>
        /// Creates a new asynchronous delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        public DelegateTaskAsync(Func<T1, T2, T3, Task> action, T1 arg1, T2 arg2, T3 arg3)
        {
            this.action = action;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        /// <returns>The work task.</returns>
        public override Task ExecuteAsync()
        {
            return action.Invoke(arg1, arg2, arg3);
        }
    }
}

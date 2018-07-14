using System;

namespace TorrentSwifter.Tasks
{
    /// <summary>
    /// A delegate task.
    /// </summary>
    public sealed class DelegateTask : IWorkTask
    {
        private readonly Action action;

        /// <summary>
        /// Creates a new delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        public DelegateTask(Action action)
        {
            this.action = action;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        public void Execute()
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// A delegate task.
    /// </summary>
    public sealed class DelegateTask<T> : IWorkTask
    {
        private readonly Action<T> action;
        private readonly T arg;

        /// <summary>
        /// Creates a new delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        /// <param name="arg">The argument.</param>
        public DelegateTask(Action<T> action, T arg)
        {
            this.action = action;
            this.arg = arg;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        public void Execute()
        {
            action.Invoke(arg);
        }
    }

    /// <summary>
    /// A delegate task.
    /// </summary>
    public sealed class DelegateTask<T1, T2> : IWorkTask
    {
        private readonly Action<T1, T2> action;
        private readonly T1 arg1;
        private readonly T2 arg2;

        /// <summary>
        /// Creates a new delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        public DelegateTask(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            this.action = action;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        public void Execute()
        {
            action.Invoke(arg1, arg2);
        }
    }

    /// <summary>
    /// A delegate task.
    /// </summary>
    public sealed class DelegateTask<T1, T2, T3> : IWorkTask
    {
        private readonly Action<T1, T2, T3> action;
        private readonly T1 arg1;
        private readonly T2 arg2;
        private readonly T3 arg3;

        /// <summary>
        /// Creates a new delegate task.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        public DelegateTask(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            this.action = action;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }

        /// <summary>
        /// Executes this task.
        /// </summary>
        public void Execute()
        {
            action.Invoke(arg1, arg2, arg3);
        }
    }
}

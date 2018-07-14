using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace TorrentSwifter.Collections
{
    /// <summary>
    /// A collection (list) that allows for concurrent multi-threaded usage.
    /// </summary>
    /// <typeparam name="T">The type of item in this list.</typeparam>
    public sealed class ConcurrentList<T> : ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        #region Enumerator
        [Serializable]
        private struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private T[] array;
            private int index;
            private T current;

            public T Current
            {
                get { return current; }
            }

            object IEnumerator.Current
            {
                get { return current; }
            }

            internal Enumerator(T[] array)
            {
                this.array = array;
                this.index = 0;
                this.current = default(T);
            }

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                if (index < array.Length)
                {
                    current = array[index];
                    ++index;
                    return true;
                }
                else
                {
                    current = default(T);
                    return false;
                }
            }

            void IEnumerator.Reset()
            {
                index = 0;
                current = default(T);
            }
        }
        #endregion

        #region Fields
        private readonly List<T> items;
        private readonly ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        #endregion

        #region Properties
        /// <summary>
        /// Gets the count of items in this collection.
        /// </summary>
        public int Count
        {
            get
            {
                readWriteLock.EnterReadLock();
                try
                {
                    return items.Count;
                }
                finally
                {
                    readWriteLock.ExitReadLock();
                }
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return null; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new concurrent list.
        /// </summary>
        public ConcurrentList()
        {
            items = new List<T>();
        }

        /// <summary>
        /// Creates a new concurrent list.
        /// </summary>
        /// <param name="capacity">The initial capacity of items to include in this list.</param>
        public ConcurrentList(int capacity)
        {
            items = new List<T>(capacity);
        }

        /// <summary>
        /// Creates a new concurrent list.
        /// </summary>
        /// <param name="collection">The collection if items to initially include in this list.</param>
        public ConcurrentList(IEnumerable<T> collection)
        {
            items = new List<T>(collection);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds an item to this list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            readWriteLock.EnterWriteLock();
            try
            {
                items.Add(item);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clears this collection.
        /// </summary>
        public void Clear()
        {
            readWriteLock.EnterWriteLock();
            try
            {
                items.Clear();
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns if this collection contains a specific item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>If the item is contained in the collection.</returns>
        public bool Contains(T item)
        {
            readWriteLock.EnterReadLock();
            try
            {
                return items.Contains(item);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies the items of this collection to an array.
        /// </summary>
        /// <param name="array">The array to copy items to.</param>
        public void CopyTo(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            else if (array.Length == 0)
                return;

            readWriteLock.EnterReadLock();
            try
            {
                items.CopyTo(array);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies the items of this collection to an array.
        /// </summary>
        /// <param name="array">The array to copy items to.</param>
        /// <param name="arrayIndex">The offset within the array to start copying to.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            else if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");
            else if (array.Length == 0)
                return;

            readWriteLock.EnterReadLock();
            try
            {
                items.CopyTo(array, arrayIndex);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies the items of this collection to an array.
        /// </summary>
        /// <param name="array">The array to copy items to.</param>
        /// <param name="arrayIndex">The offset within the array to start copying to.</param>
        /// <param name="count">The count of items to copy.</param>
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            else if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");
            else if (count < 0 || (arrayIndex + count) > array.Length)
                throw new ArgumentOutOfRangeException("count");
            else if (count == 0)
                return;

            readWriteLock.EnterReadLock();
            try
            {
                items.CopyTo(0, array, arrayIndex, count);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Attempts to remove an item from this collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>If the item was successfully removed.</returns>
        public bool Remove(T item)
        {
            readWriteLock.EnterWriteLock();
            try
            {
                return items.Remove(item);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Attempts to remove any item from this collection that matches the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate that decides which items to remove.</param>
        /// <param name="maxRemoveCount">The maximum count of items to remove. Zero or negative means no limit.</param>
        /// <returns>The count of items removed.</returns>
        public int RemoveAny(Predicate<T> predicate, int maxRemoveCount = 0)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            readWriteLock.EnterWriteLock();
            try
            {
                int removeCount = 0;
                int itemCount = items.Count;
                for (int i = 0; i < itemCount; i++)
                {
                    if (predicate.Invoke(items[i]))
                    {
                        items.RemoveAt(i);
                        --i;
                        --itemCount;
                        ++removeCount;

                        if (maxRemoveCount > 0 && removeCount >= maxRemoveCount)
                            break;
                    }
                }

                return removeCount;
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns an enumerator for this collection.
        /// This will take a snapshot of the collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            var itemsArray = ToArray();
            return new Enumerator(itemsArray);
        }

        /// <summary>
        /// Returns an array of all items in this collection.
        /// </summary>
        /// <returns>An array of items.</returns>
        public T[] ToArray()
        {
            readWriteLock.EnterReadLock();
            try
            {
                return items.ToArray();
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Iterates through all items in this collection and invokes a callback for each iteration with the iterated item.
        /// Each callback will have the possibility to break the iteration by returning false.
        /// </summary>
        /// <param name="iterationCallback">The callback for each iteration.</param>
        public void ForEach(Func<T, bool> iterationCallback)
        {
            if (iterationCallback == null)
                throw new ArgumentNullException("iterationCallback");

            readWriteLock.EnterReadLock();
            try
            {
                foreach (var item in items)
                {
                    if (!iterationCallback.Invoke(item))
                        break;
                }
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }
        #endregion

        #region Private Methods
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            else if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");
            else if (array.Length == 0)
                return;

            ICollection listCollection = items as ICollection;
            readWriteLock.EnterReadLock();
            try
            {
                listCollection.CopyTo(array, arrayIndex);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}

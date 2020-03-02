using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace iFactr.Data
{
    /// <summary>
    /// Represents a synchronization queue
    /// </summary>
    /// <typeparam name="T">The generic type of the queue.</typeparam>
    public class SyncQueue<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        // based on information contained in http://yacsharpblog.blogspot.com/2008/07/thread-synchronized-queing.html

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncQueue&lt;T&gt;"/> class.
        /// </summary>
        public SyncQueue()
        {
            handles = new WaitHandle[] { new AutoResetEvent(false), new ManualResetEvent(false), };
        }
        WaitHandle[] handles = null;

        private Queue<T> _q = new Queue<T>();

        // To-Do: replace lock(_q) with lock(queueLock) or something similar.
        // to avoid lock(this) problems http://bytes.com/topic/c-sharp/answers/242087-whats-wrong-lock
        //object queueLock = new object();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.ICollection"/>.
        /// </returns>
        public int Count { get { lock (_q) { return _q.Count; } } }
        
        /// <summary>
        /// Peeks this instance.
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            lock (_q)
            {
                if (_q.Count > 0)
                    return _q.Peek();
            }
            return default(T);
        }

        /// <summary>
        /// Enqueues the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        public void Enqueue(T element)
        {
            lock (_q)
            {
                _q.Enqueue(element);
                ((AutoResetEvent)handles[0]).Set();
            }
        }

        /// <summary>
        /// Dequeues the first queue element.
        /// </summary>
        /// <param name="timeout_milliseconds">The timeout value in milliseconds.</param>
        /// <returns></returns>
        public T Dequeue(int timeout_milliseconds)
        {
            T element;
            try
            {
                if (WaitHandle.WaitAny(handles, timeout_milliseconds) == 0)
                {
                    lock (_q)
                    {
                        if (_q.Count > 0)
                        {
                            element = _q.Dequeue();
                            if (_q.Count > 0)
                            {
                                ((AutoResetEvent)handles[0]).Set();
                            }
                            return element;
                        }
                    }
                }
                return default(T);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Dequeues this first element in the instance.
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            return Dequeue(-1);
        }

        /// <summary>
        /// Interrupts queue processing on this instance.
        /// </summary>
        public void Interrupt()
        {
            ((ManualResetEvent)handles[1]).Set();
        }
        /// <summary>
        /// Uninterrupts queue processing on this instance.
        /// </summary>
        public void Uninterrupt()
        {
            // for completeness, lets the queue be used again
            ((ManualResetEvent)handles[1]).Reset();
        }

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return _q.GetEnumerator();
        }

        #endregion

        #region ICollection

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is less than zero.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="array"/> is multidimensional.
        /// -or-
        /// <paramref name="index"/> is equal to or greater than the length of <paramref name="array"/>.
        /// -or-
        /// The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.
        /// </exception>
        public void CopyTo(Array array, int index)
        {
            _q.CopyTo((T[])array, index);
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.
        /// </returns>
        public bool IsSynchronized
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </returns>
        public object SyncRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IEnumerable<T>

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _q.GetEnumerator();
        }

        #endregion

    }
}

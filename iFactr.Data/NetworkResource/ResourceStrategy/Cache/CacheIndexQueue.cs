using MonoCross.Navigation;
using MonoCross.Utilities;
using System;
using System.Linq;
using System.Threading;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    [Obsolete]
    public delegate void CacheIndexDelegate(CacheIndexItem cacheIndexItem);

    [Obsolete]
    public class CacheIndexDelegateCall
    {
        /// <summary>
        /// Gets or sets the delegate.
        /// </summary>
        /// <value>The delegate.</value>
        public CacheIndexDelegate Delegate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public CacheIndexItem Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a cache index queue.
    /// </summary>
    [Obsolete]
    public class CacheIndexQueue : SyncQueue<CacheIndexDelegateCall>
    {
        /// <summary>
        /// Gets the instance of the cache index queue singleton.
        /// </summary>
        /// <value>The CacheIndexQueue instance.</value>
        public static CacheIndexQueue Instance
        {
            get
            {
                if (!MXContainer.Session.ContainsKey("iFactr-CacheIndexQueue"))
                    MXContainer.Session["iFactr-CacheIndexQueue"] = new CacheIndexQueue();

                return (CacheIndexQueue)MXContainer.Session["iFactr-CacheIndexQueue"];
            }
        }

        private const int timerDelay = 5000;
        private AutoResetEvent _timerReset = new AutoResetEvent(false);

        private bool _enabled = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CacheIndexQueue"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                if (value && this.Count() > 0)
                    TriggerTimer();
            }
        }

        private CacheIndexQueue()
        {
        }

        private void TriggerTimer()
        {
            if (!Enabled)
                return;
            _timerReset.Set();
            Device.Thread.QueueWorker(o =>
            {
                if (!_timerReset.WaitOne(timerDelay))
                    AttemptNextCall();
            });
        }

        /// <summary>
        /// Enqueues the specified cache index delegate call.
        /// </summary>
        /// <param name="cacheIndexDelegateCall">The cache index delegate call.</param>
        public new void Enqueue(CacheIndexDelegateCall cacheIndexDelegateCall)
        {
            base.Enqueue(cacheIndexDelegateCall);

            TriggerTimer();
        }
        /// <summary>
        /// Enqueues the specified cache index delegate.
        /// </summary>
        /// <param name="cacheIndexDelegate">The cache index delegate.</param>
        /// <param name="cacheIndexItem">The cache index item.</param>
        public void Enqueue(CacheIndexDelegate cacheIndexDelegate, CacheIndexItem cacheIndexItem)
        {
            CacheIndexDelegateCall cacheIndexDelegateCall = new CacheIndexDelegateCall()
            {
                Delegate = cacheIndexDelegate,
                Item = cacheIndexItem
            };

            base.Enqueue(cacheIndexDelegateCall);

            TriggerTimer();
        }

        /// <summary>
        /// Attempts the next call.
        /// </summary>
        public void AttemptNextCall()
        {
            while (this.Count > 0)
            {
                CacheIndexDelegateCall nextCall = Peek();

                if (nextCall == null)
                {
                    Dequeue();
                    continue;
                }

                try
                {
                    nextCall.Delegate(nextCall.Item);
                    Dequeue();
                }
                catch (Exception exc)
                {
                    Device.Log.Error(exc);
                }
            }
        }

    }
}
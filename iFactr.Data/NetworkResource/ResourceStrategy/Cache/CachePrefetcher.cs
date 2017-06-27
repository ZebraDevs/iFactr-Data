using System;
using System.Threading;
using MonoCross.Navigation;
using MonoCross.Utilities;

#if !NETCF
using System.Threading.Tasks;
#endif

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Represents the cache prefetcher.
    /// </summary>
    public class CachePrefetcher
    {
        private Timer timer;
        private const int timerDelay = 30 * 60 * 1000;  //  30 minutes

        /// <summary>
        /// Gets the instance of the prefetcher.
        /// </summary>
        /// <value>The prefetcher singleton instance.</value>
        public static CachePrefetcher Instance
        {
            get
            {
                if (!MXContainer.Session.ContainsKey("iFactr-CachePrefetcher"))
                    MXContainer.Session["iFactr-CachePrefetcher"] = new CachePrefetcher();

                return (CachePrefetcher)MXContainer.Session["iFactr-CachePrefetcher"];
            }
        }

        private CachePrefetcher()
        {
        }

        private bool _enabled = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CachePrefetcher"/> is enabled.
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
                if (_enabled)
                    TriggerTimer();
                else
                {
                    timer.Dispose();
                    timer = null;
                }
            }
        }

        private void TriggerTimer()
        {
            if (!Enabled)
                return;

#if !NETCF
            if (timer != null)
                timer.Cancel();
            timer = new Timer(o => InitiatePrefetch(), null, 1000, timerDelay);
#else
            if (timer != null)
                timer.Change(1000, timerDelay);
            else
            {
                timer = new Timer( new TimerCallback( ( o ) =>
                {
                    InitiatePrefetch();
                } ), null, 1000, timerDelay );
            }
#endif
        }

#if !NETCF
        private sealed class Timer : CancellationTokenSource
        {
            internal Timer(Action<object> callback, object state, int millisecondsDueTime, int millisecondsPeriod, bool waitForCallbackBeforeNextPeriod = false)
            {
                Task.Delay(millisecondsDueTime, Token).ContinueWith(async (t, s) =>
                {
                    var tuple = (Tuple<Action<object>, object>)s;

                    while (!IsCancellationRequested)
                    {
                        if (waitForCallbackBeforeNextPeriod)
                            tuple.Item1(tuple.Item2);
                        else
                            new Task(() => tuple.Item1(tuple.Item2)).Start();

                        await Task.Delay(millisecondsPeriod, Token).ConfigureAwait(false);
                    }

                }, Tuple.Create(callback, state), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    Cancel();

                base.Dispose(disposing);
            }
        }
#endif

        /// <summary>
        /// Initiates the prefetch.
        /// </summary>
        public void InitiatePrefetch()
        {
            Device.Thread.QueueIdle(CacheIndexMap.CleanIndexes);
#if PCL
            Task.Delay(60000).Wait();
#else
            Thread.Sleep(60000);
#endif
            Device.Thread.QueueIdle(CacheIndexMap.PreFetchIndexes);
        }
    }
}

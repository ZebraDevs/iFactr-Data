namespace iFactr.Data.Utilities.NetworkResource
{
    /// <summary>
    /// Enum to specify how to handle attempts to refresh stale data.
    /// </summary>
    public enum CacheStaleMethod
    {
        /// <summary>
        /// Deferred: Attempt to refresh stale data by placing request upon idle thread queue
        /// </summary>
        Deferred,
        /// <summary>
        /// Immediate: Attempt to refresh stale data immediately.
        /// </summary>
        Immediate
    }
}

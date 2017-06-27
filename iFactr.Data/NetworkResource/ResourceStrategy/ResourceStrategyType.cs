namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    //As new strategy types are added, maintain list in ResourceStrategyController.Initialize()
    /// <summary>
    /// Specifies the valid resource strategy types.
    /// </summary>
    public enum ResourceStrategyType
    {
        /// <summary>
        /// Places a copy of the resource in both the in-memory and persistent cache.
        /// </summary>
        Cache,
        /// <summary>
        /// Returns the resource directly with no caching.
        /// </summary>
        DirectStream,
        /// <summary>
        /// Places a copy of the resource in the persistent cache, but not in-memory.
        /// </summary>
        LocalFile
    }
}

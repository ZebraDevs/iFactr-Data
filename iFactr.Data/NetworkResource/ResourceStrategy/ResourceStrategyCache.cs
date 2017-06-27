using iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache;
using MonoCross.Utilities;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    internal class ResourceStrategyCache : BaseResourceStrategy, IResourceStrategy
    {
        private static bool _initialized = false;

        public override ResourceStrategyType Type
        {
            get
            {
                return ResourceStrategyType.Cache;
            }
        }

        public override ResourceResponse GetResponse( string uri )
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return GetResponse( uri, args );
        }

        /// <summary>
        /// Gets the resource response.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="args">The arguments to be used on the request.</param>
        /// <returns></returns>
        public override ResourceResponse GetResponse( string uri, NetworkResourceArguments args )
        {
            ResourceResponseCache response = new ResourceResponseCache( uri, args );

            // find the item in the cache index
            CacheIndex cacheIndex = CacheIndexMap.GetFromUri( uri );
            CacheIndexItem cacheIndexItem = cacheIndex.Get( uri );

            // ensure cache item is current
            response.ReturnStatus = cacheIndex.EnsureCurrentCache( cacheIndexItem, args );

            // store local references to cache index and item to prevent having to look up
            // values again later.
            response.CacheIndex = cacheIndex;
            response.CacheIndexItem = cacheIndexItem;

            // populate response data collection with cache index values;
            response.Data.Add( "RelativeUri", cacheIndexItem.RelativeUri );
            response.Data.Add( "AttemptToRefresh", cacheIndexItem.AttemptToRefresh.ToString() );
            response.Data.Add( "Downloaded", cacheIndexItem.Downloaded.ToString() );
            response.Data.Add( "Expiration", cacheIndexItem.Expiration.ToString() );
            response.Data.Add( "IsExpired", cacheIndexItem.IsExpired.ToString() );
            response.Data.Add( "IsStale", cacheIndexItem.IsStale.ToString() );
            response.Data.Add( "ETag", cacheIndexItem.ETag );

            return response;
        }

        public override void Initialize()
        {
            if ( _initialized )
                return;

            _initialized = true;
        }
    }
}

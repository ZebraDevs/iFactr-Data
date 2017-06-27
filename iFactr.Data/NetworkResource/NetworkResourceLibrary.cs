using iFactr.Data.Utilities.NetworkResource.ResourceStrategy;
using MonoCross.Utilities;
using System;

namespace iFactr.Data.Utilities.NetworkResource
{
    /// <summary>
    /// Singleton
    /// </summary>
    public class NetworkResourceLibrary
    {
        // -- .NET Optimized Singleton (not 'lazy' implementation)
        // Static members are 'eagerly initialized' - immediately when class is loaded for the first time.
        // .NET guarantees thread safety for static initialization
        private static readonly NetworkResourceLibrary _library = new NetworkResourceLibrary();

        // Private constructor 
        private NetworkResourceLibrary()
        {
        }

        // Public static property to get the object
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static NetworkResourceLibrary Instance
        {
            get
            {
                return _library;
            }
        }

        // -- PUBLIC PROPERTIES
        /// <summary>
        /// The default ResourceStrategyType is currently the ResourceStrategyType.Cache
        /// </summary>
        public ResourceStrategyType DefaultResourceStrategyType
        {
            get
            {
                return ResourceStrategyType.Cache;
            }
        }

        // -- PUBLIC METHODS
        /// <summary>
        /// Gets the resource request.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <returns></returns>
        public ResourceRequest GetResourceRequest( string uri )
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return GetResourceRequest( uri, DefaultResourceStrategyType, args );
        }

        /// <summary>
        /// Returns Resource Request for specified cache with default resource strategy
        /// </summary>
        /// <returns></returns>
        public ResourceRequest GetResourceRequest( string uri, NetworkResourceArguments args )
        {
            // return default value
            return GetResourceRequest( uri, DefaultResourceStrategyType, args );
        }

        /// <summary>
        /// Gets the resource request.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="resourceStrategyType">Type of the resource strategy.</param>
        /// <returns></returns>
        public ResourceRequest GetResourceRequest( string uri, ResourceStrategyType resourceStrategyType )
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return GetResourceRequest( uri, resourceStrategyType, args );
        }

        /// <summary>
        /// Returns Resource Request for specified cache and resource strategy
        /// </summary>
        /// <returns></returns>
        public ResourceRequest GetResourceRequest( string uri, ResourceStrategyType resourceStrategyType, NetworkResourceArguments args )
        {
            if ( string.IsNullOrEmpty( uri ) )
                throw new ArgumentNullException( "uri" );

            // return default value
            ResourceRequest resourceRequest = new ResourceRequest( uri, resourceStrategyType, args );
            return resourceRequest;
        }

        /// <summary>
        /// Initializes the Network Resource Library and the supporting objects
        /// </summary>
        public void Initialize()
        {
            // Initialize our resource strategies
            ResourceStrategyController.Initialize();
        }
    }
}

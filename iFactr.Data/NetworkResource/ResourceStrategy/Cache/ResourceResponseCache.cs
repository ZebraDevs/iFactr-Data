using MonoCross.Utilities;
using System;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Represents the resource response cache.
    /// </summary>
    public class ResourceResponseCache : ResourceResponse
    {
        /// <summary>
        /// Gets or sets the index of the cache.
        /// </summary>
        /// <value>The index of the cache.</value>
        public CacheIndex CacheIndex
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the cache index item.
        /// </summary>
        /// <value>The cache index item.</value>
        public CacheIndexItem CacheIndexItem
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceResponseCache"/> class.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="args">The arguments used on the request.</param>
        internal ResourceResponseCache( string uri, NetworkResourceArguments args )
        {
            Uri = uri;
            NetworkResourceArguments = args;
        }

        /// <summary>
        /// Gets or sets the expiration.
        /// </summary>
        /// <value>The expiration.</value>
        public override DateTime Expiration
        {
            get
            {
                return CacheIndexItem.Expiration;
            }
        }

        /// <summary>
        /// Gets or sets the attempt to refresh date.
        /// </summary>
        /// <value>The attempt to refresh date.</value>
        public override DateTime AttemptToRefresh
        {
            get
            {
                return CacheIndexItem.AttemptToRefresh;
            }
        }

        /// <summary>
        /// returns the full path name of the cached file for CacheIndexItem
        /// </summary>
        /// <returns></returns>
        public override string GetResponseFileName()
        {
            MonoCross.NetworkResponse NetworkResponse;
            string filename = CacheIndex.GetFileName( CacheIndexItem, NetworkResourceArguments, out NetworkResponse );
            ReturnStatus = NetworkResponse;
            return filename;
        }

        /// <summary>
        /// Gets the response string.
        /// </summary>
        /// <returns></returns>
        public override string GetResponseString()
        {
            try
            {
                string cachedFile = GetResponseFileName();
                if ( string.IsNullOrEmpty( cachedFile ) || !Device.File.Exists( cachedFile ) )
                    return string.Empty;
                return Device.File.ReadString( cachedFile );
            }
            catch ( Exception e )
            {
                // Let the user know what went wrong.
                Device.Log.Error( "The file could not be read:", e );
            }

            return null;
        }

        /// <summary>
        /// Gets the response byte array.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetResponseBytes()
        {
            try
            {
                string cachedFile = GetResponseFileName();
                if (string.IsNullOrEmpty(cachedFile) || !Device.File.Exists(cachedFile))
                    return new byte[0];
                return Device.File.Read(cachedFile);
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Device.Log.Error("The file could not be read:");
                Device.Log.Error(e.Message);
                throw;
            }
        }
    }
}
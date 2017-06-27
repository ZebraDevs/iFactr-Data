using MonoCross;
using MonoCross.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Represents a cache manifest for the prefetcher.
    /// </summary>
    public class CacheManifest
    {
        private delegate void DelegateAddCacheManifestItem(CacheManifest cacheManifest, string value);

        /// <summary>
        /// Enum used to parse cache manifest file.
        /// </summary>
        private enum CacheManifestMode { CACHE, NETWORK, FALLBACK }

        /// <summary>
        /// Gets or sets the cache.
        /// </summary>
        /// <value>The Collection of resources to be pre-fetched.</value>
        public List<string> Cache { get; set; }

        /// <summary>
        /// Gets or sets the network.
        /// </summary>
        /// <value>The Collection of resources and/or patterns to be white-listed for never caching.</value>
        public List<string> Network { get; set; }

        /// <summary>
        /// Gets or sets the fallback.
        /// </summary>
        /// <value>The Collection of resources and/or patterns with fallback mappings if connection and cache in unavailable.</value>
        public List<string> Fallback { get; set; }

        /// <summary>
        /// Gets or sets the manifest URI.
        /// </summary>
        /// <value>The manifest URI.</value>
        public Uri ManifestUri { get; set; }

        /// <summary>
        /// Gets the name of the manifest file.
        /// </summary>
        /// <value>The name of the manifest file.</value>
        public string ManifestFileName
        {
            get
            {
                string uri = ManifestUri.AbsoluteUri;
                return uri.Substring(uri.LastIndexOf('/') + 1);
            }
        }

        /// <summary>
        /// Gets the manifest base URI.
        /// </summary>
        /// <value>The manifest base URI.</value>
        public string ManifestBaseUri
        {
            get
            {
                string uri = ManifestUri.AbsoluteUri;
                int index = uri.LastIndexOf('/');
                return uri.Remove(index, uri.Length - index + 1);
            }
        }



        /// <summary>
        /// CacheManifest Private Constructor
        /// </summary>
        private CacheManifest()
        {
            Cache = new List<string>();
            Network = new List<string>();
            Fallback = new List<string>();
        }

        
        /// <summary>
        /// Factory method to create CacheManifest based on URI string
        /// </summary>
        /// <param name="manifestUri">string</param>
        /// <returns>CacheManifest</returns>
        public static CacheManifest CreateFromUri( string manifestUri )
        {
            return CreateFromUri( manifestUri, Device.RequestInjectionHeaders );
        }
        /// <summary>
        /// Factory method to create CacheManifest based on URI string
        /// </summary>
        /// <param name="manifestUri">the manifest URI string.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <returns>CacheManifest</returns>
        public static CacheManifest CreateFromUri(string manifestUri, IDictionary<string, string> headers)
        {
            // validate parameter
            if (string.IsNullOrEmpty(manifestUri))
                throw new ArgumentNullException("manifestUri");

            NetworkResponse networkResponse = Device.Network.Fetcher.Fetch(manifestUri, headers, 60000);
            if ( networkResponse.StatusCode != HttpStatusCode.OK )
            {
                throw new Exception( string.Format("Unable to download manifest file from server: uri: {0}  message: {1}", manifestUri, networkResponse.Message ), networkResponse.Exception );
            }

            // create CacheManifest object from manifest string from response
            CacheManifest cacheManifest = CacheManifest.CreateFromString( networkResponse.ResponseString );
            cacheManifest.ManifestUri = new Uri(manifestUri);

            return cacheManifest;
        }

        /// <summary>
        /// Factory Method to create CacheManifest from a properly formatted String.
        /// </summary>
        /// <param name="manifestString">Manifest Response string (i.e. cache manifest file as a string)</param>
        /// <returns>CacheManifest</returns>
        public static CacheManifest CreateFromString(string manifestString)
        {
            // validate parameter
            if (string.IsNullOrEmpty(manifestString))
                throw new ArgumentNullException("manifestString");

            // create CacheManifest object
            CacheManifest cacheManifest = new CacheManifest();

            // populate CacheManifest from string
            PopulateCacheManifest(cacheManifest, manifestString);

            return cacheManifest;
        }

        /// <summary>
        /// populates a CacheManifest object from a string of cache manifest data.
        /// </summary>
        /// <param name="cacheManifest"></param>
        /// <param name="manifest"></param>
        private static void PopulateCacheManifest(CacheManifest cacheManifest, string manifest)
        {

            if (string.IsNullOrEmpty(manifest))
                throw new ArgumentNullException("manifest");
            if (cacheManifest == null)
                throw new ArgumentNullException("cacheManifest");

            DelegateAddCacheManifestItem addCacheManifestItem;

            // Add code to validate manifest string

            // default mode to Cache
            addCacheManifestItem = AddCacheManifestCacheItem;

            // break string into lines array
            char[] delimiters = { '\r', '\n' };
            var lines = manifest.Split(delimiters).Where(line => !string.IsNullOrEmpty(line));

            var first = lines.First().Trim(Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble(), 0, Encoding.UTF8.GetPreamble().Length).ToCharArray());

            if (first.Trim() != "CACHE MANIFEST")
                throw new Exception("Invalid Cache manifest");

            // strip string of comment lines and empty lines
            foreach (String s in lines)
            {
                // skip first line of file string. (i.e "CACHE MANIFEST" )
                if (s == lines.First()) continue;

                string line = s.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                switch (line.ToUpper())
                {
                    case "NETWORK:":
                        addCacheManifestItem = AddCacheManifestNetworkItem;
                        continue;
                    case "CACHE:":
                        addCacheManifestItem = AddCacheManifestCacheItem;
                        continue;
                    case "FALLBACK:":
                        addCacheManifestItem = AddCacheManifestFallbackItem;
                        continue;
                }
                // add string to mode
                addCacheManifestItem(cacheManifest, line);
            }
        }

        /// <summary>
        /// Delegate to parse and add entry to Network list.
        /// </summary>
        /// <param name="cacheManifest"></param>
        /// <param name="value"></param>e-manifest are never cached.
        /// <remarks>Network list items in cache</remarks>
        private static void AddCacheManifestNetworkItem(CacheManifest cacheManifest, string value)
        {
            cacheManifest.Network.Add(value);
        }

        /// <summary>
        /// Delegate to parse and add entry to Cache list
        /// </summary>
        /// <param name="cacheManifest"></param>
        /// <param name="value"></param>
        private static void AddCacheManifestCacheItem(CacheManifest cacheManifest, string value)
        {
            cacheManifest.Cache.Add(value);
        }

        /// <summary>
        /// Delegate to parse and add entry to fallback list
        /// </summary>
        /// <param name="cacheManifest"></param>
        /// <param name="value"></param>
        private static void AddCacheManifestFallbackItem(CacheManifest cacheManifest, string value)
        {
            cacheManifest.Fallback.Add(value);
        }
    }
}
using MonoCross.Navigation;
using MonoCross.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Represents the cache index map.
    /// </summary>
    public static class CacheIndexMap
    {
        /// <summary>
        /// Gets or sets a value indicating whether the cache index map is case insensitive.
        /// </summary>
        /// <value><c>true</c> if case is ignored; otherwise, <c>false</c>.</value>
        public static bool IgnoreCase
        {
            get { return _ignoreCase; }
            set { _ignoreCase = value; }
        }
        private static bool _ignoreCase = true;

        /// <summary>
        /// Prefetch property to control the prefetch process overall.
        /// </summary>
        public static bool PrefetchProcessEnabled
        {
            get { return _prefetchEnabled; }
            set { _prefetchEnabled = value; }
        }
        private static bool _prefetchEnabled = true;

        private const string IndexMapKey = "iFactr_CacheIndexMap";

        /// <summary>
        /// Gets the cache index map.
        /// </summary>
        /// <value>The cache index map as a dictionary keyed by base URI.</value>
        public static Dictionary<string, CacheIndex> Map
        {
            get
            {
                lock (MXContainer.Session)
                {
                    object map;
                    if (MXContainer.Session.TryGetValue(IndexMapKey, out map))
                        return (Dictionary<string, CacheIndex>) map;
                        
                    Dictionary<string, CacheIndex> newMap;
                    if (_ignoreCase)
                        newMap = new Dictionary<string, CacheIndex>(StringComparer.OrdinalIgnoreCase);
                    else
                        newMap = new Dictionary<string, CacheIndex>(StringComparer.Ordinal);
                    
                    MXContainer.Session[IndexMapKey] = newMap;
                    return newMap;
                }
            }
        }

        /// <summary>
        /// Adds the specified cache index.
        /// </summary>
        /// <param name="cacheIndex">The cache index to add to the map.</param>
        public static void Add(CacheIndex cacheIndex)
        {
            if (cacheIndex == null)
                throw new ArgumentNullException("cacheIndex");

            string key = CacheIndexMap.GetKey(cacheIndex);
            
            lock (Map)
            {
                // if the cacheIndex key doesn't already exist in the map then add it.
                if ( !Map.ContainsKey( key ) )
                    Map.Add( key, cacheIndex );
            }
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public static void Add(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key", "Requested CacheIndex key is null or empty");
            
            lock (Map)
            {
                if ( !Map.ContainsKey( key ) )
                    Map.Add( key, CacheIndex.Create(key) );
            }
        }

        /// <summary>
        /// Adds the specified cache manifest.
        /// </summary>
        /// <param name="cacheManifest">The cache manifest.</param>
        public static void Add(CacheManifest cacheManifest)
        {
            if (cacheManifest == null)
                throw new ArgumentNullException("cacheManifest");

            lock (Map)
            {
                // if the cacheIndex key doesn't already exist in the map then add it.
                if (!Map.ContainsKey(cacheManifest.ManifestBaseUri))
                {
                    CacheIndex cacheIndex = CacheIndex.Create(cacheManifest);
                    Map.Add(cacheManifest.ManifestBaseUri, cacheIndex);
                }
            }
        }

        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static CacheIndex Get(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key", "Requested CacheIndex key is null or empty");

            CacheIndex cacheIndex;
            if (!Map.TryGetValue(key, out cacheIndex))
            {
                cacheIndex = CacheIndex.Create(key);
                CacheIndexMap.Add(cacheIndex);
            }

            return cacheIndex;
        }

        /// <summary>
        /// Returns cache index that matches Uri
        /// </summary>
        /// <param name="uriString">Absolute Uri for resource represented by Uri</param>
        /// <returns></returns>
        public static CacheIndex GetFromUri(string uriString)
        {
            if (String.IsNullOrEmpty(uriString))
                throw new ArgumentNullException("uriString");

            string key = GetKeyFromUri(uriString);

            // To-Do: should this really create a cache index item
            CacheIndex cacheIndex;
            if (!Map.TryGetValue(key, out cacheIndex))
            {
                cacheIndex = CacheIndex.Create(key);
                CacheIndexMap.Add(cacheIndex);
            }

            return cacheIndex;
        }

        /// <summary>
        /// Updates the specified cache index.
        /// </summary>
        /// <param name="cacheIndex">Index of the cache.</param>
        public static void Update(CacheIndex cacheIndex)
        {
            if (cacheIndex == null)
                throw new ArgumentNullException("cacheIndex");

            string key = CacheIndexMap.GetKey(cacheIndex);

            // if the key exists in the map then replace it with current parameter..
            if (Map.ContainsKey(key))
            {
                CacheIndexMap.Remove(key);
            }

            CacheIndexMap.Add(cacheIndex);
        }

        /// <summary>
        /// Derives a unique key for a cache index, i.e. cacheIndex.BaseUri
        /// </summary>
        /// <param name="cacheIndex"></param>
        /// <returns></returns>
        public static string GetKey(CacheIndex cacheIndex)
        {
            if (cacheIndex == null)
                throw new ArgumentNullException("cacheIndex");

            return cacheIndex.BaseUri;
        }

        /// <summary>
        /// Gets the key from URI specified.
        /// </summary>
        /// <param name="uriString">The URI string.</param>
        /// <returns></returns>
        public static string GetKeyFromUri(string uriString)
        {
            if (string.IsNullOrEmpty(uriString))
                throw new ArgumentNullException("uriString");

            //if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            //    throw new ArgumentException("uriString does not represent a valid Absolute Uri " + uriString);

            uriString = uriString.RemoveTrailingSlash();

            Uri uri = new Uri(uriString);
            string schemeHost = string.Format("{0}://{1}", uri.Scheme, uri.Host);

            var keys = Map.Keys.Where(k => uriString.StartsWith(k)).OrderByDescending(k => k.Length);

            if (keys.Count() == 0)
            {
                return schemeHost;
            }

            return keys.First();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public static void Clear()
        {
            lock (Map)
            {
                Map.Clear();
            }
        }

        /// <summary>
        /// Removes the specified cache index.
        /// </summary>
        /// <param name="cacheIndex">Index of the cache.</param>
        public static void Remove(CacheIndex cacheIndex)
        {
            if (cacheIndex == null)
                throw new ArgumentNullException("cacheIndex", "Requested CacheIndex is null");

            string key = CacheIndexMap.GetKey(cacheIndex);

            CacheIndexMap.Remove(key);
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public static void Remove(string key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key", "Requested CacheIndex key is null or empty");

            lock (Map)
            {
                // if the cacheIndex key exists in the map, then remove it.
                if (Map.ContainsKey(key))
                {
                    Map.Remove(key);
                }
            }
        }


        /// <summary>
        /// Overload to match ParameterizedThreadStart delegate
        /// </summary>
        /// <param name="obj"></param>
        public static void PreFetchIndexes( object obj )
        {
            PreFetchIndexes();
        }
        /// <summary>
        /// Calls Prefetch items for each CacheIndex.
        /// </summary>
        public static void PreFetchIndexes()
        {
            lock (Map)
            {
                foreach ( CacheIndex cacheIndex in Map.Values )
                {
                    Device.Thread.QueueIdle( cacheIndex.PreFetchItems );
                }
            }
        }


        /// <summary>
        /// Overload to match ParameterizedThreadStart delegate
        /// </summary>
        /// <param name="obj"></param>
        public static void CleanIndexes( object obj )
        {
            CleanIndexes();
        }
        /// <summary>
        /// Calls CleanIndex for each CacheIndex.
        /// </summary>
        public static void CleanIndexes()
        {
            lock (Map)
            {
                foreach ( CacheIndex cacheIndex in Map.Values )
                {
                    Device.Thread.QueueIdle( cacheIndex.CleanIndex );
                }
            }
        }
    }
}
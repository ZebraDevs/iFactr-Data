using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MonoCross.Utilities.Serialization;
using System.Threading;
using MonoCross.Utilities.Threading;
using MonoCross.Utilities;
using MonoCross;

#if !NETCF
using System.Reflection;
#endif

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Defines the delegate for prefetch events.
    /// </summary>
    /// <param name="indexUri">The URI of the CacheIndex on which the prefetch is called.</param>
    /// <param name="items">The CacheIndexItems for the prefetch operation.</param>
    public delegate void PrefetchDelegate(string indexUri, CacheIndexItem[] items);

    /// <summary>
    /// Represents an index of cached resources.
    /// </summary>
#if !SILVERLIGHT && !NETFX_CORE && !PCL
    [Serializable]
#endif
#if (DROID)
    [Android.Runtime.Preserve(AllMembers = true)]
#elif (TOUCH)
    [MonoTouch.Foundation.Preserve (AllMembers = true)]
#endif
    public class CacheIndex : List<CacheIndexItem>
    {
        static object staticLock = new object();  // lock object for static method
        object syncLock = new object(); // lock object for instance methods.
        const int serialiationInterval = 30000;
        List<CacheIndexItem> prefetchItems = new List<CacheIndexItem>();

        /// <summary>
        /// Occurs when a cache prefetch operation is complete.
        /// </summary>
        public event PrefetchDelegate OnPrefetchComplete;

        /// <summary>
        /// For serialization on Touch only.  DO NOT USE. 
        /// </summary>
        public CacheIndex()
        {
        }

        CacheIndex(string baseUri)
        {
            BaseUri = baseUri;
        }
        CacheIndex(string baseUri, string cachePath)
        {
            BaseUri = baseUri;
            CachePath = cachePath;

            string directoryName = this.CachePath.AppendPath(this.BaseUriPath);
            if (!Device.File.Exists(directoryName))
                Device.File.CreateDirectory(directoryName);
        }

        static CacheIndex()
        {
            SerializationInterval = serialiationInterval;
        }

        // To-Do - make more efficient so 2 new statements aren't used
        /// <summary>
        /// Creates a new cache index using the specified base URI string.
        /// </summary>
        /// <param name="baseUriString">The base URI string.</param>
        /// <returns></returns>
        public static CacheIndex Create(string baseUriString)
        {
            return Create(baseUriString, Device.SessionDataPath);
        }
        /// <summary>
        /// Creates a new cache index using the specified base URI string and cache path.
        /// </summary>
        /// <param name="baseUriString">The base URI string.</param>
        /// <param name="cachePath">The cache path.</param>
        /// <returns></returns>
        public static CacheIndex Create(string baseUriString, string cachePath)
        {
            if (string.IsNullOrEmpty(baseUriString))
                throw new ArgumentNullException("baseUriString");

            CacheIndex cacheIndex = new CacheIndex(baseUriString, cachePath);

            // attempt to deserialize cache index.
            cacheIndex = DeserializeCacheIndex(cacheIndex.SerializeFile);

            if (cacheIndex == null)
            {
                cacheIndex = new CacheIndex(baseUriString, cachePath);
            }
            else
            {
                // To-Do: for some reason properties in this class aren't being serialized
                //        so for now, simply reapply base string, and use default values and SerializeFileName
                cacheIndex.BaseUri = baseUriString;
                cacheIndex.CachePath = cachePath;
            }

            cacheIndex.PreFetchIndexEnabled = true;
            cacheIndex.CleanIndexEnabled = true;

            return cacheIndex;
        }

        /// <summary>
        /// Creates a new cache index using the specified cache manifest.
        /// </summary>
        /// <param name="cacheManifest">The cache manifest.</param>
        /// <returns></returns>
        public static CacheIndex Create(CacheManifest cacheManifest)
        {
            return Create(cacheManifest, Device.SessionDataPath);
        }

        // To-Do - make more efficient so 2 new statements aren't used
        /// <summary>
        /// Creates a new cache index using the specified cache manifest and cache path.
        /// </summary>
        /// <param name="cacheManifest">The cache manifest.</param>
        /// <param name="cachePath">The cache path.</param>
        /// <returns></returns>
        public static CacheIndex Create(CacheManifest cacheManifest, string cachePath)
        {
            // derive base URI from cache manifest.
            // base Uri = root folder of cache manifest file.

            if (cacheManifest == null)
            {
                throw new ArgumentNullException("cacheManifest cannot be null.");
            }

            string baseUriString = cacheManifest.ManifestBaseUri;

            CacheIndex cacheIndex = new CacheIndex(baseUriString, cachePath);

            // attempt to deserialize cache index.
            cacheIndex = DeserializeCacheIndex(cacheIndex.SerializeFile);

            if (cacheIndex == null)
            {
                cacheIndex = new CacheIndex(baseUriString, cachePath);
            }
            else
            {
                // To-Do: for some reason properties in this class aren't being serialized
                //        so for now, simply reapply base string, and use default values and SerializeFileName
                cacheIndex.BaseUri = baseUriString;
                cacheIndex.CachePath = cachePath;
            }

            // since we're creating from CacheManifest, execute the update
            cacheIndex.UpdateIndex(cacheManifest);

            cacheIndex.PreFetchIndexEnabled = true;
            cacheIndex.CleanIndexEnabled = true;

            return cacheIndex;
        }

        // To-Do: change default value as needed.
        string _cachePath;
        /// <summary>
        /// Gets or sets the cache path.
        /// </summary>
        /// <value>The cache path.</value>
        public string CachePath
        {
            get
            {
#if DEBUG
                if (string.IsNullOrEmpty(_cachePath))
                    _cachePath = Device.SessionDataPath.AppendPath("Cache");
#endif
                return _cachePath.RemoveTrailingSlash();
            }
            protected set
            {
                _cachePath = value.RemoveTrailingSlash();
            }
        }

        string _baseUri;
        /// <summary>
        /// Gets or sets the base URI.
        /// </summary>
        /// <value>The base URI.</value>
        public string BaseUri
        {
            get
            {
                return _baseUri.RemoveTrailingSlash();
            }
            protected set
            {
                _baseUri = value.RemoveTrailingSlash();
            }
        }

        /// <summary>
        /// Gets the base URI path.
        /// </summary>
        /// <value>The base URI path.</value>
        public string BaseUriPath
        {
            get
            {
                string path = BaseUri;
                if (path == null)
                    return null;

                // remove http/https from beginning
                if (path.StartsWith(@"https://"))
                    path = path.Substring(7);
                if (path.StartsWith(@"http://"))
                    path = path.Substring(6);

                if (Regex.IsMatch(path, "^[a-zA-Z]:.*"))
                    path = path.Substring(2);

                // remove slashes from front and back
                path = path.RemoveLeadingSlash();

                // convert / and \ to underscore
                path = path.Replace(@"\", "_").Replace(@"/", "_").Replace(@".", "_");

                // clean out the port number
                if (path.Contains(":")) { path = path.Replace(':', '_'); }

                return path;
            }
        }

        string _serializeFile;
        /// <summary>
        /// Gets or sets the serialize file.
        /// </summary>
        /// <value>The serialize file.</value>
        public string SerializeFile
        {
            get
            {
                if (_serializeFile != null)
                    return _serializeFile;

                //_serializeFile = String.Format( "{0}/{1}/{2}", CachePath, BaseUriPath, SerializeFileName );
                _serializeFile = CachePath.AppendPath(BaseUriPath).AppendPath(SerializeFileName);

                return _serializeFile;
            }
            protected set
            {
                _serializeFile = value;
            }
        }

        string _serializeFileName;
        /// <summary>
        /// Gets or sets the name of the serialize file name.
        /// </summary>
        /// <value>The name of the serialize file.</value>
        public string SerializeFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_serializeFileName))
                    _serializeFileName = "cache_index.xml";

                return _serializeFileName;

            }
            protected set
            {
                _serializeFileName = value;
            }
        }

        /// <summary>
        /// Deserializes the cache index.
        /// </summary>
        /// <param name="serializeFileName">Name of the serialize file.</param>
        /// <returns></returns>
        public static CacheIndex DeserializeCacheIndex(string serializeFileName)
        {
            lock (staticLock)
            {
                CacheIndex cacheIndex = null;
                ISerializer<CacheIndex> iSerializer = SerializerFactory.Create<CacheIndex>(SerializationFormat.XML);
                try
                {
                    cacheIndex = iSerializer.DeserializeObjectFromFile(serializeFileName);
                    if (cacheIndex != null)
                    {
                        cacheIndex.PreFetchIndexEnabled = true;
                        cacheIndex.CleanIndexEnabled = true;
                    }
                }
                catch (Exception cexc)
                {
                    if (cexc.Message.Contains("Bad PKCS7 padding") || cexc.Message.Contains("Padding is invalid and cannot be removed"))
                    {
                        // attempt to deserialize file with no encryption.
                        cacheIndex = iSerializer.DeserializeObjectFromFile(serializeFileName, EncryptionMode.NoEncryption);
                    }
                    else
                        throw;
                }
                return cacheIndex;
            }
        }

        /// <summary>
        /// Serializes the cache index.
        /// </summary>
        /// <param name="obj">The cache index to serialize.</param>
        public static void SerializeCacheIndex(Object obj)
        {
            if (obj == null)
                return;

            CacheIndex cacheIndex = obj as CacheIndex;

            if (cacheIndex.Count() <= 0)
                return;

            lock (staticLock)
            {
                ISerializer<CacheIndex> iSerializer = SerializerFactory.Create<CacheIndex>(SerializationFormat.XML);
                iSerializer.SerializeObjectToFile(cacheIndex, cacheIndex.SerializeFile);
            }
        }

        DateTime LastSerializationDate = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the serialization interval.
        /// </summary>
        /// <value>The serialization interval.</value>
        public static int SerializationInterval { get; set; }

        /// <summary>
        /// Serializes the cache index.
        /// </summary>
        /// <param name="cacheIndex">The cache index to serialize.</param>
        public static void SerializeCacheIndex(CacheIndex cacheIndex)
        {
            //            // execute serialize cache index off main thread.
            //            Device.Thread.QueueWorker(CacheIndex.SerializeCacheIndex, cacheIndex);

            // Reduce frequency of actual serializations to match the serialization interval.
            if (DateTime.Now.Subtract(cacheIndex.LastSerializationDate).TotalMilliseconds > SerializationInterval || cacheIndex.LastSerializationDate == DateTime.MinValue)
            {
                SerializeCacheIndexImmediate(cacheIndex);
            }
        }

        /// <summary>
        /// Serializes the cache index immediately.
        /// </summary>
        /// <param name="cacheIndex">The cache index to serialize.</param>
        public static void SerializeCacheIndexImmediate(CacheIndex cacheIndex)
        {
            lock (staticLock)
            {
                try
                {
                    cacheIndex.LastSerializationDate = DateTime.Now;
                    ISerializer<CacheIndex> iSerializer = SerializerFactory.Create<CacheIndex>(SerializationFormat.XML);
                    iSerializer.SerializeObjectToFile(cacheIndex, cacheIndex.SerializeFile);
                }
                catch (Exception e)
                {
                    int count = -1;
                    if (cacheIndex != null) { count = cacheIndex.Count; }
                    Device.Log.Error("Exception trying to serialize cache index immediately. Cache index count was: " + count, e);
                }
            }
        }


        /// <summary>
        /// Prefetch property to control the prefetch process for this instance of CacheIndex.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if prefecth is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool PreFetchIndexEnabled { get; set; }

        /// <summary>
        /// Clean index property to control the clean index process for this instance of CacheIndex.
        /// </summary>
        /// <value><c>true</c> if clean index is enabled; otherwise, <c>false</c>.</value>
        public bool CleanIndexEnabled { get; set; }


        /// <summary>
        /// Adds the specified cache index item.
        /// </summary>
        /// <param name="cacheIndexItem">The cache index item.</param>
        public new void Add(CacheIndexItem cacheIndexItem)
        {
            Add(cacheIndexItem, false);
        }

        /// <summary>
        /// Adds the specified cache index item.
        /// </summary>
        /// <param name="cacheIndexItem">The cache index item.</param>
        /// <param name="saveIndex">if set to <c>true</c> save the index to persietent storage.</param>
        public void Add(CacheIndexItem cacheIndexItem, bool saveIndex)
        {
            StringComparer comparer = CacheIndexMap.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            lock (syncLock)
            {
                CacheIndexItem item1 = this.FirstOrDefault(item => comparer.Compare(item.RelativeUri, cacheIndexItem.RelativeUri) == 0);
                if (item1 != null)
                    base.Remove(item1);
                base.Add(cacheIndexItem);

                if (saveIndex)
                    SerializeCacheIndex(this);
            }
        }

        /// <summary>
        /// Updates the index using the specified cache manifest.
        /// </summary>
        /// <param name="manifest">The cache manifest.</param>
        public void UpdateIndex(CacheManifest manifest)
        {
            if (manifest == null || manifest.Cache == null || manifest.Cache.Count == 0)
                return;

            StringComparer comparer = CacheIndexMap.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            lock (syncLock)
            {
                // loop through each string in manifest cache list and create cache index item and add to list.
                foreach (string sCacheItem in manifest.Cache)
                {
                    CacheIndexItem cacheIndexItem = this.FirstOrDefault(item => comparer.Compare(item.RelativeUri, sCacheItem) == 0);

                    if (null == cacheIndexItem)
                    {
                        cacheIndexItem = new CacheIndexItem()
                        {
                            RelativeUri = sCacheItem,
                            PreFetch = true
                        };

                        this.Add(cacheIndexItem, false);
                    }
                }
                // SerializeCacheIndex( this );

                //For all items in index where PreFetch is true, but does not exist in manifest, set PreFetch to false
                IEnumerable<CacheIndexItem> items2 = this.Where(item => item.PreFetch == true);
                foreach (var item in items2)
                {
                    if (!manifest.Cache.Any(cacheItem => comparer.Compare(cacheItem, item.RelativeUri) == 0))
                        item.PreFetch = false;
                }

                SerializeCacheIndex(this);
            }
        }

        /// <summary>
        /// Cleans the index.
        /// </summary>
        /// <param name="obj">The obj.</param>
        public void CleanIndex(object obj)
        {
            if (!this.CleanIndexEnabled)
                return;

            CleanIndex();
        }

        /// <summary>
        /// Cleans the cache index.
        /// </summary>
        public void CleanIndex()
        {
            lock (syncLock)
            {
                if (!this.CleanIndexEnabled)
                    return;

                CacheIndexItem[] items = this.Where(item => !item.PreFetch && item.IsExpired).ToArray();

                int MAX_POSTS = 20;
                int itemCount = items.Length;
                int maxCount = Math.Min(itemCount, MAX_POSTS);

                for (int i = 0; i < maxCount; i++)
                {
                    Device.Thread.QueueIdle(this.RemoveCurrentCache, items[i]);
                }

                if (itemCount > MAX_POSTS)
                    Device.Thread.QueueIdle(this.CleanIndex, (object)null);
            }
        }


        /// <summary>
        /// overload to match ParameterizedThreadStart delegate
        /// </summary>
        /// <param name="obj"></param>
        public void PreFetchItems(object obj)
        {
            if (!this.PreFetchIndexEnabled)
                return;

            PreFetchItems();
        }
        /// <summary>
        /// Prefetches items in CacheIndex list
        /// </summary>
        public void PreFetchItems()
        {
            lock (syncLock)
            {
                if (!this.PreFetchIndexEnabled)
                    return;

                // if cache is not supported then don't bother prefetching.
                if (Device.NetworkGetMethod == MonoCross.Utilities.Network.NetworkGetMethod.NoCache)
                {
                    Device.Log.Debug("Cache is not supported, prefetch request ignored.");
                    return;
                }

                // TO-DO: CacheIndex.PrefetchItems; consider adding a check to see if file is empty or missing and add those to the idle queue as well
                CacheIndexItem[] items = this.Where(item => item.PreFetch && (!item.IsDownloaded || item.IsExpired || item.IsStale)).OrderByDescending(item => item.UsageCount).ToArray();

                int MAX_POSTS = 20;
                int itemCount = items.Count();
                int maxCount = Math.Min(itemCount, MAX_POSTS);

                for (int i = 0; i < maxCount; i++)
                {
                    if (!prefetchItems.Contains(items[i]))
                        prefetchItems.Add(items[i]);
                    Device.Thread.QueueIdle(this.EnsureCurrentCache, items[i]);
                }
                if (itemCount > MAX_POSTS && CacheIndexMap.PrefetchProcessEnabled)
                    Device.Thread.QueueIdle(this.PreFetchItems, (object)null);
                else if (OnPrefetchComplete != null)
                {
                    Device.Thread.QueueWorker(this.WaitForPrefetchComplete, prefetchItems.ToArray());
                    prefetchItems.Clear();
                }
            }
        }

        void WaitForPrefetchComplete(object itemUris)
        {
            var mre = new ManualResetEvent(false);
            while (IsPrefetching)
                mre.WaitOne(1000);
            OnPrefetchComplete(BaseUri, itemUris as CacheIndexItem[]);
        }

        bool IsPrefetching
        {
            get
            {
                return IdleThreadQueue.ThreadCountSafeRead > 0 ||
#if NETCF
                       IdleThreadQueue.Instance.Any(i => i.Delegate.Method.Name == "EnsureCurrentCache") ||
                       IdleThreadQueue.Instance.Any(i => i.Delegate.Method.Name == "PreFetchItems");
#else
                       IdleThreadQueue.Instance.Any(i => i.Delegate.GetMethodInfo().Name == "EnsureCurrentCache") ||
                       IdleThreadQueue.Instance.Any(i => i.Delegate.GetMethodInfo().Name == "PreFetchItems");
#endif
            }
        }

        /// <summary>
        /// Weeds Index: Remove all files in cache folder and subfolders that do not
        /// have corresponding <see cref="CacheIndexItem"/> entries in this cache index.
        /// </summary>
        public void WeedIndex()
        {
            try
            {
                lock (syncLock)
                {
                    // get list of files in cache folder.
                    foreach (var f in GetFilesRecursive(CachePath.AppendPath(BaseUriPath))
                                 .Where(f => !f.Equals(SerializeFile))
                                 .Except(this.Select(i => GetCachePath(i))))
                    {
                        Device.File.Delete(f);
                    }
                }
            }
            catch (Exception exc)
            {
                Device.Log.Error("CacheIndex.WeedIndex() encountered an exception", exc);
            }
        }

        IEnumerable<string> GetDirectoriesRecursive(string directory)
        {
            foreach (var subdir in Device.File.GetDirectoryNames(directory))
            {
                yield return subdir;
                foreach (var file in Device.File.GetDirectoryNames(subdir))
                {
                    yield return file;
                }
            }
        }

        IEnumerable<string> GetFilesRecursive(string directory)
        {
            foreach (var file in GetDirectoriesRecursive(directory).SelectMany(folder => Device.File.GetFileNames(folder)))
            {
                yield return file;
            }
            foreach (var file in Device.File.GetFileNames(directory))
            {
                yield return file;
            }
        }


        /// <summary>
        /// Kills Index: Removes all files in cache folder and all entries in this cache index.
        /// </summary>
        public void KillIndex()
        {
            try
            {
                lock (syncLock)
                {
                    // Remove all files and directories in cache folder subdirectories.
                    var path = CachePath.AppendPath(BaseUriPath);
                    foreach (var d in Device.File.GetDirectoryNames(path))
                        Device.File.DeleteDirectory(d, true);

                    // remove all files in cache folder except for Serializefile
                    foreach (var f in Device.File.GetFileNames(path).Where(f => !f.Equals(SerializeFile)))
                        Device.File.Delete(f);


                    // remove all entries in cache index
                    Clear();

                    // Serialize the CacheIndex
                    SerializeCacheIndex(this);

                }
            }
            catch (Exception exc)
            {
                Device.Log.Error("CacheIndex.KillIndex() encountered an exception", exc);
            }
        }

        /// <summary>
        /// Gets a CacheIndexItem from the CacheIndex for requested uri
        /// </summary>
        /// <param name="uri">Absolute Uri associated with requested CacheIndexItem</param>
        /// <returns></returns>
        public CacheIndexItem Get(string uri)
        {
            return Get(uri, true);
        }

        /// <summary>
        /// Gets a CacheIndexItem from the CacheIndex for requested uri
        /// </summary>
        /// <param name="uri">Absolute Uri associated with requested CacheIndexItem</param>
        /// <param name="addIfNew">if set to <c>true</c> adds the index item if it is not present.</param>
        /// <returns></returns>
        public CacheIndexItem Get(string uri, bool addIfNew)
        {
            if (String.IsNullOrEmpty(uri))
                throw new ArgumentNullException("uri");

            lock (syncLock)
            {
                // relative uri is the uri minus the base uri at the start.
                string relativeUri = GetRelativeUri(uri);
                StringComparer comparer = CacheIndexMap.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

                CacheIndexItem cacheIndexItem = this.FirstOrDefault(item => comparer.Compare(item.RelativeUri, relativeUri) == 0);

                if (null != cacheIndexItem)
                {
                    cacheIndexItem.UsageCount++;
                    cacheIndexItem.PreFetch = true;  // set to true so prefetch functionality will work with the fetched object.
                    return cacheIndexItem;
                }

                if (!addIfNew)
                    return null;

                cacheIndexItem = new CacheIndexItem()
                {
                    RelativeUri = relativeUri,
                    PreFetch = true,
                    UsageCount = 1
                };

                // Add item to the list
                this.Add(cacheIndexItem, true);

                return cacheIndexItem;
            }
        }

        /// <summary>
        /// Gets the relative URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public string GetRelativeUri(string uri)
        {
            //uri = uri.ToLower();

            // if the uri requested doesn't start with this cache index's base uri, 
            // then it doesn't belong to this cache index so return null;
            if (!uri.StartsWith(this.BaseUri))
                return null;

            return uri.Substring(this.BaseUri.Length).RemoveLeadingSlash();
        }

        /// <summary>
        /// Overload to match ParameterizedThreadStart delegate.
        /// </summary>
        /// <param name="obj"></param>
        public void EnsureCurrentCache(Object obj)
        {
            if (obj is CacheIndexItem)
            {
                NetworkResourceArguments args = new NetworkResourceArguments()
                {
                    Headers = Device.RequestInjectionHeaders,
                    CacheStaleMethod = CacheStaleMethod.Immediate
                };
                NetworkResponse NetworkResponse = EnsureCurrentCache((CacheIndexItem)obj, args);
                // To-Do: do something with the NetworkResponse object.
            }
        }
        /// <summary>
        /// Ensures that the CacheIndexItem has a current resource in cache.
        /// </summary>
        public NetworkResponse EnsureCurrentCache(CacheIndexItem cacheIndexItem)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return EnsureCurrentCache(cacheIndexItem, args);
        }

        /// <summary>
        /// Ensures that the CacheIndexItem has a current resource in cache.
        /// </summary>
        public NetworkResponse EnsureCurrentCache(CacheIndexItem cacheIndexItem, NetworkResourceArguments args)
        {
            NetworkResponse networkResponse = new NetworkResponse();

            if (cacheIndexItem == null)
            {
                networkResponse.Message = "Cache Index Item requested argument is null";
                networkResponse.StatusCode = HttpStatusCode.BadRequest;
                return networkResponse;
            }

            string filePath = GetCachePath(cacheIndexItem);

            bool itemInCache = Device.File.Exists(filePath);

            // if item in cache and not expired and not stale, then existing cache is current and no need to download new file
            if (itemInCache && !cacheIndexItem.IsExpired && !cacheIndexItem.IsStale)
            {
                networkResponse.Message = "Existing cache is current, no need to download.";
                networkResponse.Expiration = cacheIndexItem.Expiration;
                networkResponse.Downloaded = cacheIndexItem.Downloaded;
                networkResponse.AttemptToRefresh = cacheIndexItem.AttemptToRefresh;
                networkResponse.StatusCode = HttpStatusCode.OK;

                return networkResponse;
            }

            // if Stale, but not expired, then place separate request on queue to download later on separate process
            if (!cacheIndexItem.IsExpired && cacheIndexItem.IsStale && args.CacheStaleMethod == CacheStaleMethod.Deferred)
            {
                Device.Thread.QueueIdle(this.EnsureCurrentCache, cacheIndexItem);

                networkResponse.Message = "Existing cache is stale, placing refresh request onto idle queue";
                networkResponse.Expiration = cacheIndexItem.Expiration;
                networkResponse.Downloaded = cacheIndexItem.Downloaded;
                networkResponse.AttemptToRefresh = cacheIndexItem.AttemptToRefresh;
                networkResponse.StatusCode = HttpStatusCode.OK;

                return networkResponse;
            }

            // if we got here, then attempt to download file.
            try
            {
                CacheFetcher cacheFetcher = new CacheFetcher();
                networkResponse = cacheFetcher.Fetch(this, cacheIndexItem, args);

#if !SILVERLIGHT
                if (networkResponse.WebExceptionStatusCode != WebExceptionStatus.Success  //.NameResolutionFailure 
                    && args.CacheStaleMethod == CacheStaleMethod.Immediate
                    && itemInCache
                    && !cacheIndexItem.IsExpired
                    && cacheIndexItem.IsStale)
                {
                    // unable to refresh cache file from server but existing cache file is still good.
                    networkResponse.Exception = null;
                    networkResponse.Message = string.Empty;
                    networkResponse.StatusCode = HttpStatusCode.OK;
                    networkResponse.WebExceptionStatusCode = WebExceptionStatus.Success;
                }
#endif

            }
            catch (NetworkResourceLibraryException nrlexc)
            {
                networkResponse.Downloaded = cacheIndexItem.Downloaded;
                networkResponse.AttemptToRefresh = cacheIndexItem.AttemptToRefresh;
                networkResponse.Expiration = cacheIndexItem.Expiration;
                networkResponse.Message = nrlexc.Message;
                networkResponse.Exception = nrlexc;

                return networkResponse;
            }
            finally
            {
                switch (networkResponse.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.ServiceUnavailable:
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.BadGateway:
                    case (HttpStatusCode)(-1):
                    case (HttpStatusCode)(-2):
                        CacheIndexMap.PrefetchProcessEnabled = false;
                        break;
                    default:
                        CacheIndexMap.PrefetchProcessEnabled = true;
                        break;
                }
                CacheIndex.SerializeCacheIndex(this);
            }

            return networkResponse;
        }

        /// <summary>
        /// overload to match ParameterizedThreadStart delegate
        /// </summary>
        /// <param name="obj"></param>
        public void RemoveCurrentCache(object obj)
        {
            if (obj is CacheIndexItem)
                RemoveCurrentCache((CacheIndexItem)obj);
        }

        /// <summary>
        /// Removes cached file from file system and expires metadata. 
        /// </summary>
        /// <param name="cacheIndexItem"></param>
        public void RemoveCurrentCache(CacheIndexItem cacheIndexItem)
        {
            //For all expired items where PreFetch is false, delete the file and then remove the item from the index
            //For all expired items where PreFetch is true, delete the file and set Downloaded to null
            if (cacheIndexItem == null)
                return;

            // delete file if it exists.
            string filePath = GetCachePath(cacheIndexItem);

            if (Device.File.Exists(filePath))
                Device.File.Delete(filePath);

            lock (syncLock)
            {
                if (cacheIndexItem.PreFetch)
                    cacheIndexItem.Expire();
                else
                    Remove(cacheIndexItem);

                CacheIndex.SerializeCacheIndex(this);
            }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <param name="cacheIndexItem">The cache index item.</param>
        /// <param name="networkResponse">The network response.</param>
        /// <returns></returns>
        public string GetFileName(CacheIndexItem cacheIndexItem, out NetworkResponse networkResponse)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders
            };
            return GetFileName(cacheIndexItem, args, out networkResponse);
        }
        /// <summary>
        /// Confirms the requested item is in cache and current, then returns full name to cached file
        /// </summary>
        /// <returns></returns>
        public string GetFileName(CacheIndexItem cacheIndexItem, NetworkResourceArguments args, out NetworkResponse networkResponse)
        {
            networkResponse = null;

            if (cacheIndexItem == null)
                return null;

            // networkResponse = this.EnsureCurrentCache( cacheIndexItem, args );

            string filePath = this.GetCachePath(cacheIndexItem);

            // now that we've checked for and processed the item in the cache, confirm whether it's still there. 
            // and if so, set up a stream and return to calling function
            if (Device.File.Exists(filePath))
                return filePath;

            return null;

        }

        /// <summary>
        /// Gets the cache path.
        /// </summary>
        /// <param name="cacheIndexItem">The cache index item.</param>
        /// <returns></returns>
        public string GetCachePath(CacheIndexItem cacheIndexItem)
        {
            var chars = new[] { '<', '>', ':', '"', '|', '?', '*' };
            string uriString = CachePath.AppendPath(BaseUriPath).AppendPath(new string(cacheIndexItem.ID.ToCharArray().Select(c => chars.Contains(c) ? '_' : c).ToArray()));

            Uri uri = new Uri(uriString, UriKind.RelativeOrAbsolute);

            // Azure has filepath directory length limitations, so enforcing limit of 248 for cache.
            if (!uri.IsAbsoluteUri)
                return uriString;
#if !AZURE
            return uri.LocalPath;

#else
            if (uri.LocalPath.Length <= 247)
                return uri.LocalPath;

            // To-Do: find better way to manage Azure cache folder, this is under a spell.
            // shrink file name to first 227 and last 20, causes cache file name collisions for BestSellers, due to long api key parameter.
            return (uri.LocalPath.Remove(227) + "~" + uri.LocalPath.Substring(uri.LocalPath.Length - 20));
#endif
        }

        /// <summary>
        /// Gets the absolute URI.
        /// </summary>
        /// <param name="cacheIndexItem">The cache index item.</param>
        /// <returns></returns>
        public string GetAbsouteUri(CacheIndexItem cacheIndexItem)
        {
            string path = this.BaseUri.RemoveTrailingSlash();

            path += ("/" + cacheIndexItem.RelativeUri);

            return path;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}", _cachePath);
        }
    }
}
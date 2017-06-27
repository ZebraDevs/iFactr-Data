using MonoCross;
using MonoCross.Utilities;
using MonoCross.Utilities.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
//using iFactr.Core;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// 
    /// </summary>
    public class CacheZip
    {
        const int DefaultTimeout = 300000;  // 5 minutes
        ///// <summary>
        ///// Defines the delegate for factory events
        ///// </summary>
        //public delegate void NetworkResourceLibraryEventHandler();
        //public delegate void NetworkResourceLibraryErrorHandler();

        ///// <summary>
        ///// Occurs when asynch download completes.
        ///// </summary>
        //public event NetworkResourceLibraryEventHandler OnProgressing;
        //public event NetworkResourceLibraryEventHandler OnComplete;
        //public event NetworkResourceLibraryErrorHandler OnError;

        //public Uri ZipUri
        //{
        //    get;
        //    set;
        //}

        //public string ZipFileName
        //{
        //    get
        //    {
        //        string uri = ZipUri.AbsoluteUri;
        //        return uri.Substring( uri.LastIndexOf( '/' ) + 1 );
        //    }
        //}

        //public string ZipBaseUri
        //{
        //    get
        //    {
        //        string uri = ZipUri.AbsoluteUri;
        //        return uri.Remove( uri.LastIndexOf( '/' ) );
        //    }
        //}

        ///// <summary>
        ///// CacheZip Private Constructor
        ///// </summary>
        //private CacheZip()
        //{
        //}

        ///// <summary>
        ///// Factory method to create CacheZip based on URI string
        ///// </summary>
        ///// <param name="zipUri">string representing URI of cache zip file</param>
        ///// <returns>CacheZip</returns>
        //public static CacheZip CreateFromUri( string zipUri )
        //{
        //    // validate parameter
        //    if ( string.IsNullOrEmpty( zipUri ) )
        //        throw new ArgumentNullException( "zipUri" );

        //    // create CacheZip object from manifest string from response
        //    return new CacheZip()
        //    {
        //        ZipUri = new Uri( zipUri )
        //    };

        //}

        /// <summary>
        /// Extracts a resoure into the NRL cache files and applies metadata.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="metadataFileName">Name of the metadata file.</param>
        /// <returns></returns>
        public NetworkResponse Extract(string uri, string metadataFileName)
        {
            return Extract(uri, metadataFileName, Device.RequestInjectionHeaders, DefaultTimeout);
        }
        /// <summary>
        /// Extracts a resoure into the NRL cache files and applies metadata.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="metadataFileName">Name of the metadata file.</param>
        /// <param name="timeout">The timeout for the requst in milliseconds.</param>
        /// <returns></returns>
        public NetworkResponse Extract(string uri, string metadataFileName, int timeout)
        {
            return Extract(uri, metadataFileName, Device.RequestInjectionHeaders, timeout);
        }
        /// <summary>
        /// Extracts a resoure into the NRL cache files and applies metadata.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="metadataFileName">Name of the metadata file.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <returns></returns>
        public NetworkResponse Extract(string uri, string metadataFileName, IDictionary<string, string> headers)
        {
            return Extract(uri, metadataFileName, headers, DefaultTimeout);
        }

        /// <summary>
        /// Extracts a resoure into the NRL cache files and applies metadata.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="metadataFileName">Name of the metadata file.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="timeout">The timeout for the requst in milliseconds.</param>
        /// <returns></returns>
        public NetworkResponse Extract(string uri, string metadataFileName, IDictionary<string, string> headers, int timeout)
        {
            return Extract(uri, metadataFileName, headers, timeout, null, SerializationFormat.XML);
        }
        /// <summary>
        /// Extracts a resoure into the NRL cache files and applies metadata.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="metadataFileName">Name of the metadata file.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="timeout">The timeout for the requst in milliseconds.</param>
        /// <param name="postObject">The object to post.</param>
        /// <param name="serializationFormat">The serialization format.</param>
        /// <returns></returns>
        public NetworkResponse Extract(string uri, string metadataFileName, IDictionary<string, string> headers, int timeout,
            object postObject, SerializationFormat serializationFormat)
        {
            // if cache is not supported then don't bother extracting.
            if (Device.NetworkGetMethod == MonoCross.Utilities.Network.NetworkGetMethod.NoCache)
            {
                Device.Log.Debug("Cache is not supported, Zip file extraction request ignored.");
                return new NetworkResponse()
                {
                    StatusCode = HttpStatusCode.OK,
                    Message = "Cache is not supported, Zip file extraction request ignored.",
                    URI = uri,
                    Verb = "GET"
                };
            }

            // validate parameter
            if (string.IsNullOrEmpty(uri))
                throw new ArgumentNullException("uri");

            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                throw new ArgumentNullException("Cache Zip location is not a valid Absolute URI " + uri, "uri");

            DateTime dtMetric = DateTime.UtcNow;

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(uri);
            CacheIndexItem zipCacheIndexItem = cacheIndex.Get(uri);
            zipCacheIndexItem.PreFetch = false;

            NetworkResponse networkResponse = null;

            bool idleQueueEnabled = Device.Thread.IdleQueueEnabled;
            try
            {
                // temporarily deactivate idle queue thread.
                if (idleQueueEnabled)
                    Device.Thread.IdleQueueEnabled = false;

                // download zip file from uri
                string zipFileName = cacheIndex.GetCachePath(zipCacheIndexItem);
                networkResponse = DownloadZipFile(uri, zipFileName, headers, timeout, postObject); //, serializationFormat );

                if (networkResponse.StatusCode != HttpStatusCode.OK)
                    return networkResponse;

                string cachePath = cacheIndex.CachePath.AppendPath(cacheIndex.BaseUriPath);

                // extract zip file into NRL cache
                networkResponse.Message = "Extract Zip File";
                ExtractZipFile(zipFileName, cachePath);

                // process metadata for extracted files.
                networkResponse.Message = "Import Metadata";
                metadataFileName = cacheIndex.CachePath.AppendPath(cacheIndex.BaseUriPath).AppendPath(metadataFileName);
                ImportMetadata(metadataFileName, cacheIndex);

                Device.Log.Metric(string.Format("Extract and process zip file: file: {0} Time: {1} milliseconds", zipFileName, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

                networkResponse.Message = "Complete";
            }
            //catch ( WebException ex )
            //{
            //    if ( ex.Response != null )
            //    {
            //        networkResponse.StatusCode = ( (HttpWebResponse) ex.Response ).StatusCode;
            //        ex.Data["StatusDescription"] = ( (HttpWebResponse) ex.Response ).StatusDescription;
            //    }
            //    else
            //    {
            //        networkResponse.StatusCode = (HttpStatusCode) ( -2 );
            //    }
            //    networkResponse.WebExceptionStatusCode = ex.Status;

            //    Device.Log.Error( ex );
            //    networkResponse.Exception = ex;
            //}
            catch (Exception exc)
            {
                Device.Log.Error(exc);
                networkResponse.Exception = exc;
                networkResponse.StatusCode = (HttpStatusCode)(-1);
            }
            finally
            {
                // remove zip file after processing is complete. perhaps in a finally block
                if (cacheIndex != null && zipCacheIndexItem != null)
                {
                    cacheIndex.RemoveCurrentCache(zipCacheIndexItem);
                    // cacheIndex.Remove( zipCacheIndexItem );
                }

                // reactivate queue thread if it was previously active.
                if (idleQueueEnabled)
                    Device.Thread.IdleQueueEnabled = true;

                // Serialize Cache Index due to changes.
                CacheIndex.SerializeCacheIndex(cacheIndex);
            }

            return networkResponse;
        }


        /// <summary>
        /// Downloads a zip file resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="filename">The filename for the resource.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="timeout">The request timeout in milliseconds.</param>
        /// <param name="postObject">The object to post.</param>
        /// <returns></returns>
        private static NetworkResponse DownloadZipFile(string uri, string filename, IDictionary<string, string> headers,
                                int timeout, object postObject)  // really want this, SerializationFormat serializationFormat)
        {
            DateTime dtMetric = DateTime.UtcNow;

            // obtain resource from network, with 5 minute timeout
            NetworkResponse networkResponse = null;
            if (postObject != null)
            {
                networkResponse = Device.Network.Poster.PostObject(uri, postObject, "POST", headers, timeout);  // serializationFormat
                if (networkResponse.StatusCode == HttpStatusCode.OK)
                    Device.File.Save(filename, networkResponse.ResponseBytes);
            }
            else
            {
                networkResponse = Device.Network.Fetcher.Fetch(uri, filename, headers, timeout);
            }

            Device.Log.Metric(string.Format("DownloadZipFile: Uri: {0}  Time: {1} milliseconds", uri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

            return networkResponse;
        }

        /// <summary>
        /// Extracts the zip file.
        /// </summary>
        /// <param name="zipFileName">Name of the zip file.</param>
        /// <param name="cachePath">The cache path.</param>
        public static void ExtractZipFile(string zipFileName, string cachePath)
        {
            DateTime dtMetric = DateTime.UtcNow;

			// read temp file iFactr APIs because file saved to disk with encryption
            var zipBytes = new MemoryStream(Device.File.Read(zipFileName));

            using (var zip = Ionic.Zip.ZipFile.Read(zipBytes))
                foreach (var entry in zip)
                    entry.Extract(cachePath, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);

            Device.Log.Metric(string.Format("Extract zip file: file: {0} cache path: {1}  Time: {2:0} milliseconds", zipFileName, cachePath, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));
        }

        private static void ImportMetadata(string metadataFileName, CacheIndex cacheIndex)
        {
            DateTime dtMetric = DateTime.UtcNow;

            ISerializer<CacheIndexItem> iSerializer = SerializerFactory.Create<CacheIndexItem>(SerializationFormat.XML);
            List<CacheIndexItem> listCache = iSerializer.DeserializeListFromFile(metadataFileName);

            for (int i = 0; i < listCache.Count; i++)
            {
                // add index item, don't save index until all Adds have been completed.
                var cacheItem = listCache[i];
                if (cacheItem != null)
                    cacheIndex.Add(cacheItem, false);
            }

            // Serialize Cache Index due to changes.
            CacheIndex.SerializeCacheIndex((object)cacheIndex);

            Device.File.Delete(metadataFileName);

            Device.Log.Metric(string.Format("Import Metadata: file: {0} Time: {1} milliseconds", metadataFileName, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));
        }
    }
}
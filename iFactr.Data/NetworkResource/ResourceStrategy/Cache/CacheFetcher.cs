using MonoCross;
using MonoCross.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Synchronous wrapper class around CacheFetcherAsynch
    /// </summary>
    internal class CacheFetcher
    {
        private readonly ManualResetEvent _allDone = new ManualResetEvent(false);
        private readonly AutoResetEvent _autoEvent = new AutoResetEvent(false);

        //private object padLock = new object();
        //NetworkResponse _networkResponse = new NetworkResponse();
        private NetworkResponse PostNetworkResponse
        {
            get;
            //{
            //lock ( padLock )
            //{
            //    return _networkResponse;
            //}
            //}
            set;
            // {
            //lock ( padLock )
            //{
            //    _networkResponse = value;
            //}
            //}
        }

        /// <summary>
        /// Defines the delegate for factory events
        /// </summary>
        public delegate void NetworkResourceLibraryEventHandler(RequestState state);
        public delegate void NetworkResourceLibraryErrorHandler(RequestState state);

        /// <summary>
        /// Occurs when asynch download completes.
        /// </summary>
        public event NetworkResourceLibraryEventHandler OnDownloadComplete;
        public event NetworkResourceLibraryErrorHandler OnError;

        public NetworkResponse Fetch(CacheIndex cacheIndex, CacheIndexItem cacheIndexItem, int timeoutMilliseconds)
        {
            NetworkResourceArguments args = new NetworkResourceArguments() {
                Headers = Device.RequestInjectionHeaders,
                TimeoutMilliseconds = timeoutMilliseconds,
            };
            return Fetch(cacheIndex, cacheIndexItem, args);
        }

        /// <summary>
        /// Fetches the specified cache index item from the cache index.
        /// </summary>
        /// <param name="cacheIndex">Index of the cache.</param>
        /// <param name="cacheIndexItem">The cache index item.</param>
        /// <param name="args">NetworkResourceArguments for the request</param>
        /// <returns></returns>
        public NetworkResponse Fetch(CacheIndex cacheIndex, CacheIndexItem cacheIndexItem, NetworkResourceArguments args) //IDictionary<string, string> headers, int timeoutMilliseconds)
        {
            PostNetworkResponse = new NetworkResponse();
            var fetchParameters = new FetchParameters
            {
                CacheIndex = cacheIndex,
                CacheIndexItem = cacheIndexItem,
                Headers = args.Headers,
                DefaultExpiration = args.Expiration,
            };
            int timeoutMilliseconds = args.TimeoutMilliseconds;

            DateTime dtMetric = DateTime.UtcNow;

            // set callback and error handler
            OnDownloadComplete += CacheFetcher_OnDownloadComplete;
            OnError += CacheFetcher_OnError;

            Exception threadExc = null;
            Device.Thread.QueueWorker(parameters =>
            {
                try
                {
                    FetchAsynch(parameters, timeoutMilliseconds);
                }
                catch (Exception e)
                {
                    // You could react or save the exception to an 'outside' variable 
                    threadExc = e;    
                }
                finally
                {
                    _autoEvent.Set(); // if you're firing and not forgetting ;)    
                }
            }, fetchParameters);

            // WaitOne returns true if autoEvent were signaled (i.e. process completed before timeout expired)
            // WaitOne returns false it the timeout expired before the process completed.
#if NETCF
            if (!_autoEvent.WaitOne(timeoutMilliseconds, false))
#else
            if (!_autoEvent.WaitOne(timeoutMilliseconds))
#endif
            {
                string message = "CacheFetcher call to FetchAsynch timed out. uri " + fetchParameters.CacheIndexItem.RelativeUri;
                Device.Log.Metric(string.Format("CacheFetcher timed out: Uri: {0} Time: {1:F0} milliseconds ", fetchParameters.CacheIndexItem.RelativeUri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

                var networkResponse = new NetworkResponse
                {
                    Message = message,
                    URI = fetchParameters.CacheIndex.GetAbsouteUri(fetchParameters.CacheIndexItem),
                    StatusCode = HttpStatusCode.RequestTimeout,
                    WebExceptionStatusCode = WebExceptionStatus.RequestCanceled, // not using ConnectFailure because connection may have succeeded
                    ResponseString = string.Empty,
                    Expiration = DateTime.MinValue.ToUniversalTime(),
                    Downloaded = DateTime.MinValue.ToUniversalTime(),
                    AttemptToRefresh = DateTime.MinValue.ToUniversalTime(),
                    Exception = threadExc,
                };

                return networkResponse;
            }
            else if (threadExc != null)
            {
                PostNetworkResponse.Exception = threadExc;
                PostNetworkResponse.Message = "CacheFetcher.FetchAsync threw an exception";
                PostNetworkResponse.StatusCode = (HttpStatusCode)(-1);
            }

            Device.Log.Metric(string.Format("CacheFetcher Completed: Uri: {0} Time: {1:F0} milliseconds ", PostNetworkResponse.URI, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

            return PostNetworkResponse;
        }

        void CacheFetcher_OnError(RequestState state)
        {
            var exc = new Exception("CacheFetcher call to FetchAsynch threw an exception", state.Exception);
            var webEx = state.Exception as WebException;
            if (webEx != null)
            {
                if (state != null && state.StatusCode == HttpStatusCode.NotModified)
                {
                    Device.Log.Info("Not Modified returned by CacheFetcher for resource uri: {0}", state.Request.RequestUri);
                }
                else if (webEx.Message.Contains("Network is unreachable") ||
                    webEx.Message.Contains("Error: ConnectFailure") || // iOS Message when in Airplane mode
                    webEx.Message.Contains("The remote name could not be resolved:") || // Windows message when no network access
                    webEx.Message.Contains("(304) Not Modified")) // HttpWebResponse.EndGetResponse throws exception on HttpStatusCode.NotModified response
                {
                    Device.Log.Info(exc);
                }
                else { Device.Log.Error(exc);  }
            }
            else if (state.StatusCode == HttpStatusCode.RequestTimeout)
            {
                Device.Log.Info(exc);
            }
            else if (state.Exception != null && state.Exception.Message != null &&
                     state.Exception.Message.Contains("Error: NameResolutionFailure"))
            {
                Device.Log.Info(exc);
            }
            else { Device.Log.Error(exc); }

            PostNetworkResponse.StatusCode = state.StatusCode;
            PostNetworkResponse.Message = exc.Message;
            PostNetworkResponse.Exception = exc;
            PostNetworkResponse.URI = state.AbsoluteUri;
            PostNetworkResponse.Verb = state.Verb;
            PostNetworkResponse.ResponseString = null;
            PostNetworkResponse.ResponseBytes = null;
            PostNetworkResponse.WebExceptionStatusCode = state.WebExceptionStatusCode;

            Device.PostNetworkResponse(PostNetworkResponse);

            _autoEvent.Set(); // release the Fetch(...) call
        }

        void CacheFetcher_OnDownloadComplete(RequestState state)
        {
            PostNetworkResponse.StatusCode = state.StatusCode;
            PostNetworkResponse.URI = state.AbsoluteUri;
            PostNetworkResponse.Verb = state.Verb;
            PostNetworkResponse.ResponseString = state.ResponseString;
            PostNetworkResponse.ResponseBytes = state.ResponseBytes;
            PostNetworkResponse.Expiration = state.Expiration;
            PostNetworkResponse.AttemptToRefresh = state.AttemptToRefresh;
            PostNetworkResponse.Downloaded = state.Downloaded;
            PostNetworkResponse.Message = state.ErrorMessage;

            switch (PostNetworkResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.Accepted:
                    // things are ok, no event required
                    break;
                case HttpStatusCode.NoContent:           // return when an object is not found
                case HttpStatusCode.Unauthorized:        // return when session expires
                case HttpStatusCode.InternalServerError: // return when an exception happens
                case HttpStatusCode.ServiceUnavailable:  // return when the database or siteminder are unavailable
                    PostNetworkResponse.Message = String.Format("Network Service responded with status code {0}", state.StatusCode);
                    Device.PostNetworkResponse(PostNetworkResponse);
                    break;
                default:
                    PostNetworkResponse.Message = String.Format("CacheFetcher completed but received HTTP {0}", state.StatusCode);
                    Device.Log.Error(PostNetworkResponse.Message);
                    Device.PostNetworkResponse(PostNetworkResponse);
                    return;
            }
        }

        /// <summary>
        /// Performs an asynchronous fetch from the cache index.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="timeoutMilliseconds">The timeout milliseconds.</param>
        public void FetchAsynch(Object parameters, int timeoutMilliseconds)
        {
            var fetchParameters = (FetchParameters)parameters;

            var request = (HttpWebRequest)WebRequest.Create(fetchParameters.CacheIndex.GetAbsouteUri(fetchParameters.CacheIndexItem));
            request.Method = "GET";
            //request.Proxy = null;

#if !SILVERLIGHT && !NETFX_CORE && !PCL
            request.AutomaticDecompression = DecompressionMethods.GZip;
#endif

            if (fetchParameters.Headers != null && fetchParameters.Headers.Any())
            {
                foreach (string key in fetchParameters.Headers.Keys)
                {
                    if (key.ToLower() == "accept")
                    {
                        request.Accept = fetchParameters.Headers[key];
                    }
                    else if (key.ToLower() == "content-type")
                    {
                        request.ContentType = fetchParameters.Headers[key];
                    }
                    else if (key.ToLower() == "host")
                    {
                        Exception ex;
#if NETCF
                        ex = new ArgumentException("Host header value cannot be set in Compact Frameword libraries.");
#else
                        //TODO: add the URL explaining PCL incompatibility
                        ex = new ArgumentException("Host header value cannot be set in PCL libraries.");
#endif
                        Device.Log.Error(ex);
                        throw ex;
                    }
                    else
                    {
                        request.Headers[key] = fetchParameters.Headers[key];
                    }
                }
            }

            RequestState state = new RequestState()
             {
                 Request = request,
                 CacheFileName = fetchParameters.CacheIndex.GetCachePath(fetchParameters.CacheIndexItem),
                 CacheIndex = fetchParameters.CacheIndex,
                 CacheIndexItem = fetchParameters.CacheIndexItem,
                 RelativeUri = fetchParameters.CacheIndexItem.RelativeUri,
                 BaseUri = fetchParameters.CacheIndex.BaseUri,
                 Expiration = DateTime.UtcNow.Add(fetchParameters.DefaultExpiration),
             };

            try
            {
                // Start the asynchronous request.
                IAsyncResult result = request.BeginGetResponse(ResponseCallback, state);
#if NETCF
                if (!_allDone.WaitOne(timeoutMilliseconds, false))
#else
                if (!_allDone.WaitOne(timeoutMilliseconds))
#endif
                {
                    try { request.Abort(); } catch (Exception) { } // .Abort() always throws exception
                    return;
                }
            }
            catch (Exception exc)
            {
                Device.Log.Error("CacheFetcher.FetchAsynch encountered exception", exc);
                _autoEvent.Set();
            }
        }

        // Define other methods and classes here
        private void ResponseCallback(IAsyncResult result)
        {
            // Get and fill the RequestState
            RequestState state = (RequestState)result.AsyncState;

            try
            {
                bool downloadFile = false;
                HttpWebRequest request = state.Request;

                // End the Asynchronous response and get the actual response object
                state.Response = (HttpWebResponse)request.EndGetResponse(result);

                state.StatusCode = state.Response.StatusCode;

                switch (state.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                    case HttpStatusCode.Accepted:
                        state.Downloaded = DateTime.UtcNow;
                        break;
                    case HttpStatusCode.NoContent:
                        Device.Log.Info("Empty payload returned in CacheFetcher: Result {0} for {1}", state.StatusCode, request.RequestUri);
                        state.Expiration = DateTime.UtcNow;
                        state.AttemptToRefresh = DateTime.UtcNow;
                        state.Downloaded = DateTime.UtcNow;
                        OnDownloadComplete(state);
                        return;
                    default:
                        state.ErrorMessage = String.Format("Get failed. Received HTTP {0} for {1}", state.StatusCode, request.RequestUri);
                        Device.Log.Error(state.ErrorMessage);
                        state.Expiration = DateTime.UtcNow;
                        state.AttemptToRefresh = DateTime.UtcNow;
                        state.Downloaded = DateTime.UtcNow;
                        OnDownloadComplete(state);

                        return;
                }

                #region Determine whether new version of file needs to be downloaded.

                // create storage directory if it doesn't exist.
                Device.File.EnsureDirectoryExistsForFile(state.CacheFileName);

                string tempCacheFile = state.CacheFileName + "_" + DateTime.Now.Ticks.ToString() + ".tmp";

                bool itemInCache = Device.File.Exists(state.CacheFileName);

                Device.Log.Debug("CacheFetcher.ResponseCallback Uri: {0}  IsExpired: {1} ", state.AbsoluteUri, state.CacheIndexItem.IsExpired );
                Device.Log.Debug("CacheFetcher.ResponseCallback Uri: {0}  IsStale: {1} ", state.AbsoluteUri, state.CacheIndexItem.IsStale );
                Device.Log.Debug("CacheFetcher.ResponseCallback Uri: {0}  Downloaded: {1} ", state.AbsoluteUri, state.CacheIndexItem.Downloaded );
                Device.Log.Debug("CacheFetcher.ResponseCallback Uri: {0}  Header Last-Modified: {1} ", state.AbsoluteUri, state.Response.Headers["Last-Modified"].TryParseDateTimeUtc() );
                Device.Log.Debug("CacheFetcher.ResponseCallback Uri: {0}  Header Etag: {1}  NRL: Etag: {2}", state.AbsoluteUri, state.Response.Headers["Etag"], state.CacheIndexItem.ETag );

                if (!itemInCache)
                {
                    downloadFile = true;
                }
                else if (state.CacheIndexItem.IsExpired || state.CacheIndexItem.IsStale)
                {
                    // At this point, since the cached item is "old", assume the item needs to be downloaded...
                    downloadFile = true;

                    // ... but check headers for actual content expiration.  If the file in cache hasn't changed on 
                    // the web server, we won't waste any time downloading the same thing we've already got.
                    if (state.Response.Headers["Last-Modified"].TryParseDateTimeUtc() < state.CacheIndexItem.Downloaded && state.CacheIndexItem.Downloaded > DateTime.MinValue.ToUniversalTime())
                    {
                        Device.Log.Debug("CacheFetcher.ResponseCallback.Download Check: Unchanged since Last-Downloaded value");
                       
                        downloadFile = false;
                    }
                    else if (state.Response.Headers["ETag"] != null && state.CacheIndexItem.ETag != null && state.CacheIndexItem.ETag == state.Response.Headers["ETag"])
                    {
                        Device.Log.Debug("CacheFetcher.ResponseCallback.Download Check: Etag matched");

                        downloadFile = false;
                    }
                }

                #endregion

                #region Download file if necessary

                DateTime dtMetric;

                if (downloadFile)
                {
                    //To-Do: refactor streamreaders/writers conversion to strings to a more efficient streaming mechanism
                    Stream httpStream = null;
                    try
                    {
                        httpStream = state.Response.GetResponseStream();

                        dtMetric = DateTime.UtcNow;


                        using (var ms = new MemoryStream())
                        {
#if NETCF
                            var buffer = new byte[16 * 1024]; // Fairly arbitrary size
                            int bytesRead;

                            while ((bytesRead = httpStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }
#else
                            httpStream.CopyTo(ms);
#endif
                            state.ResponseBytes = ms.ToArray();
                            state.ResponseString = Encoding.UTF8.GetString(state.ResponseBytes, 0, state.ResponseBytes.Length);
                        }

                        // pre download work.
                        // if the item is in cache and expired then delete it
                        if (itemInCache)
                        {
                            Device.File.Delete(state.CacheFileName);
                            itemInCache = false;
                        }
                        Device.File.Save(tempCacheFile, state.ResponseBytes);

                        Device.Log.Metric(string.Format("CacheFetcher save stream to temp file: Name: {0} Time: {1:F0} milliseconds ", tempCacheFile, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

                        // Set the CacheIndexItem properties upon successful download.
                        state.CacheIndexItem.Downloaded = state.Downloaded;
                        state.CacheIndexItem.Expiration = SetExpirationTime(state);

                        state.CacheIndexItem.AttemptToRefresh = state.Response.Headers["iFactr-Attempt-Refresh"].TryParseDateTimeUtc();
                        state.CacheIndexItem.ETag = state.Response.Headers["ETag"];
                        state.CacheIndexItem.ContentType = state.Response.Headers["Content-Type"];

                        state.AttemptToRefresh = state.CacheIndexItem.AttemptToRefresh;
                        state.Expiration = state.CacheIndexItem.Expiration;
                    }
                    finally
                    {
                        if (httpStream != null)
                        {
#if !NETFX_CORE && !PCL
                            httpStream.Close();
#endif
                            httpStream.Dispose();
                        }

                        if (state.Response != null)
#if NETFX_CORE || PCL
                            state.Response.Dispose();
#else
                            state.Response.Close();
#endif
                    }

                    // move downloaded tmp file to cache file location.
                    if (Device.File.Length(tempCacheFile) == 0 && !(state.Response != null && state.Response.StatusCode == HttpStatusCode.OK))
                    {
                        Device.File.Delete(tempCacheFile);
                        string error = "File Download returned an empty file.  Validate the URI is correct. uri=" + state.Request.RequestUri;
                        //throw new NetworkResourceLibraryException( error );
                        Device.Log.Error(error);
                    }
                    else
                    {
                        dtMetric = DateTime.UtcNow;

                        if (Device.File.Exists(state.CacheFileName))
                            Device.File.Delete(state.CacheFileName);

                        Device.File.Move(tempCacheFile, state.CacheFileName);
                        Device.Log.Metric(string.Format("CacheFetcher move temp file to cache file: Name: {0}  Length: {1}  Time: {2:0} milliseconds ", state.CacheFileName, Device.File.Length(state.CacheFileName), DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));
                    }
                }
                else  // downloadFile == false
                {
                    Device.Log.Debug("CacheFetcher.ResponseCallback.Download Check: No need to download, file is current");

                    // update cache index item with current values
                    state.CacheIndexItem.Downloaded = state.Response.Headers["Date"].TryParseDateTimeUtc();
                    state.CacheIndexItem.Expiration = SetExpirationTime(state);

                    state.CacheIndexItem.AttemptToRefresh = state.Response.Headers["iFactr-Attempt-Refresh"].TryParseDateTimeUtc();
                    state.CacheIndexItem.ETag = state.Response.Headers["ETag"];
                    state.CacheIndexItem.ContentType = state.Response.Headers["Content-Type"];
                }

                #endregion

                OnDownloadComplete(state);

            }
            catch (WebException ex)
            {
                string StatusDescription = string.Empty;
#if !NETCF
                ex.Data.Add("Uri", state.Request.RequestUri);
                ex.Data.Add("Verb", state.Request.Method);
#endif
                if (ex.Response != null)
                {
                    state.StatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                    StatusDescription = ((HttpWebResponse)ex.Response).StatusDescription;
                }
                else if (ex.Message.ToLower().Contains("request was aborted"))
                {
                    state.StatusCode = HttpStatusCode.RequestTimeout;
                    StatusDescription = "Request cancelled by client because the server did not respond within timeout";
                }
                else
                {
                    state.StatusCode = (HttpStatusCode)(-2);
                }
                state.WebExceptionStatusCode = ex.Status;
#if !NETCF
                ex.Data.Add("StatusCode", state.StatusCode);
                ex.Data.Add("WebException.Status", ex.Status);
                ex.Data.Add("StatusDescription", StatusDescription);
#endif
                state.ErrorMessage = string.Format("Call to {0} had a WebException. {1}   Status: {2}   Desc: {3}", state.Request.RequestUri, ex.Message, ex.Status, StatusDescription);
                state.Exception = ex;
                state.Expiration = DateTime.UtcNow;
                state.AttemptToRefresh = DateTime.UtcNow;
                state.Downloaded = DateTime.UtcNow;

                OnError(state);
            }
            catch (Exception ex)
            {
#if !NETCF
                ex.Data.Add("Uri", state.Request.RequestUri);
                ex.Data.Add("Verb", state.Request.Method);
#endif
                state.ErrorMessage = string.Format("Call to {0} had an Exception. {1}", state.Request.RequestUri, ex.Message);
                state.Exception = ex;
                state.StatusCode = (HttpStatusCode)(-1);
                state.Expiration = DateTime.UtcNow;
                state.AttemptToRefresh = DateTime.UtcNow;
                state.Downloaded = DateTime.UtcNow;

                OnError(state);
            }
            finally
            {
                if (state.Response != null)
#if NETFX_CORE || PCL
                    state.Response.Dispose();
#else
                    state.Response.Close();
#endif
                state.Request = null;

                _allDone.Set();
            }
        }

        private DateTime SetExpirationTime(RequestState state)
        {
            DateTime expirationTime = DateTime.UtcNow.AddHours(1);
            DateTime defaultExpirationFromServer = new DateTime(0);

            if (state != null && state.Response != null)
                defaultExpirationFromServer = state.Response.Headers["Expires"].TryParseDateTimeUtc();
            

            if (defaultExpirationFromServer != DateTime.MinValue.ToUniversalTime())
                expirationTime = defaultExpirationFromServer; // default to value provided by server
            else if (state.Expiration != null && state.Expiration.Ticks != 0)
                expirationTime = state.Expiration; // otherwise use value provided by developer
            else
                expirationTime = state.CacheIndexItem.Downloaded.AddHours(1); // otherwise fall back to old default

            return expirationTime;
        }

        public class FetchParameters
        {
            public CacheIndex CacheIndex
            {
                get;
                set;
            }
            public CacheIndexItem CacheIndexItem
            {
                get;
                set;
            }
            public IDictionary<string, string> Headers
            {
                get;
                set;
            }

            public TimeSpan DefaultExpiration
            {
                get;
                set;
            }
        }

    }


    /// <summary>
    /// subclass to store information for Asynchronous file
    /// </summary>
    public class RequestState
    {
        /// <summary>
        /// Gets or sets the size of the buffer.
        /// </summary>
        /// <value>The size of the buffer.</value>
        public int BufferSize
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public HttpWebRequest Request
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        /// <value>The response.</value>
        public HttpWebResponse Response
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the response string.
        /// </summary>
        /// <value>The response string.</value>
        public string ResponseString
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the response bytes.
        /// </summary>
        /// <value>The response bytes.</value>
        public byte[] ResponseBytes
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        /// <value>The expiration date.</value>
        public DateTime Expiration
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the downloaded date.
        /// </summary>
        /// <value>The downloaded date.</value>
        public DateTime Downloaded
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the relative URI.
        /// </summary>
        /// <value>The relative URI.</value>
        public string RelativeUri
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the base URI.
        /// </summary>
        /// <value>The base URI.</value>
        public string BaseUri
        {
            get;
            set;
        }
        /// <summary>
        /// Gets the absolute URI.
        /// </summary>
        /// <value>The absolute URI.</value>
        public string AbsoluteUri
        {
            get
            {
                return BaseUri.AppendPath(RelativeUri);
            }
        }
        /// <summary>
        /// Gets or sets the HTTP verb.
        /// </summary>
        /// <value>The HTTP verb.</value>
        public string Verb
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the name of the cache file.
        /// </summary>
        /// <value>The name of the cache file.</value>
        public string CacheFileName
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the cache index.
        /// </summary>
        /// <value>The cache index.</value>
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
        /// Gets or sets the HTTP status code.
        /// </summary>
        /// <value>The status HTTP code.</value>
        public HttpStatusCode StatusCode
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the web exception status code.
        /// </summary>
        /// <value>The web exception status code.</value>
        public WebExceptionStatus WebExceptionStatusCode
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string ErrorMessage
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<string, string> Data
        {
            get;
            set;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestState"/> class.
        /// </summary>
        public RequestState()
        {
            BufferSize = 6 * 1024;
            Request = null;
            Response = null;
            CacheFileName = null;
        }

        /// <summary>
        /// Gets or sets the attempt to refresh date.
        /// </summary>
        /// <value>The attempt to refresh date.</value>
        public DateTime AttemptToRefresh
        {
            get;
            set;
        }
    }

}


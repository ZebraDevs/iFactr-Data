using MonoCross;
using System;
using System.Net;

namespace iFactr.Data
{
    /// <summary>
    /// The source of the object on the provider response.
    /// </summary>
    public enum ObjectSource
    {
        /// <summary>
        /// Object source unknown, or not specified.
        /// </summary>
        NotSpecified,
        /// <summary>
        /// Object source is the in-memory provider cache.
        /// </summary>
        InMemoryCache,
        /// <summary>
        /// Object source is the in-memory provider queue.
        /// </summary>
        InMemoryQueue,
        /// <summary>
        /// Object source is the persistent cache, (NRL).
        /// </summary>
        NRLCache,
        /// <summary>
        /// Object source is the server.
        /// </summary>
        Server,
        /// <summary>
        /// Object source is a local file.
        /// </summary>
        LocalFile
    }
    /// <summary>
    /// Represents a data provider response to a RESTful request.
    /// </summary>
    /// <typeparam name="T">The type of the item returned on the response.</typeparam>
    public class ProviderResponse<T>
    {
        /// <summary>
        /// Gets the DateTime value of when resource will next be refreshed
        /// </summary>
        public virtual DateTime AttemptToRefresh
        {
            get;
            private set;
        }
        /// <summary>
        /// Creates a provider response based on the specified network response.
        /// </summary>
        /// <param name="networkResponse">The network response used to create the provider response.</param>
        /// <returns></returns>
        public static ProviderResponse<T> Create( NetworkResponse networkResponse )
        {
            ProviderResponse<T> response = new ProviderResponse<T>()
            {
                Expiration = networkResponse.Expiration,
                Downloaded = networkResponse.Downloaded,
                Exception = networkResponse.Exception,
                StatusCode = networkResponse.StatusCode,
                Message = networkResponse.Message,
                WebExceptionStatusCode = networkResponse.WebExceptionStatusCode,
                ResponseBytes = networkResponse.ResponseBytes,
                ResponseString = networkResponse.ResponseString,
                AttemptToRefresh = networkResponse.AttemptToRefresh,
            };

            return response;
        }
        /// <summary>
        /// Gets or sets the object source.
        /// </summary>
        /// <value>The object source of the response.</value>
        public ObjectSource ObjectSource
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the expiration date on the response.
        /// </summary>
        /// <value>The cache expiration date of the item on the response.</value>
        public DateTime Expiration
        {
            get;
            set;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is expired.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is expired; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpired
        {
            get
            {
                // treat DateTime.MinValue as "never expires"
                if ( Expiration > DateTime.MinValue.ToUniversalTime() && DateTime.UtcNow >= Expiration )
                    return true;
                else
                    return false;
            }
        }
        /// <summary>
        /// Gets or sets the downloaded date of the response.
        /// </summary>
        /// <value>The downloaded date of the provider response.</value>
        public DateTime Downloaded
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the status code of the response.
        /// </summary>
        /// <value>The HTTP status code.</value>
        public HttpStatusCode StatusCode
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the web exception status code of the response.
        /// </summary>
        /// <value>The web exception status code.</value>
        public WebExceptionStatus WebExceptionStatusCode
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the message of the response.
        /// </summary>
        /// <value>The message as string value.</value>
        public string Message
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the exception of the response.
        /// </summary>
        /// <value>The exception, if present, of the provider response.</value>
        public Exception Exception
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the source of the response.
        /// </summary>
        /// <value>The source as a string value.</value>
        /// <summary>
        /// Gets or sets the body of the response from the server as a <see cref="System.String"/>.
        /// </summary>
        public string ResponseString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the body of the response from the server as an array of <see cref="System.Byte"/>s.
        /// </summary>
        public byte[] ResponseBytes
        {
            get;
            set;
        }

        public string Source
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the item object on the response.
        /// </summary>
        /// <value>The object/item associated with the response.</value>
        public T Object
        {
            get;
            set;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderResponse&lt;T&gt;"/> class.
        /// </summary>
        public ProviderResponse()
        {
            AttemptToRefresh = new DateTime(0);
        }
    }
}

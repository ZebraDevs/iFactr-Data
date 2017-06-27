using MonoCross.Utilities;
using System;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.DirectStream
{
    /// <summary>
    /// Represents a direct stream resource response.
    /// </summary>
    public class ResourceResponseDirectStream : ResourceResponse
    {
        internal string ResponseString
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceResponseDirectStream"/> class.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="args">The arguments used on the request.</param>
        public ResourceResponseDirectStream( string uri, NetworkResourceArguments args )
        {
            if (uri == null || args == null)
            {
                throw new ArgumentNullException("Arguments cannot be null");
            }
            Uri = uri;
            NetworkResourceArguments = args;

            ReturnStatus = Device.Network.Fetcher.Fetch( uri, args.Headers, args.TimeoutMilliseconds);
            Expiration = ReturnStatus.Expiration;
            AttemptToRefresh = ReturnStatus.AttemptToRefresh;
            ResponseString = ReturnStatus.ResponseString;
            ResponseBytes = ReturnStatus.ResponseBytes;
        }

        /// <summary>
        /// returns Uri for response object. Direct stream doesn't have a natural file name.
        /// </summary>
        /// <returns></returns>
        public override string GetResponseFileName()
        {
            // To-Do: really ought to throw not supported exception here.
            return Uri;
        }

        //public override Stream GetResponseStream()
        //{
        //    byte[] bytes = Encoding.UTF8.GetBytes( ResponseString );
        //    return new MemoryStream( bytes );
        //}

        /// <summary>
        /// Gets the response string.
        /// </summary>
        /// <returns></returns>
        public override string GetResponseString()
        {
            return ResponseString;
        }

        /// <summary>
        /// Gets the response byte array.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetResponseBytes()
        {
            return ResponseBytes;
        }
    }
}

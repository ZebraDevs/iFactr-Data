using iFactr.Data.Utilities.NetworkResource.ResourceStrategy;
using System;

namespace iFactr.Data.Utilities.NetworkResource
{
    /// <summary>
    /// Internal constructor, create from NetworkResourceLibrary
    /// </summary>
    public class ResourceRequest
    {
        /// <summary>
        /// Full Uri used for obtaining request results.
        /// </summary>
        private string _uri = null;
        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>The URI.</value>
        public string Uri
        {
            get { return _uri; }
            internal set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value");

                //if ( !System.Uri.IsWellFormedUriString( value, UriKind.Absolute ) )
                //    throw new ArgumentException( "Value is not a well-formed Absolute Uri" );

                _uri = value;
            }
        }
        /// <summary>
        /// Gets or sets the network resource arguments.
        /// </summary>
        /// <value>The network resource arguments.</value>
        public NetworkResourceArguments NetworkResourceArguments
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the resource strategy.
        /// </summary>
        /// <value>The type of the resource strategy.</value>
        public ResourceStrategyType ResourceStrategyType { get; protected set; }

        internal ResourceRequest( string uri, ResourceStrategyType resourceStrategyType, NetworkResourceArguments args )
        {
            Uri = uri;
            ResourceStrategyType = resourceStrategyType;
            NetworkResourceArguments = args;
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <param name="timeoutMilliseconds">The timeout milliseconds.</param>
        /// <returns></returns>
        public ResourceResponse GetResponse(int timeoutMilliseconds)
        {
            NetworkResourceArguments.TimeoutMilliseconds = timeoutMilliseconds;

            return GetResponse();
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public ResourceResponse GetResponse()
        {
            ResourceStrategyController controller = new ResourceStrategyController(ResourceStrategyType);
            ResourceResponse response = controller.GetResponse(Uri, NetworkResourceArguments);

            return response;
        }
    }
}

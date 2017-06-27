using System;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    /// <summary>
    /// Represents a base resource strategy.
    /// </summary>
    public abstract class BaseResourceStrategy : IResourceStrategy
    {
        #region IResourceStrategy Members

        /// <summary>
        /// Gets the resource strategy type.
        /// </summary>
        /// <value>The resource strategy type.</value>
        public abstract ResourceStrategyType Type { get; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public virtual void Initialize()
        {
            return;
        }

        /// <summary>
        /// Gets the resource response.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <returns></returns>
        public virtual ResourceResponse GetResponse(string uri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the resource response.
        /// </summary>
        /// <param name="uri">The URI of the request.</param>
        /// <param name="args">The arguments to be used on the request.</param>
        /// <returns></returns>
        public virtual ResourceResponse GetResponse( string uri, NetworkResourceArguments args )
        {
            throw new NotImplementedException();
        }
        #endregion

        // To-Do: move to Uri Extensions or other helper class.
        /// <summary>
        /// Gets the base URI.
        /// </summary>
        /// <param name="uriString">The URI string.</param>
        /// <returns></returns>
        public virtual string GetBaseUri(string uriString )
        {
            //if (!System.Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            //    throw new ArgumentException("uriString is not a well-formed Absolute Uri");

            System.Uri uri = new Uri(uriString);
            return uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.IndexOf(uri.AbsolutePath));
        }
        
        // To-Do: move to Uri Extensions or other helper class.
        /// <summary>
        /// Gets the relative URI.
        /// </summary>
        /// <param name="uriString">The URI string.</param>
        /// <returns></returns>
        public virtual string GetRelativeUri(string uriString)
        {
            System.Uri uri = new Uri(uriString);
            return uri.AbsolutePath;
        }
    
    }
}

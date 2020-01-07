using MonoCross.Utilities;
using System;
using System.Collections.Generic;

namespace iFactr.Data
{
    //To-Do: Determine approach and logic for handling conflicts from HTTP header cache and managed-code specified cache period
    /// <summary>
    /// Represents a RESTful cache of items of a specific object type.
    /// </summary>
    /// <typeparam name="T">The object type of the items in the cache.</typeparam>
    public class RestfulCache<T> : Dictionary<string, RestfulObject<T>>
    {
        /// <summary>
        /// Gets or sets the base URI.
        /// </summary>
        /// <value>The base URI.</value>
        public string BaseUri { get; set; }
        /// <summary>
        /// Gets or sets the relative URI.
        /// </summary>
        /// <value>The relative URI.</value>
        public string RelativeUri { get; set; }
        /// <summary>
        /// Gets the absolute URI.
        /// </summary>
        /// <value>The absolute URI.</value>
        public string AbsoluteUri
        {
            get
            {
                //TODO: Look at appending the Port number here
                // No sure if this fixed the Post Number, find out where AbsolutUri is use
                // Also UPDATE RestfulQueue class also.
                // Something along the lines of:
                // return (BaseUri + ":8080").AppendPath(RelativeUri);

                return BaseUri.AppendPath(RelativeUri);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulCache&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI of the cache.</param>
        /// <param name="relativeUri">The relative URI of the cache.</param>
        public RestfulCache(string baseUri, string relativeUri)
        {
            if (baseUri == null || relativeUri == null)
            {
                throw new ArgumentNullException("Arguments cannot be null");
            }
            BaseUri = baseUri;
            RelativeUri = relativeUri;
        }
    }
}

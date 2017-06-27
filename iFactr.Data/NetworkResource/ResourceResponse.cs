using MonoCross;
using System;
using System.Collections.Generic;

namespace iFactr.Data.Utilities.NetworkResource
{
    /// <summary>
    /// Abstract Network Resource Library Response class.
    /// </summary>
    public abstract class ResourceResponse
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<string, string> Data{ get; protected set; }

        /// <summary>
        /// Gets or sets the response bytes.
        /// </summary>
        /// <value>The response bytes.</value>
        protected byte[] ResponseBytes { get; set; }

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
                    throw new ArgumentNullException("value", "value cannot be null or empty.");

                //if ( !System.Uri.IsWellFormedUriString( value, UriKind.Absolute ) )
                //    throw new ArgumentException( "Value is not a well-formed Absolute Uri" );

                _uri = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected DateTime _expiration = DateTime.MinValue.ToUniversalTime();
        /// <summary>
        /// Gets or sets the expiration.
        /// </summary>
        /// <value>The expiration.</value>
        public virtual DateTime Expiration
        {
            get
            {
                return _expiration;
            }
            protected set
            {
                _expiration = value.ToUniversalTime();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected DateTime _attemptToRefresh = DateTime.MinValue.ToUniversalTime();
        /// <summary>
        /// Gets or sets the attempt to refresh.
        /// </summary>
        /// <value>The attempt to refresh.</value>
        public virtual DateTime AttemptToRefresh
        {
            get
            {
                return _attemptToRefresh;
            }
            protected set
            {
                _attemptToRefresh = value.ToUniversalTime();
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
        /// Initializes a new instance of the <see cref="ResourceResponse"/> class.
        /// </summary>
        public ResourceResponse()
        {
            Data = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the return status.
        /// </summary>
        /// <value>The return status.</value>
        public virtual NetworkResponse ReturnStatus
        {
            get;
            internal set;
        }
        /// <summary>
        /// Gets the name of the response file.
        /// </summary>
        /// <returns></returns>
        public abstract string GetResponseFileName();
        //public abstract Stream GetResponseStream();
        /// <summary>
        /// Gets the response string.
        /// </summary>
        /// <returns></returns>
        public abstract string GetResponseString();
        /// <summary>
        /// Gets the response byte array.
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetResponseBytes();
    }
}

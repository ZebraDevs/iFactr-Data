using System;
using System.Collections.Generic;

namespace iFactr.Data.Utilities.NetworkResource
{
    /// <summary>
    /// Class containing arguments/parameters for NetworkResourceRequests.
    /// </summary>
    public class NetworkResourceArguments
    {
        /// <summary>
        /// Header values to inject into web request calls
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get;
            set;
        }

        /// <summary>
        /// Method to attempt to refresh stale data (used in NRL Cache)
        /// </summary>
        public CacheStaleMethod CacheStaleMethod
        {
            get;
            set;
        }

		int _timeoutMilliseconds = 60000;
        /// <summary>
        /// Gets or sets the timeout milliseconds.
        /// </summary>
        /// <value>The timeout milliseconds; default is 60,000 milliseconds</value>
        public int TimeoutMilliseconds
		{
			get { return _timeoutMilliseconds; }
			set { _timeoutMilliseconds = value; }
		}

        /// <summary>
        /// Default Expiration of the resource being retreived
        /// </summary>
        public TimeSpan Expiration
        {
            get { return _expiration; }
            set { _expiration = value; }
        }
        TimeSpan _expiration = new TimeSpan(0);
    }
}

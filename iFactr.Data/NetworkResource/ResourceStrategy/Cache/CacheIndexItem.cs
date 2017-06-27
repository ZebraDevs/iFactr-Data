using MonoCross.Utilities;
using System;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Represents a cache index item.
    /// </summary>
#if (DROID)
    [Android.Runtime.Preserve( AllMembers = true )]
#elif (TOUCH)
    [MonoTouch.Foundation.Preserve (AllMembers = true)]
#endif
    public class CacheIndexItem
    {
        #region Public Properties

        /// <summary>
        /// A unique <see cref="string"/> identifier that determines the path for this instance's location on disk.
        /// </summary>
        public string ID { get; set; }

        DateTime _downloaded;
        /// <summary>
        /// Downloaded date in universal time
        /// </summary>
        public DateTime Downloaded
        {
            get
            {
                return _downloaded;
            }
            set
            {
                //if ( CachePeriod.Ticks > 0 && value > DateTime.MinValue.ToUniversalTime() )
                //{
                //    Expiration = value.Add( CachePeriod );
                //}
                _downloaded = value.ToUniversalTime();
            }
        }

        //TimeSpan _cachePeriod = new TimeSpan(0);
        //public TimeSpan CachePeriod
        //{
        //    get
        //    {
        //        return _cachePeriod;
        //    }
        //    set
        //    {
        //        if ( value.Ticks > 0 && Downloaded > DateTime.MinValue.ToUniversalTime() )
        //        {
        //            Expiration = Downloaded.Add( value );
        //        }
        //        _cachePeriod = value;
        //    }
        //}

        DateTime _attemptToRefresh;
        /// <summary>
        /// Gets or sets the attempt to refresh date.
        /// </summary>
        /// <value>The attempt to refresh date.</value>
        public DateTime AttemptToRefresh
        {
            get
            {
                return _attemptToRefresh;
            }
            set
            {
#if NETCF
                _attemptToRefresh = value.ToUniversalTime();
#else
                if (value.Kind == DateTimeKind.Utc)
                {
                    _attemptToRefresh = value;
                    return;
                }

                // The UTC time is equal to the local time minus the UTC offset.
                long tickCount = value.Ticks - TimeZoneInfo.Local.GetUtcOffset(value).Ticks;

                if (tickCount > DateTime.MaxValue.Ticks)
                {
                    // value is too large to fit in DateTime
                    tickCount = DateTime.MaxValue.Ticks;
                }

                if (tickCount < DateTime.MinValue.Ticks)
                {
                    // value is too small to fit in DateTime
                    tickCount = DateTime.MinValue.Ticks;
                }

                _attemptToRefresh = new DateTime(tickCount, DateTimeKind.Utc);
#endif
            }
        }

        DateTime _expiration;

        /// <summary>
        /// cache expiration date in universal time
        /// </summary>
        public DateTime Expiration
        {
            get
            {
                return _expiration;
            }
            set
            {
#if NETCF
                _expiration = value.ToUniversalTime();
#else
                //value = value.ToUniversalTime();
                //if ( CachePeriod.Ticks > 0 && Downloaded > DateTime.MinValue.ToUniversalTime() )
                //{
                //    _expiration = Downloaded.Add( CachePeriod );
                //}
                //else
                //{
                //    _expiration = value;
                //}

                if (value.Kind == DateTimeKind.Utc)
                {
                    _expiration = value;
                    return;
                }

                // The UTC time is equal to the local time minus the UTC offset.
                long tickCount = value.Ticks - TimeZoneInfo.Local.GetUtcOffset(value).Ticks;

                if (tickCount > DateTime.MaxValue.Ticks)
                {
                    // value is too large to fit in DateTime
                    tickCount = DateTime.MaxValue.Ticks;
                }

                if (tickCount < DateTime.MinValue.Ticks)
                {
                    // value is too small to fit in DateTime
                    tickCount = DateTime.MinValue.Ticks;
                }

                _expiration = new DateTime(tickCount, DateTimeKind.Utc);
#endif
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is prefectched.
        /// </summary>
        /// <value><c>true</c> if the item is prefectched; otherwise, <c>false</c>.</value>
        public bool PreFetch
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the E tag.
        /// </summary>
        /// <value>The E tag.</value>
        public string ETag
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the usage count.
        /// </summary>
        /// <value>The usage count.</value>
        public int UsageCount
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        public string ContentType
        {
            get;
            set;
        }

        private string _relativeUri;
        /// <summary>
        /// Gets or sets the relative URI.
        /// </summary>
        /// <value>The relative URI.</value>
        public string RelativeUri
        {
            get
            {
                return _relativeUri;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value");

                string uri = System.Text.RegularExpressions.Regex.Replace(value, "[: ]", "-");
                if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
                    throw new ArgumentException("Value is not a well-formed Relative Uri " + value);

                _relativeUri = uri.RemoveLeadingSlash();
            }
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
                if (Expiration > DateTime.MinValue.ToUniversalTime() && DateTime.UtcNow >= Expiration)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is stale.
        /// </summary>
        /// <value><c>true</c> if this instance is stale; otherwise, <c>false</c>.</value>
        public bool IsStale
        {
            get
            {
                // treat DateTime.MinValue as "never stales"
                if (IsExpired || (AttemptToRefresh > DateTime.MinValue.ToUniversalTime() && DateTime.UtcNow >= AttemptToRefresh))
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is downloaded.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is downloaded; otherwise, <c>false</c>.
        /// </value>
        public bool IsDownloaded
        {
            get
            {
                // treat DateTime.MinValue as "not downloaded"
                return (Downloaded > DateTime.MinValue.ToUniversalTime());
            }
        }

        #endregion

        /// <summary>
        /// Expires the metadata for the given item but doesn't remove cached file.
        /// </summary>
        public void Expire()
        {
            Downloaded = DateTime.MinValue.ToUniversalTime();
            Expiration = DateTime.UtcNow;
            AttemptToRefresh = DateTime.UtcNow;
        }
        /// <summary>
        /// Marks the metadata as Stale for the given item but doesn't remove cached file.
        /// </summary>
        public void Stale()
        {
            AttemptToRefresh = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheIndexItem"/> class.
        /// </summary>
        public CacheIndexItem() { ID = Guid.NewGuid().ToString(); }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("CacheIndexItem: {0}, Downloaded: {1}, ID: {2}", _relativeUri, Downloaded, ID);
        }
    }
}

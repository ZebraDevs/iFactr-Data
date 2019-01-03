using MonoCross.Navigation;
using System;
using System.Runtime.Serialization;

namespace iFactr.Data
{
    // To-Do: move HttpVerb class to MonoCross.Utilities.Networking, so it can be used there for simple network interface.
    /// <summary>
    /// This static class contains HTTP verb values.
    /// </summary>
    public static class HttpVerb
    {
        /// <summary>
        /// No HTTP Verb.
        /// </summary>
        public const string None = "";

        /// <summary>
        /// The GET Verb.
        /// </summary>
        public const string Get = "GET";

        /// <summary>
        /// The POST Verb.
        /// </summary>
        public const string Post = "POST";

        /// <summary>
        /// The PUT Verb.
        /// </summary>
        public const string Put = "PUT";

        /// <summary>
        /// The DELETE Verb.
        /// </summary>
        public const string Delete = "DELETE";
    }

    /// <summary>
    /// Represents a RESTful item of the type specified.
    /// </summary>
    /// <typeparam name="T">The item object type for the instance.</typeparam>
#if (DROID)
    [Android.Runtime.Preserve(AllMembers = true)]
#elif (TOUCH)
    [MonoTouch.Foundation.Preserve (AllMembers = true)]
#endif
    [DataContract]
    public class RestfulObject<T>
    {
        /// <summary>
        /// Gets or sets the expiration date of the RESTful object.
        /// </summary>
        /// <value>The expiration date.</value>
        [DataMember(Order = 1)]
        public DateTime ExpirationDate
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the attempt refresh date of the RESTful object.
        /// </summary>
        /// <value>The attempt refresh date.</value>
        [DataMember(Order = 2)]
        public DateTime AttemptRefreshDate
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the URI endpoint of the RESTful object.
        /// </summary>
        /// <value>The URI endpoint.</value>
        [DataMember(Order = 3)]
        public virtual string UriEndpoint
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the POST URI endpoint of the RESTful object.
        /// </summary>
        /// <value>The POST transaction URI endpoint.</value>
        [DataMember(Order = 8)]
        public virtual string TransactionEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(_postEndpoint) && Verb == HttpVerb.Post)
                    return _postEndpoint;
                else
                    return UriEndpoint;
            }
            set { _postEndpoint = value; }
        }
        string _postEndpoint = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether the RESTful object is lazy loaded.
        /// </summary>
        /// <value><c>true</c> if lazy loaded; otherwise, <c>false</c>.</value>
        [DataMember(Order = 4)]
        public bool LazyLoaded
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the HTTP verb of the RESTful object.
        /// </summary>
        /// <value>The HTTP verb.</value>
        [DataMember(Order = 5)]
        public string Verb
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the item instance of the RESTful object.
        /// </summary>
        /// <value>The item instance.</value>
        [DataMember(Order = 6)]
        public T Object
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the put post delete headers of the RESTful object.
        /// </summary>
        /// <value>The put post delete headers.</value>
        [DataMember(Order = 7)]
        public SerializableDictionary<string, string> PutPostDeleteHeaders { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulObject&lt;T&gt;"/> class.
        /// </summary>
        public RestfulObject()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulObject&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="domainObject">The item instance.</param>
        /// <param name="uriEndpoint">The URI endpoint.</param>
        public RestfulObject(T domainObject, string uriEndpoint)
            : this(domainObject, uriEndpoint, CachePeriod.Default()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulObject&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="domainObject">The item instance.</param>
        /// <param name="uriEndpoint">The URI endpoint.</param>
        /// <param name="expirationDate">The expiration date.</param>
        public RestfulObject(T domainObject, string uriEndpoint, DateTime expirationDate)
            : this(domainObject, HttpVerb.None, uriEndpoint, expirationDate) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulObject&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="domainObject">The item instance.</param>
        /// <param name="httpVerb">The HTTP verb.</param>
        /// <param name="uriEndpoint">The URI endpoint.</param>
        public RestfulObject(T domainObject, string httpVerb, string uriEndpoint)
            : this(domainObject, httpVerb, uriEndpoint, CachePeriod.Default()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulObject&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="domainObject">The item instance.</param>
        /// <param name="httpVerb">The HTTP verb.</param>
        /// <param name="uriEndpoint">The URI endpoint.</param>
        /// <param name="expirationDate">The expiration date.</param>
        public RestfulObject(T domainObject, string httpVerb, string uriEndpoint, DateTime expirationDate)
        {
            if (uriEndpoint == null)
            {
                throw new ArgumentNullException("uriEndpoint cannot be null");
            }
            UriEndpoint = uriEndpoint;
            Object = domainObject;
            ExpirationDate = expirationDate;
            Verb = httpVerb;
        }

        /// <summary>
        /// Creates a copy of a RESTful object for the item instance provided.
        /// </summary>
        /// <param name="domainObject">The item instance to clone to a new RESTful object.</param>
        /// <returns></returns>
        public RestfulObject<T> Clone(T domainObject)
        {
            RestfulObject<T> restObj = new RestfulObject<T>(domainObject, this.UriEndpoint)
            {
                ExpirationDate = this.ExpirationDate,
                LazyLoaded = this.LazyLoaded,
                Verb = this.Verb,
                AttemptRefreshDate = this.AttemptRefreshDate
            };

            return restObj;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("RestfulObject verbed {2} for: '{0}' type {1}", (object)Object ?? "<null>", typeof(T).ToString(), Verb);
        }
    }
}
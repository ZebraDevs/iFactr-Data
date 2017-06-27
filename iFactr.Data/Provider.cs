using MonoCross;
using MonoCross.Navigation;
using MonoCross.Utilities;
using iFactr.Data.Utilities.NetworkResource;
using iFactr.Data.Utilities.NetworkResource.ResourceStrategy;
using iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache;
using iFactr.Data.Utilities.NetworkResource.ResourceStrategy.DirectStream;
using MonoCross.Utilities.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace iFactr.Data
{
    /// <summary>
    /// Specifies the supported operations on a data provider.
    /// </summary>
    [Flags]
    public enum ProviderMethod
    {
        /// <summary>
        /// Enables list and item retrieval; GET is always supported on data providers.
        /// </summary>
        GET = 0,  // gets are always supported in a Restful interface...
        /// <summary>
        /// Enables item creation on a data provider via the Add method.
        /// </summary>
        POST = 1,
        /// <summary>
        /// Enables item modification on a data provider via the Update method.
        /// </summary>
        PUT = 2,
        /// <summary>
        /// Enables item removal on a data provider via the Delete method.
        /// </summary>
        DELETE = 4

    };

    /// <summary>
    /// Defines the implementation contract for a data provider.
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// Gets the type of the provider.
        /// </summary>
        /// <value>The type of the provider.</value>
        Type ProviderType
        {
            get;
        }
    }
    /// <summary>
    /// Defines the implementation contract for a composite data provider.
    /// </summary>
    public interface ICompositeDataProvider : IDataProvider
    {
        /// <summary>
        /// Gets a collection of associated data providers.
        /// </summary>
        /// <value>The providers.</value>
        ProviderRegistry Providers
        {
            get;
        }
    }
    /// <summary>
    /// Defines the implementation contract for a generic data provider.
    /// </summary>
    /// <typeparam name="T">The generic object type for the provider.</typeparam>
    public interface IDataProvider<T>
    {
        /// <summary>
        /// Gets a provider managed object.
        /// </summary>
        /// <param name="parameters">The parameters to be used in the request.</param>
        /// <returns></returns>
        T Get(Dictionary<string, string> parameters);
        /// <summary>
        /// Adds the specified object using the provider.
        /// </summary>
        /// <param name="obj">The object to add.</param>
        void Add(T obj);
        /// <summary>
        /// Changes the specified object using the provider.
        /// </summary>
        /// <param name="obj">The object to change.</param>
        void Change(T obj);
        /// <summary>
        /// Deletes the specified object using the provider.
        /// </summary>
        /// <param name="obj">The object to delete.</param>
        void Delete(T obj);
    }

    /// <summary>
    /// This class represents an iFactr data provider.
    /// </summary>
    /// <typeparam name="T">The generic object type for the provider.</typeparam>
    /// <remarks>
    /// The Provider&lt;T&gt; class provides the base implementation for all data
    /// providers, and implements the base list plus transaction methods, (CRUD).
    /// </remarks>
    public abstract class Provider<T> : IDataProvider
    {
        object syncLock = new object();
        //Type[] auxTypes = null;

        /// <summary>The RestfulQueue supporting this Provider</summary>
        protected RestfulQueue<T> queue;
        RestfulCache<T> cache;
        DeltaCache<T> delta;
        SerializableDictionary<string, string> putPostDeleteHeaders;
        /// <summary>
        /// Gets the provider-specific header values to be included in all RESTful transaction requests to the server.
        /// </summary>
        /// <value>The transaction header value dictionary.</value>
        public SerializableDictionary<string, string> PutPostDeleteHeaders
        {
            get
            {
                if (putPostDeleteHeaders == null)
                    putPostDeleteHeaders = new SerializableDictionary<string, string>();
                return putPostDeleteHeaders;
            }
        }
        /// <summary>
        /// Gets the combined RequestInjectionHeaders from the iFactr Application, along with the provider-specific PutPostDeleteHeaders
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string, string> MergedHeaders
        {
            get
            {
                var mergedHeaders = Device.RequestInjectionHeaders.Concat(PutPostDeleteHeaders).ToDictionary(dic => dic.Key, dic => dic.Value);
                return mergedHeaders;
            }
        }

        /// <summary>
        /// Gets or sets whether the Provider should try to post to a list endpoint by default.
        /// </summary>
        /// <value><c>true</c> to post to a list endpoint; otherwise <c>false</c>.</value>
        /// <remarks>Will always return <c>true</c> if <see cref="Format"/> is <see cref="SerializationFormat.ODATA"/>.</remarks>
        public bool PostToListEndpoint
        {
            get { return Format == SerializationFormat.ODATA || _postToListEndpoint; }
            set { _postToListEndpoint = value; }
        }
        bool _postToListEndpoint;
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        public Provider(string baseUri, string objectUri, string listUri)
        {
            this.PopulateKeys(null, null, null);
            Initialize(baseUri, objectUri, listUri, SerializationFormat.XML, null, SerializationFormat.XML, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        public Provider(string baseUri, string objectUri, string listUri, int responseTimeout)
        {
            this.PopulateKeys(null, null, null);
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, SerializationFormat.XML, null, SerializationFormat.XML, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="keyParameter0">The key parameter0.</param>
        /// <param name="keyParameter1">The key parameter1.</param>
        /// <param name="keyParameter2">The key parameter2.</param>
        public Provider(string baseUri, string objectUri, string listUri, string keyParameter0, string keyParameter1, string keyParameter2)
        {
            this.PopulateKeys(keyParameter0, keyParameter1, keyParameter2);
            Initialize(baseUri, objectUri, listUri, SerializationFormat.XML, null, SerializationFormat.XML, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="keyParameter0">The key parameter0.</param>
        /// <param name="keyParameter1">The key parameter1.</param>
        /// <param name="keyParameter2">The key parameter2.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        public Provider(string baseUri, string objectUri, string listUri, string keyParameter0, string keyParameter1, string keyParameter2, int responseTimeout)
        {
            this.PopulateKeys(keyParameter0, keyParameter1, keyParameter2);
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, SerializationFormat.XML, null, SerializationFormat.XML, null);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="keyParameters">The key parameters.</param>
        public Provider(string baseUri, string objectUri, string listUri, params string[] keyParameters)
        {
            this.KeyParameter = keyParameters.ToList();
            Initialize(baseUri, objectUri, listUri, SerializationFormat.XML, null, SerializationFormat.XML, null);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        /// <param name="keyParameters">The key parameters.</param>
        public Provider(string baseUri, string objectUri, string listUri, int responseTimeout, params string[] keyParameters)
        {
            this.KeyParameter = keyParameters.ToList();
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, SerializationFormat.XML, null, SerializationFormat.XML, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="format">The format.</param>
        public Provider(string baseUri, string objectUri, string listUri, SerializationFormat format)
        {
            this.PopulateKeys(null, null, null);
            Initialize(baseUri, objectUri, listUri, format, null, SerializationFormat.XML, null);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="format">The format.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        public Provider(string baseUri, string objectUri, string listUri, SerializationFormat format, int responseTimeout)
        {
            this.PopulateKeys(null, null, null);
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, format, null, SerializationFormat.XML, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="queueFormat">The queue format.</param>
        /// <param name="keyParameter0">The key parameter0.</param>
        /// <param name="keyParameter1">The key parameter1.</param>
        /// <param name="keyParameter2">The key parameter2.</param>
        public Provider(string baseUri, string objectUri, string listUri, SerializationFormat queueFormat, string keyParameter0, string keyParameter1, string keyParameter2)
        {
            this.PopulateKeys(keyParameter0, keyParameter1, keyParameter2);
            Initialize(baseUri, objectUri, listUri, queueFormat, null, SerializationFormat.XML, null);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="queueFormat">The queue format.</param>
        /// <param name="keyParameter0">The key parameter0.</param>
        /// <param name="keyParameter1">The key parameter1.</param>
        /// <param name="keyParameter2">The key parameter2.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        public Provider(string baseUri, string objectUri, string listUri, SerializationFormat queueFormat, string keyParameter0, string keyParameter1, string keyParameter2, int responseTimeout)
        {
            this.PopulateKeys(keyParameter0, keyParameter1, keyParameter2);
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, queueFormat, null, SerializationFormat.XML, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="queueFormat">The queue format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        /// <param name="businessObjectFormat">The business object format.</param>
        /// <param name="customSerializationType">Type of the custom serialization.</param>
        /// <param name="keyParameter0">The key parameter0.</param>
        /// <param name="keyParameter1">The key parameter1.</param>
        /// <param name="keyParameter2">The key parameter2.</param>
        public Provider(string baseUri, string objectUri, string listUri,
            SerializationFormat queueFormat, Type customQueueSerializationType,
            SerializationFormat businessObjectFormat, Type customSerializationType,
            string keyParameter0, string keyParameter1, string keyParameter2)
        {
            this.PopulateKeys(keyParameter0, keyParameter1, keyParameter2);
            Initialize(baseUri, objectUri, listUri, queueFormat, customQueueSerializationType,
                businessObjectFormat, customSerializationType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="queueFormat">The queue format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        /// <param name="businessObjectFormat">The business object format.</param>
        /// <param name="customSerializationType">Type of the custom serialization.</param>
        /// <param name="keyParameter0">The key parameter0.</param>
        /// <param name="keyParameter1">The key parameter1.</param>
        /// <param name="keyParameter2">The key parameter2.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        public Provider(string baseUri, string objectUri, string listUri,
                        SerializationFormat queueFormat, Type customQueueSerializationType,
                        SerializationFormat businessObjectFormat, Type customSerializationType,
                        string keyParameter0, string keyParameter1, string keyParameter2, int responseTimeout)
        {
            this.PopulateKeys(keyParameter0, keyParameter1, keyParameter2);
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, queueFormat, customQueueSerializationType,
                       businessObjectFormat, customSerializationType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="format">The format.</param>
        /// <param name="keyParameters">The key parameters.</param>
        public Provider(string baseUri, string objectUri, string listUri, SerializationFormat format, params string[] keyParameters)
        {
            this.KeyParameter = keyParameters.ToList();
            Initialize(baseUri, objectUri, listUri, SerializationFormat.XML, null, format, null);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="format">The format.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        /// <param name="keyParameters">The key parameters.</param>
        public Provider(string baseUri, string objectUri, string listUri, SerializationFormat format, int responseTimeout, params string[] keyParameters)
        {
            this.KeyParameter = keyParameters.ToList();
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, format, null, SerializationFormat.XML, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="queueFormat">The queue format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        /// <param name="businessObjectFormat">The business object format.</param>
        /// <param name="customSerializationType">Type of the custom serialization.</param>
        /// <param name="keyParameters">The key parameters.</param>
        public Provider(string baseUri, string objectUri, string listUri,
            SerializationFormat queueFormat, Type customQueueSerializationType,
            SerializationFormat businessObjectFormat, Type customSerializationType,
            params string[] keyParameters)
        {
            this.KeyParameter = keyParameters.ToList();
            Initialize(baseUri, objectUri, listUri, queueFormat, customQueueSerializationType, businessObjectFormat, customSerializationType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Provider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="queueFormat">The queue format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        /// <param name="businessObjectFormat">The business object format.</param>
        /// <param name="customSerializationType">Type of the custom serialization.</param>
        /// <param name="responseTimeout">The response timeout (in milliseconds).</param>
        /// <param name="keyParameters">The key parameters.</param>
        public Provider(string baseUri, string objectUri, string listUri,
                        SerializationFormat queueFormat, Type customQueueSerializationType,
                        SerializationFormat businessObjectFormat, Type customSerializationType,
                        int responseTimeout, params string[] keyParameters)
        {
            this.KeyParameter = keyParameters.ToList();
            ResponseTimeout = responseTimeout;
            Initialize(baseUri, objectUri, listUri, queueFormat, customQueueSerializationType, businessObjectFormat, customSerializationType);
        }

        /// <summary>
        /// Populates the keys.
        /// </summary>
        /// <param name="keyParameter0">The key parameter0.</param>
        /// <param name="keyParameter1">The key parameter1.</param>
        /// <param name="keyParameter2">The key parameter2.</param>
        public void PopulateKeys(string keyParameter0, string keyParameter1, string keyParameter2)
        {
            this.KeyParameter = new List<string>();
            if (null != keyParameter0)
                this.KeyParameter.Add(keyParameter0);
            if (null != keyParameter1)
                this.KeyParameter.Add(keyParameter1);
            if (null != keyParameter2)
                this.KeyParameter.Add(keyParameter2);
        }

        private void Initialize(string baseUri, string objectUri, string listUri, SerializationFormat queueSerializationFormat,
            Type customQueueSerializationType,
            SerializationFormat serializationFormat, Type customSerializationType)
        {
            DefaultExpiration = TimeSpan.Zero;

            if (!String.IsNullOrEmpty(baseUri))
                CacheIndexMap.Add(baseUri);

            this.ListRelativeUri = listUri;
            cache = new RestfulCache<T>(baseUri, objectUri);
            queue = new RestfulQueue<T>(baseUri, objectUri, queueSerializationFormat, customQueueSerializationType,
                serializationFormat, customSerializationType, _responseTimeout);
            queue.DeserializeQueue();
            queue.RequestReturnsObject = true;
            DeltaCache = new DeltaCache<T>();

            if (ResourceStrategy == ResourceStrategyType.Cache && CacheMethod != CacheMethodType.PersistOnly)
            {
                DeltaCache.Deserialize();    // load up changed values from the cached files

                //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
                //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( QueueSerializationFormat, AuxilliaryTypes );

                var items = DeltaCache.Where(item => item.Verb == HttpVerb.Post || item.Verb == HttpVerb.Put);

                foreach (DeltaCacheItem dci in items)
                    this.RetrieveFromNrlCache(dci.Uri /*, Serializer */ );
            }

            this.CacheStaleMethod = iFactr.Data.Utilities.NetworkResource.CacheStaleMethod.Deferred;

            queue.OnRequestComplete += new RestfulQueue<T>.RequestComplete(queue_OnRequestComplete);
            queue.OnRequestError += new RestfulQueue<T>.RequestError(queue_OnRequestError);
            queue.OnRequestFailed += new RestfulQueue<T>.RequestFailed(queue_OnRequestFailed);
        }

        #endregion

        /// <summary>
        /// Called when a RESTful transaction is successfully completed.
        /// </summary>
        /// <param name="item">The transaction item.</param>
        /// <param name="verb">The HTTP verb of the transaction.</param>
        protected virtual void OnTransactionComplete(RestfulObject<T> item, string verb) { }
        /// <summary>
        /// Called when a RESTful transaction fails due to an unhandled exception.
        /// </summary>
        /// <param name="item">The transacion item.</param>
        /// <param name="verb">The HTTP verb of the transaction.</param>
        /// <param name="ex">The the unhandled exception causing the failure.</param>
        protected virtual void OnTransactionFailed(RestfulObject<T> item, string verb, Exception ex) { }
        /// <summary>
        /// Called when a RESTful transaction fails due to an HTTP error status code.
        /// </summary>
        /// <param name="item">The transaction item.</param>
        /// <param name="verb">The HTTP verb of the transaction.</param>
        /// <param name="error">The HTTP error returned by the server.</param>
        protected virtual void OnTransactionError(RestfulObject<T> item, string verb, HttpStatusCode error) { }

        void queue_OnRequestError(RestfulObject<T> item, string verb, HttpStatusCode error)
        {
            OnTransactionError(item, verb, error);
        }
        void queue_OnRequestFailed(RestfulObject<T> item, string verb, Exception ex)
        {
            OnTransactionFailed(item, verb, ex);
        }
        void queue_OnRequestComplete(RestfulObject<T> item, string verb)
        {

            if (item.Object != null)
            {
                if (ResourceStrategy == ResourceStrategyType.Cache)
                {
                    if (verb != HttpVerb.Delete)
                    {
                        //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
                        //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( QueueSerializationFormat, AuxilliaryTypes );

                        StoreInNrlCache(item.Object, /* Serializer, */ item.UriEndpoint, item.ExpirationDate, item.AttemptRefreshDate);
                    }

                    // add returned item to delta cache.
                DeltaCache.Add(new DeltaCacheItem
                {
                        Uri = item.UriEndpoint,
                        Verb = verb,
                        PostDate = DateTime.UtcNow
                    });
                }

                if (verb != HttpVerb.Delete)
                {
                    item.LazyLoaded = true;
                    if (CacheMethod != CacheMethodType.PersistOnly)
                        cache[item.UriEndpoint] = item;
                }
            }
            OnTransactionComplete(item, verb);
        }
        #region URI Properties

        private string _listRelativeUri;
        /// <summary>
        /// Gets or sets the list relative URI.
        /// </summary>
        /// <value>The list relative URI.</value>
        protected string ListRelativeUri
        {
            get
            {
                return _listRelativeUri;
            }
            private set
            {
                // To-Do: determine means to validate relative URI that contains curly brackets.  as in "Accounts/{UserId}/{Account}.xml"
                //if ( !Uri.IsWellFormedUriString( value, UriKind.Relative ) )
                //    throw new ArgumentException( "List URI is not a valid Relative URI. " + value );
                _listRelativeUri = value;
            }
        }

        /// <summary>
        /// Gets the list absolute URI.
        /// </summary>
        /// <value>The list absolute URI.</value>
        protected string ListAbsoluteUri
        {
            get
            {
                return cache.BaseUri.AppendPath(ListRelativeUri);
            }
        }

        /// <summary>
        /// Gets the object absolute URI.
        /// </summary>
        /// <value>The object absolute URI.</value>
        protected string ObjectAbsoluteUri
        {
            get
            {
                return cache.AbsoluteUri;
            }
        }

        /// <summary>
        /// Gets the base URI.
        /// </summary>
        /// <value>The base URI.</value>
        protected string BaseUri
        {
            get
            {
                return cache.BaseUri;
            }
        }

        /// <summary>
        /// Gets the object relative URI.
        /// </summary>
        /// <value>The object relative URI.</value>
        protected string ObjectRelativeUri
        {
            get
            {
                return cache.RelativeUri;
            }
        }

        private int _responseTimeout = 60000;  // Default 60 seconds
        /// <summary>
        /// Gets or sets the response timeout.
        /// </summary>
        /// <value>The response timeout (in milliseconds).</value>
        public int ResponseTimeout
        {
            get
            {
                return _responseTimeout;
            }
            set
            {
                _responseTimeout = value;
                if (queue != null) { queue.ResponseTimeout = _responseTimeout; }
            }
        }

        #endregion

        /// <summary>
        /// Gets the type of the provider.
        /// </summary>
        /// <value>The type of the provider.</value>
        public Type ProviderType
        {
            get
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// Gets or sets the auxilliary serialization types for the provider.
        /// </summary>
        /// <value>The auxilliary types to be used for object serialization.</value>
        public Type[] AuxilliaryTypes
        {
            get { return queue.AuxilliaryTypes; }
            set { queue.AuxilliaryTypes = value; }
        }

        List<string> KeyParameter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether queue processing is enabled.
        /// </summary>
        /// <value><c>true</c> if the queue is actively processing transactions; otherwise, <c>false</c>.  
        /// Setting this value to <c>true</c> will re-enable the queue when it is inactive, 
        /// and begin processin any pending transactions on the provider.</value>
        public bool QueueEnabled
        {
            get
            {
                return queue.Enabled;
            }

            set
            {
                queue.Enabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the queue returns an updated copy of the object.
        /// </summary>
        /// <value>
        ///     <c>true</c> if a transaction returns the updated object; otherwise, <c>false</c>.
        /// </value>
        protected bool QueueRequestReturnsObject
        {
            get
            {
                return queue.RequestReturnsObject;
            }

            set
            {
                queue.RequestReturnsObject = value;
            }
        }

        /// <summary>
        /// Discards the queue and clears out all items
        /// </summary>
        public void DiscardQueue()
        {
            queue.DiscardQueue();
        }

        internal DeltaCache<T> DeltaCache
        {
            get
            {
                return delta;
            }
            set
            {
                delta = value;
            }
        }

        /// <summary>
        /// Gets a list of cached objects combined with any queued objects with transactions pending.
        /// </summary>
        /// <value>
        /// The cache list of the Provider&lt;T&gt; as a <see cref="List&lt;T&gt;"/> instance.
        /// </value>
        public virtual List<T> CacheList
        {
            get
            {
                List<T> retval = new List<T>();
                //List<RestfulObject<T>> cacheitems = cache.Values.ToList();
                List<string> cacheKeys = cache.Keys.ToList();
                Dictionary<string, RestfulObject<T>> queueitems = new Dictionary<string, RestfulObject<T>>();
                lock (syncLock)
                {
                    foreach (RestfulObject<T> item in queue)
                    {
                        var itemUri = GetUri(item.Object);
                        queueitems[itemUri] = item;
                    }

                    foreach (string key in cacheKeys)
                    {
                        if (key != null)
                        {
                            if (!queueitems.ContainsKey(key))
                            {
                                var restfulObject = cache.GetValueOrDefault(key);
                                if (restfulObject != null && restfulObject.Object != null)
                                    retval.Add(restfulObject.Object);
                            }
                            else
                            {
                                var queueItem = queueitems[key];
                                if (queueItem != null && queueItem.Verb != HttpVerb.Delete)
                                    retval.Add(queueItem.Object);
                            }
                        }
                    }
                    // now add all the items from the queue that aren't in the cache to the result set.
                    foreach (string key in queueitems.Keys.Except(cacheKeys))
                    {
                        if (key != null && queueitems[key].Verb != HttpVerb.Delete)
                            retval.Add(queueitems[key].Object);
                    }
                }
                return retval;
            }
        }

        /// <summary>
        /// Gets a list of objects currently in the transaction queue.
        /// </summary>
        /// <value>
        /// The queue list of the Provider&lt;T&gt; as a <see cref="List&lt;T&gt;"/> instance.
        /// </value>
        public virtual List<T> QueueList
        {
            get
            {
                // To-Do: implement for performance ... return List<T> retval = new List<T>().Union(queue);
                List<T> retval = new List<T>();
                lock (syncLock)
                {
                    foreach (RestfulObject<T> item in queue)
                    {
                        retval.Add(item.Object);
                    }
                }
                return retval;
            }
        }

        #region ProviderResponse<List<T>> GetList() Overloads

        /// <summary>
        /// Gets a list of objects using the default cache method, and the parameters provided.
        /// </summary>
        /// <param name="parameters">parameter collection needed to populate object uri</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(Dictionary<string, string> parameters)
        {
            return GetList(CacheMethod, parameters);
        }
        /// <summary>
        /// Gets list of objects using the cache method specified, and the parameters provided.
        /// </summary>
        /// <param name="cacheMethod">Cache method to use for request</param>
        /// <param name="parameters">parameter collection needed to populate object uri</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(CacheMethodType cacheMethod, Dictionary<string, string> parameters)
        {
            DateTime dtMetric = DateTime.UtcNow;
            DateTime dtGetUri = DateTime.UtcNow;

            string listUri = GetUri(ListAbsoluteUri, parameters);

            Device.Log.Metric(string.Format("Get.GetURI: Time: {0:F0} milliseconds", DateTime.UtcNow.Subtract(dtGetUri).TotalMilliseconds));

            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = this.CacheStaleMethod,
                TimeoutMilliseconds = _responseTimeout,
                Expiration = this.DefaultExpiration,
            };            

            ResourceRequest request = NetworkResourceLibrary.Instance.GetResourceRequest(listUri, ResourceStrategy, args);
            ResourceResponse response = (ResourceResponse)request.GetResponse();

            ProviderResponse<List<T>> providerResponse = ProviderResponse<List<T>>.Create(response.ReturnStatus);


            //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
            //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( QueueSerializationFormat, AuxilliaryTypes );
#if DEBUG
            var responsestring = response.GetResponseString();
#endif

            try
            {
                providerResponse.Object = Serializer.DeserializeList(response.GetResponseBytes(), MonoCross.Utilities.EncryptionMode.NoEncryption);
            }
            catch (Exception e) { providerResponse.Exception = e; }
            
            Device.Log.Metric(string.Format("GetList: Uri: {0}  Time: {1:F0} milliseconds", listUri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

            // clear deltas older than downloaded date of list.
            DeltaCache.Clear(providerResponse.Downloaded);

            this.ExplodeListCache(cacheMethod, providerResponse.Object, response.Expiration, response.AttemptToRefresh);

            return providerResponse;
        }

        /// <summary>
        /// Gets a list of objects using the default cache method.
        /// </summary>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList()
        {
            return GetList(CacheMethod);
        }
        /// <summary>
        /// Gets a list of objects using the cache method provided.
        /// </summary>
        /// <param name="cacheMethod">Cache method to use for request.</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(CacheMethodType cacheMethod)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            return GetList(cacheMethod, parameters);
        }

        /// <summary>
        /// Gets a list of objects using the default cache method, and the parameter provided.
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(string parm0)
        {
            return GetList(CacheMethod, parm0);
        }
        /// <summary>
        /// Gets an instance of the list from the List Uri.
        /// </summary>
        /// <param name="cacheMethod">Cache method to use for request</param>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(CacheMethodType cacheMethod, string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            return GetList(cacheMethod, parameters);
        }

        /// <summary>
        /// Gets an instance of the list from the List Uri; using provider default cache method
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <param name="parm1">Second parameter to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(string parm0, string parm1)
        {
            //            if( KeyParameter.Count() != 2 ) throw new NotSupportedException();

            return GetList(CacheMethod, parm0, parm1);
        }
        /// <summary>
        /// Gets an instance of the list from the List Uri
        /// </summary>
        /// <param name="cacheMethod">Cache method to use for request</param>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <param name="parm1">Second parameter to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(CacheMethodType cacheMethod, string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            return GetList(cacheMethod, parameters);
        }

        /// <summary>
        /// Gets an instance of the list from the List Uri; using provider default cache method
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <param name="parm1">Second parameter to use in obtaining object</param>
        /// <param name="parm2">Third parameter to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(string parm0, string parm1, string parm2)
        {
            return GetList(CacheMethod, parm0, parm1, parm2);
        }
        /// <summary>
        /// Gets an instance of the list from the List Uri
        /// </summary>
        /// <param name="cacheMethod">Cache method to use for request</param>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <param name="parm1">Second parameter to use in obtaining object</param>
        /// <param name="parm2">Third parameter to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(CacheMethodType cacheMethod, string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            return GetList(cacheMethod, parameters);
        }

        /// <summary>
        /// Gets an instance of the list from the List Uri; using provider default cache method
        /// </summary>
        /// <param name="parms">string array of parameter values to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(params string[] parms)
        {
            return GetList(CacheMethod, parms);
        }
        /// <summary>
        /// Gets an instance of the list from the List Uri
        /// </summary>
        /// <param name="cacheMethod">Cache method to use for request</param>
        /// <param name="parms">string array of parameter values to use in obtaining object</param>
        /// <returns>ProviderResponse List of type T</returns>
        public virtual ProviderResponse<List<T>> GetList(CacheMethodType cacheMethod, params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            return GetList(cacheMethod, parameters);
        }

        #endregion

        #region ProviderResponse<List<T>> GetListbyUri() Overloads

        /// <summary>
        /// Obtains a list of items from given URI, defaults cache method to none, and uses provider default format.
        /// </summary>
        /// <param name="listRelativeUri">Relative Uri for list</param>
        /// <returns>List of items obtained at URI</returns>
        public virtual ProviderResponse<List<T>> GetListbyUri(string listRelativeUri)
        {
            return GetListbyUri(CacheMethodType.None, /* QueueSerializationFormat,*/ listRelativeUri);
        }

        ///// <summary>
        ///// Obtains a list of items from given URI, and uses provider default format.
        ///// </summary>
        ///// <param name="cacheMethod">Cache method to use for request</param>
        ///// <param name="listRelativeUri">Relative Uri for list</param>
        ///// <returns>List of items obtained at URI</returns>
        //public virtual ProviderResponse<List<T>> GetListbyUri( CacheMethodType cacheMethod, string listRelativeUri )
        //{
        //    return GetListbyUri( cacheMethod, /* QueueSerializationFormat, */ listRelativeUri );
        //}

        ///// <summary>
        ///// Obtains a list of items from given URI, defaults cache method to none
        ///// </summary>
        ///// <param name="serializationFormat">Serialization format for request, currently XML or JSON</param>
        ///// <param name="listRelativeUri">Relative Uri for list</param>
        ///// <returns>List of items obtained at URI</returns>
        //public virtual ProviderResponse<List<T>> GetListbyUri( /* SerializationFormat serializationFormat, */ string listRelativeUri )
        //{
        //    return GetListbyUri( CacheMethodType.None, /* serializationFormat, */ listRelativeUri );
        //}

        /// <summary>
        /// Obtains a list of items from given URI
        /// </summary>
        /// <param name="cacheMethod">Cache method to use for request</param>
        /// <param name="listRelativeUri">Relative Uri for list</param>
        /// <returns>List of items obtained at URI</returns>
        public virtual ProviderResponse<List<T>> GetListbyUri(CacheMethodType cacheMethod, /* SerializationFormat serializationFormat,*/ string listRelativeUri)
        {
            DateTime dtMetric = DateTime.UtcNow;

            NetworkResponse networkResponse;
            string uri = BaseUri.AppendPath(listRelativeUri);

            var value = GetNetworkResource(uri, out networkResponse);

            ProviderResponse<List<T>> providerResponse = ProviderResponse<List<T>>.Create(networkResponse);

            try
            {
                providerResponse.Object = Serializer.DeserializeList(value, EncryptionMode.NoEncryption);

                this.ExplodeListCache(cacheMethod, providerResponse.Object, networkResponse.Expiration, networkResponse.AttemptToRefresh);
            }
            catch (Exception e) { providerResponse.Exception = e; }

            Device.Log.Metric(string.Format("GetListByUri: Uri: {0}  Time: {1:F0} milliseconds", uri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

            return providerResponse;
        }

        #endregion

        #region ProviderResponse<T> Get() Overloads

        /// <summary>
        /// returns an object corresponding to an object of the same type. Used for converting lightly loaded objects into fully loaded.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual ProviderResponse<T> Get(T item)
        {
            return Get(MapParams(item));
        }
        /// <summary>
        /// Gets an instance of type T from Object Uri.
        /// </summary>
        /// <param name="parameters">parameter collection needed to populate object uri</param>
        /// <returns></returns>
        public virtual ProviderResponse<T> Get(Dictionary<string, string> parameters)
        {
            DateTime dtMetric = DateTime.UtcNow;

            string uri = GetUri(parameters);

            // To-Do: replace lock(this) with another lock variant...  lock(mylock)
            lock (this)
            {
                ProviderResponse<T> providerResponse = new ProviderResponse<T>()
                {
                    //                    ResponseType = ProviderResponseType.Object
                };

                // To-Do: clean up the below logic so we don't have the overhead of declaring default (T)
                RestfulObject<T> obj = null;

                // check queue for item
                if (queue.Count() > 0)
                {
                    obj = queue.Where(item => item.UriEndpoint == uri).FirstOrDefault();

                    if (obj != default(RestfulObject<T>))
                    {
                        providerResponse.ObjectSource = ObjectSource.InMemoryQueue;
                        providerResponse.StatusCode = HttpStatusCode.OK;
                        providerResponse.Object = obj.Object;
                        return providerResponse;
                    }
                }

                // check in-memory cache
                if (cache.ContainsKey(uri))
                {
                    obj = cache[uri];
                }

                // if object is null, then load from Network Resource Library which manages the cache and expiration.
                if (obj == null || obj.Object == null || obj.ExpirationDate < DateTime.UtcNow || obj.AttemptRefreshDate < DateTime.UtcNow || !obj.LazyLoaded)
                {
                    try 
                    { 
                        providerResponse = GetCacheResource(parameters); 
                    }
                    catch (Exception e) 
                    { 
                        providerResponse.Exception = e;
                        Expire(uri, ExpireMethodType.RemoveAll);
                        providerResponse.ObjectSource = ObjectSource.NotSpecified;
                        providerResponse.Object = default(T);
                    }
                }
                else
                {
                    providerResponse.ObjectSource = ObjectSource.InMemoryCache;
                    providerResponse.StatusCode = HttpStatusCode.OK;
                    providerResponse.Object = obj.Object;
                }

                Device.Log.Metric(string.Format("Get: Uri: {0}  Time: {1:F0} milliseconds", uri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

                return providerResponse;
            }
        }
        /// <summary>
        /// Gets an instance of type T from Object Uri.
        /// </summary>
        /// <returns></returns>
        public virtual ProviderResponse<T> Get()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            return Get(parameters);
        }
        /// <summary>
        /// Gets an instance of type T from Object Uri.
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <returns></returns>
        public virtual ProviderResponse<T> Get(string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            return Get(parameters);
        }
        /// <summary>
        /// Gets an instance of type T from Object Uri.
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <param name="parm1">Second parameter to use in obtaining object</param>
        /// <returns></returns>
        public virtual ProviderResponse<T> Get(string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            return Get(parameters);
        }
        /// <summary>
        /// Gets an instance of type T from Object Uri.
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining object</param>
        /// <param name="parm1">Second parameter to use in obtaining object</param>
        /// <param name="parm2">Third parameter to use in obtaining object</param>
        /// <returns></returns>
        public virtual ProviderResponse<T> Get(string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            return Get(parameters);
        }
        /// <summary>
        /// Gets an instance of type T from Object Uri.
        /// </summary>
        /// <param name="parms">string array of parameter values to use in obtaining object</param>
        /// <returns></returns>
        public virtual ProviderResponse<T> Get(params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            return Get(parameters);
        }

        #endregion

        #region ProviderResponse<T> GetByUri() overloads

        /// <summary>
        /// Request object by direct call to object's relative URI, no caching is supported and provider default serialization format is applied.
        /// </summary>
        /// <param name="objectRelativeUri">Relative URI endpoint of object being requested</param>
        /// <returns></returns>
        public ProviderResponse<T> GetByUri(string objectRelativeUri)
        {
            return GetByUri(CacheMethodType.None, /* QueueSerializationFormat, */ objectRelativeUri);
        }

        ///// <summary>
        ///// Request object by direct call to object's relative URI, no caching is supported
        ///// </summary>
        ///// <param name="serializationFormat">Serialization format (e.g. XML or JSON) to apply to request</param>
        ///// <param name="objectRelativeUri">Relative URI endpoint of object being requested</param>
        ///// <returns></returns>
        //public ProviderResponse<T> GetByUri( SerializationFormat serializationFormat, string objectRelativeUri )
        //{
        //    return GetByUri( CacheMethodType.None, serializationFormat, objectRelativeUri );
        //}

        ///// <summary>
        ///// Request object by direct call to object's relative URI, provider default serialization format is applied.
        ///// </summary>
        ///// <param name="cacheMethod">CacheMethod to apply to object returned from call</param>
        ///// <param name="objectRelativeUri">Relative URI endpoint of object being requested</param>
        ///// <returns></returns>
        //public ProviderResponse<T> GetByUri( CacheMethodType cacheMethod, string objectRelativeUri )
        //{
        //    return GetByUri( cacheMethod, /* QueueSerializationFormat, */ objectRelativeUri );
        //}

        /// <summary>
        /// Request object by direct call to object's relative URI, provider default serialization format is applied.
        /// </summary>
        /// <param name="cacheMethod">CacheMethod to apply to object returned from call</param>
        /// <param name="objectRelativeUri">Relative URI endpoint of object being requested</param>
        /// <returns></returns>
        public ProviderResponse<T> GetByUri(CacheMethodType cacheMethod, /*SerializationFormat serializationFormat,*/ string objectRelativeUri)
        {
            DateTime dtMetric = DateTime.UtcNow;

            NetworkResponse networkResponse;

            string uri = BaseUri.AppendPath(objectRelativeUri);

            var value = GetNetworkResource(uri, out networkResponse);

            ProviderResponse<T> providerResponse = ProviderResponse<T>.Create(networkResponse);

            //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
            //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( serializationFormat, AuxilliaryTypes );

            providerResponse.Object = Serializer.DeserializeObject(value, EncryptionMode.NoEncryption);

            //this.ExplodeListCache( CacheMethodType, obj );
            CacheItem(cacheMethod, /*Serializer,*/ providerResponse.Object, networkResponse.Expiration, networkResponse.AttemptToRefresh);

            Device.Log.Metric(string.Format("GetByUri: Uri: {0}  Time: {1:F0} milliseconds", uri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

            return providerResponse;
        }

        #endregion

        /// <summary>
        /// supporting method to request a URI from the NRL Direct Stream method.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="networkResponse">The network response.</param>
        /// <returns></returns>
        private byte[] GetNetworkResource(string uri, out NetworkResponse networkResponse)
        {
            DateTime dtMetric = DateTime.UtcNow;

            ResourceRequest request = NetworkResourceLibrary.Instance.GetResourceRequest(uri, ResourceStrategyType.DirectStream);
            ResourceResponseDirectStream response = (ResourceResponseDirectStream)request.GetResponse(_responseTimeout);

            Device.Log.Metric(string.Format("GetNetworkResource: Uri: {0}  Time: {0:F0} milliseconds", uri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

            networkResponse = response.ReturnStatus;
            return response.GetResponseBytes();
        }

        /// <summary>
        /// Queues a transaction for processing an Add operation on the server.  Requires ProvderMethod.POST support on the provider.
        /// </summary>
        /// <param name="obj">The Object to be added</param>
        public virtual void Add(T obj)
        {
            if ((ProviderMethods & ProviderMethod.POST) != ProviderMethod.POST)
                throw new NotSupportedException("Provider Add method is not supported for " + this.GetType().Name);

            string uri = GetUri(obj);
            if (cache.ContainsKey(uri))
                Expire(obj, ExpireMethodType.ExpireOnly);
            else
            {
                // if the object is added to the queue and then sent for posting, we need to 
                // have a reference on the cache so the newly added object will appear on relevant
                // lists so it can be selected.  otherwise the list will have to be refreshed every time
                // an add is performed.
                if (CacheMethod != CacheMethodType.PersistOnly)
                    cache.Add(uri, new RestfulObject<T>(obj, uri)
                    {
                        LazyLoaded = false,
                        ExpirationDate = CachePeriod.Expired()
                    });
            }

            //To-Do: enhance to use strategy pattern for Post, Put and Delete posts
            if (ResourceStrategy == ResourceStrategyType.LocalFile)
            {
                // instantiate RestfulObject to provide continuity with transaction event handlers
                var restObj = new RestfulObject<T>(obj, HttpVerb.Post, GetUri(ObjectAbsoluteUri, MapParams(obj)));
                try
                {
                    // attempt add transaction, and update list endpoint with new item
                    Serializer.SerializeObjectToFile(restObj.Object, restObj.UriEndpoint);
                    UpdateLocalListFile(obj, restObj.Verb);
                    OnTransactionComplete(restObj, restObj.Verb);
                }
                catch (Exception e)
                {
                    // rollback addition of new file to data store
                    if (Device.File.Exists(restObj.UriEndpoint))
                        Device.File.Delete(restObj.UriEndpoint);
                    OnTransactionFailed(restObj, restObj.Verb, new Exception("Provider Add operation failed.", e));
                }
            }
            else
            {
                switch (Device.NetworkPostMethod)
                {
                    case MonoCross.Utilities.Network.NetworkPostMethod.QueuedAsynchronous:
                        queue.Enqueue(new RestfulObject<T>(obj, HttpVerb.Post, uri) { PutPostDeleteHeaders = PutPostDeleteHeaders, TransactionEndpoint = this.PostToListEndpoint ? GetUri(ListRelativeUri, MapParams(obj)) : string.Empty });
                        queue.SerializeQueue();
                        break;
                    case MonoCross.Utilities.Network.NetworkPostMethod.ImmediateSynchronous:
                        NetworkResponse NetworkResponse = Utilities.Network.Post<T>(obj, this.PostToListEndpoint ? this.ListAbsoluteUri : GetUri(ObjectAbsoluteUri, MapParams(obj)), MergedHeaders, Format, queue.CustomSerializerType);
                        ProcessNetworkResponse(NetworkResponse);
                        break;
                }
            }
        }

        /// <summary>
        /// Queues a transaction for processing a change operation on the server.  Requires ProvderMethod.PUT support on the provider.
        /// </summary>
        /// <param name="obj">The Object to be changed.</param>
        public virtual void Change(T obj)
        {
            if ((ProviderMethods & ProviderMethod.PUT) != ProviderMethod.PUT)
                throw new NotSupportedException("Provider Change method is not supported for " + this.GetType().Name);

            Expire(obj, ExpireMethodType.ExpireOnly);

            //To-Do: enhance to use strategy pattern for Post, Put and Delete posts
            if (ResourceStrategy == ResourceStrategyType.LocalFile)
            {
                // instantiate RestfulObject to provide continuity with transaction event handlers
                var restObj = new RestfulObject<T>(obj, HttpVerb.Put, GetUri(ObjectAbsoluteUri, MapParams(obj)));
                // obtain handle to existing item for transactional integrity
                var existingObj = Serializer.DeserializeObjectFromFile(restObj.UriEndpoint);
                try
                {
                    // attempt change transaction, and update list with changed item
                    Serializer.SerializeObjectToFile(restObj.Object, restObj.UriEndpoint);
                    UpdateLocalListFile(obj, restObj.Verb);
                    OnTransactionComplete(restObj, restObj.Verb);
                }
                catch (Exception e)
                {
                    // rolback file change in data store
                    Serializer.SerializeObjectToFile(existingObj, restObj.UriEndpoint);
                    OnTransactionFailed(restObj, restObj.Verb, new Exception("Provider Change operation failed.", e));
                }
            }
            else
            {
                switch (Device.NetworkPostMethod)
                {
                    case MonoCross.Utilities.Network.NetworkPostMethod.QueuedAsynchronous:
                        queue.Enqueue(new RestfulObject<T>(obj, HttpVerb.Put, GetUri(obj)) { PutPostDeleteHeaders = PutPostDeleteHeaders });
                        queue.SerializeQueue();
                        break;
                    case MonoCross.Utilities.Network.NetworkPostMethod.ImmediateSynchronous:
                        NetworkResponse NetworkResponse = Utilities.Network.Put<T>(obj, GetUri(ObjectAbsoluteUri, MapParams(obj)), MergedHeaders, Format, queue.CustomSerializerType);
                        ProcessNetworkResponse(NetworkResponse);
                        break;
                }
            }
        }

        /// <summary>
        /// Queues a transaction for processing a delete operation on the server.  Requires ProvderMethod.DELETE support on the provider.
        /// </summary>
        /// <param name="obj">The Object to be deleted.</param>
        public virtual void Delete(T obj)
        {
            if ((ProviderMethods & ProviderMethod.DELETE) != ProviderMethod.DELETE)
                throw new NotSupportedException("Provider Delete method is not supported for " + this.GetType().Name);

            Expire(obj, ExpireMethodType.RemoveAll);

            //To-Do: enhance to use strategy pattern for Post, Put and Delete posts
            if (ResourceStrategy == ResourceStrategyType.LocalFile)
            {
                // instantiate RestfulObject to provide continuity with transaction event handlers
                var restObj = new RestfulObject<T>(obj, HttpVerb.Delete, GetUri(ObjectAbsoluteUri, MapParams(obj)));
                // obtain handle to existing item for transactional integrity
                var existingObj = Serializer.DeserializeObjectFromFile(restObj.UriEndpoint);
                try
                {
                    // attempt delete transaction
                    Device.File.Delete(restObj.UriEndpoint);
                    UpdateLocalListFile(obj, restObj.Verb);
                    OnTransactionComplete(restObj, restObj.Verb);
                }
                catch (Exception e)
                {
                    // rollback delete of file from data store
                    if (!Device.File.Exists(restObj.UriEndpoint))
                        Serializer.SerializeObjectToFile(existingObj, restObj.UriEndpoint);
                    OnTransactionFailed(restObj, restObj.Verb, new Exception("Provider Delete operation failed.", e));
                }
            }
            else
            {
                switch (Device.NetworkPostMethod)
                {
                    case MonoCross.Utilities.Network.NetworkPostMethod.QueuedAsynchronous:
                        queue.Enqueue(new RestfulObject<T>(obj, HttpVerb.Delete, GetUri(obj)) { PutPostDeleteHeaders = PutPostDeleteHeaders });
                        queue.SerializeQueue();
                        break;
                    case MonoCross.Utilities.Network.NetworkPostMethod.ImmediateSynchronous:
                        NetworkResponse NetworkResponse = Utilities.Network.Delete<T>(obj, GetUri(ObjectAbsoluteUri, MapParams(obj)), MergedHeaders, Format);
                        break;
                }
            }
        }
        void UpdateLocalListFile(T obj, string verb)
        {
            if (ResourceStrategy == ResourceStrategyType.LocalFile)
            {
                // obtain handles to existing list, and check for existing item
                var list = GetList().Object;
                if (list == null)
                    list = new List<T>();

                var existingItem = Get(obj).Object;
                switch (verb)
                {
                    case HttpVerb.Post:
                        {
                            // attempt add of item to list
                            if (existingItem == null)
                                list.Add(obj);
                            else
                                throw new ArgumentException("object already exists in local list file.");

                            break;
                        }
                    case HttpVerb.Put:
                        {
                            // attempt change of item in list
                            if (existingItem != null)
                            {
                                list.Remove(existingItem);
                                list.Add(obj);
                            }
                            else
                            {
                                throw new ArgumentException("Cannot find object in local list file.");
                            }
                            break;
                        }
                    case HttpVerb.Delete:
                        {
                            // attempt delete of item in list
                            if (existingItem != null)
                            {
                                list.Remove(existingItem);
                            }
                            else
                                throw new ArgumentException("Cannot find SampleData object in list file.");
                            break;
                        }
                    default:
                        break;
                }
                Serializer.SerializeListToFile(list, ListAbsoluteUri);
            }
        }

        /// <summary>
        /// Called after .Add or .Change is called (only when the PostMethod is NetworkPostMethod.ImmediateSynchronous)
        /// </summary>
        /// <param name="NetworkResponse"></param>
        protected void ProcessNetworkResponse(NetworkResponse NetworkResponse)
        {
            if (NetworkResponse != null && !String.IsNullOrEmpty(NetworkResponse.ResponseString) && QueueRequestReturnsObject)
            {
                //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
                //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( QueueSerializationFormat, AuxilliaryTypes );

                T newObj = Serializer.DeserializeObject(NetworkResponse.ResponseBytes, EncryptionMode.NoEncryption);
                string uri = GetUri(newObj);

                if (ResourceStrategy == ResourceStrategyType.Cache)
                    StoreInNrlCache(newObj, /* Serializer,*/ GetUri(newObj), NetworkResponse.Expiration, NetworkResponse.AttemptToRefresh);

                if (CacheMethod != CacheMethodType.PersistOnly)
                    cache[uri] = new RestfulObject<T>(newObj, uri)
                    {
                        ExpirationDate = NetworkResponse.Expiration,
                        AttemptRefreshDate = NetworkResponse.AttemptToRefresh,
                        LazyLoaded = true
                    };
            }
        }


        ///// <summary>
        ///// Abstract method to translate a provider item object to an iFactr iList item for binding to lists in the UI.
        ///// </summary>
        ///// <param name="baseUri">The base URI for the navigation action in iFactr.</param>
        ///// <param name="item">The item to be displayed on the list.</param>
        ///// <returns></returns>
        //public abstract iItem BindData(string baseUri, T item);

        ///// <summary>
        ///// Abstract method to translate a collecion of provider item object to iFactr iList items for binding to lists in the UI.
        ///// </summary>
        ///// <param name="baseUri">The base URI for the navigation action in iFactr</param>
        ///// <param name="items">The collection of item to be displayed on the list.</param>
        ///// <returns></returns>
        //public virtual IEnumerable<iItem> BindData(string baseUri, IEnumerable<T> items)
        //{
        //    return (IEnumerable<iItem>)(from item in items
        //                                select BindData(baseUri, item)).ToList();
        //}

        #region GetUri() Base Overloads

        /// <summary>
        /// Abstract method to map an item into a dictionary containing variables needed for the URI request.
        /// </summary>
        /// <param name="item">the object for which to obtain paramaters</param>
        /// <returns></returns>
        public abstract Dictionary<string, string> MapParams(T item);

        /// <summary>
        /// Returns an object relative URI for the item provided.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri(T item)
        {
            return GetUri(ObjectRelativeUri, MapParams(item));
        }

        /// <summary>
        /// Returns an object object relative URI.
        /// </summary>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            return GetUri(ObjectRelativeUri, parameters);
        }
        /// <summary>
        /// Returns an object relative URI with the parameter provided.
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining URI</param>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri(string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            return GetUri(ObjectRelativeUri, parameters);
        }
        /// <summary>
        /// Returns an object relative URI for the parameters provided.
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining URI</param>
        /// <param name="parm1">Second parameter to use in obtaining URI</param>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri(string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            return GetUri(ObjectRelativeUri, parameters);
        }
        /// <summary>
        /// Returns an object relative URI for the parameters provided.
        /// </summary>
        /// <param name="parm0">First parameter to use in obtaining URI</param>
        /// <param name="parm1">Second parameter to use in obtaining URI</param>
        /// <param name="parm2">Second parameter to use in obtaining URI</param>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri(string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            return GetUri(ObjectRelativeUri, parameters);
        }
        /// <summary>
        /// Returns an object relative URI for the parameters provided.
        /// </summary>
        /// <param name="parms">string array of parameter values to use in obtaining URI</param>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri(params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            return GetUri(ObjectRelativeUri, parameters);
        }
        /// <summary>
        /// Returns an object relative URI for the parameters provided.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri(Dictionary<string, string> parameters)
        {
            return GetUri(ObjectRelativeUri, parameters);
        }
        /// <summary>
        /// Returns an object relative URI for the parameters provided.
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="parameters"></param>
        /// <returns>Relative URI from which to obtain object</returns>
        public string GetUri(string baseUri, Dictionary<string, string> parameters)
        {
            string retval = baseUri;
            MatchCollection matchargs = Regex.Matches(retval, @"{(?<Name>\w+)}");
            foreach (Match arg in matchargs)
            {
                if (parameters.ContainsKey(arg.Groups["Name"].Value))
                    retval = retval.Replace("{" + arg.Groups["Name"].Value + "}", parameters[arg.Groups["Name"].Value]);
                else
                    retval = retval.Replace("{" + arg.Groups["Name"].Value + "}", string.Empty);
            }
            return retval;
        }

        #endregion

        private ResourceStrategyType _resourceStrategy = ResourceStrategyType.Cache;

        /// <summary>
        /// Gets or sets the resource strategy for the provider.
        /// </summary>
        /// <value>The entity level NRL ResourceStrategy to use for obtaining and caching objects from a RESTful service.</value>
        protected ResourceStrategyType ResourceStrategy
        {
            get
            {
                if (Device.NetworkGetMethod == MonoCross.Utilities.Network.NetworkGetMethod.NoCache)
                    return ResourceStrategyType.DirectStream;
                return _resourceStrategy;
            }
            set
            {
                _resourceStrategy = value;
            }
        }

        /// <summary>
        /// Gets or sets the cache stale method for the provider.
        /// </summary>
        /// <value>The method used to attempt refresh of stale items in the cache.  The default value is CacheStaleMethod.Deferred.</value>
        public CacheStaleMethod CacheStaleMethod
        {
            get;
            set;
        }

        /// <summary>
        /// Gets an items from the cache using the parameters provided.
        /// </summary>
        /// <param name="parameters">The parameters used to retrieve the item from the cache.</param>
        /// <returns></returns>
        protected virtual ProviderResponse<T> GetCacheResource(Dictionary<string, string> parameters)
        {
            DateTime dtMetric = DateTime.UtcNow;

            // load from Network Resource Library
            string relUri = GetUri(ObjectRelativeUri, parameters);
            string absUri = GetUri(ObjectAbsoluteUri, parameters);

            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = this.CacheStaleMethod,
                TimeoutMilliseconds = this.ResponseTimeout,
                Expiration = this.DefaultExpiration,
            };

            ResourceRequest request = NetworkResourceLibrary.Instance.GetResourceRequest(absUri, ResourceStrategy, args);
            ResourceResponse response = request.GetResponse();

            ProviderResponse<T> providerResponse = ProviderResponse<T>.Create(response.ReturnStatus);
            switch (ResourceStrategy)
            {
                case ResourceStrategyType.Cache:
                    providerResponse.ObjectSource = ObjectSource.NRLCache;
                    break;
                case ResourceStrategyType.DirectStream:
                    providerResponse.ObjectSource = ObjectSource.Server;
                    break;
                case ResourceStrategyType.LocalFile:
                    providerResponse.ObjectSource = ObjectSource.LocalFile;
                    break;
            }

            //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
            //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( QueueSerializationFormat, AuxilliaryTypes );

            providerResponse.Object = Serializer.DeserializeObject(response.GetResponseBytes(), EncryptionMode.NoEncryption);

            if (CacheMethod != CacheMethodType.PersistOnly)
            {
                RestfulObject<T> obj = new RestfulObject<T>(providerResponse.Object, relUri)
                {
                    ExpirationDate = (response.Expiration == DateTime.MinValue.ToUniversalTime() ? GetExpiration() : response.Expiration),
                    AttemptRefreshDate = response.AttemptToRefresh,
                    LazyLoaded = true
                };

                // write to in-memory cache
                if (cache.ContainsKey(relUri))
                    cache.Remove(relUri);

                cache.Add(relUri, obj);
            }

            Device.Log.Metric(string.Format("GetCacheResource: Uri: {0}  Time: {1:F0} milliseconds", absUri, DateTime.UtcNow.Subtract(dtMetric).TotalMilliseconds));

            return providerResponse;
        }

        /// <summary>
        /// Gets or sets the default expiration for cached items.
        /// </summary>
        /// <value>The default expiration as a TimeSpan.</value>
        protected virtual TimeSpan DefaultExpiration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets an expiration date based on the DefaultExpiration value.
        /// </summary>
        /// <returns>A DateTime representing the current provider expiration date.</returns>
        public virtual DateTime GetExpiration()
        {
            return (DefaultExpiration == TimeSpan.Zero ? DateTime.MaxValue : DateTime.UtcNow.Add(DefaultExpiration));
        }

        #region Serialization Functionality

        /// <summary>
        /// Gets or sets the serialization format.
        /// </summary>
        /// <value>The serialization format for the provider.  The default format is XML.</value>
        public SerializationFormat Format
        {
            get
            {
                return queue.Format;
            }
            set
            {
                queue.Format = value;
            }
        }

        private Type CustomSerializerType
        {
            get
            {
                return queue.CustomSerializerType;
            }
        }

        /// <summary>
        /// Gets or sets the item serializer for the provider.
        /// </summary>
        /// <value>An ISerializer instance for the provider item type.</value>
        public ISerializer<T> Serializer
        {
            get
            {
                return queue.Serializer;
            }
            set
            {
                queue.Serializer = value;
            }
        }

        #endregion

        #region List Cache Explosion Methods/Properties

        private CacheMethodType _listCacheMethod = CacheMethodType.Light;

        /// <summary>
        /// Gets or sets the cache method for the provider.
        /// </summary>
        /// <value>The cache method as a CacheMethodType value.  The default is CacheMethodType.Light.</value>
        protected CacheMethodType CacheMethod
        {
            get
            {
                return _listCacheMethod;
            }
            set
            {
                _listCacheMethod = value;
            }
        }

        private void ExplodeListCache(List<T> list, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            ExplodeListCache(CacheMethod, list, expirationDate, attemptRefreshDate);
        }
        private void ExplodeListCache(CacheMethodType cacheMethod, List<T> list, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            DateTime dt = DateTime.UtcNow;

            switch (cacheMethod)
            {
                case CacheMethodType.None:
                    ExplodeCacheNone(list);
                    break;
                case CacheMethodType.Light:
                    ExplodeCacheLight(list, expirationDate, attemptRefreshDate);
                    break;
                case CacheMethodType.Direct:
                    ExplodeCacheDirect(list, expirationDate, attemptRefreshDate);
                    break;
                case CacheMethodType.Retrieve:
                    ExplodeCacheRetrieve(list);
                    break;
                case CacheMethodType.PersistOnly:
                    ExplodeCachePersist(list, expirationDate, attemptRefreshDate);
                    break;
                default:
                    ExplodeCacheLight(list, expirationDate, attemptRefreshDate);
                    break;
            }

            Device.Log.Metric(string.Format("Exploding cache, type {0}, expense report {1}  ",
                                cacheMethod.ToString(), DateTime.UtcNow.Subtract(dt).TotalMilliseconds));
        }

        private void CacheItem(CacheMethodType cacheMethod, /* ISerializer<T> iSerializer, */ T item, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            switch (cacheMethod)
            {
                case CacheMethodType.None:
                    break;
                case CacheMethodType.Light:
                    CacheLight(item, expirationDate, attemptRefreshDate);
                    break;
                case CacheMethodType.Direct:
                    CacheDirect(item, /*iSerializer,*/ expirationDate, attemptRefreshDate);
                    break;
                case CacheMethodType.Retrieve:
                    CacheRetrieve(item);
                    break;
                case CacheMethodType.PersistOnly:
                    CachePersist(item, /*iSerializer,*/ expirationDate, attemptRefreshDate);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// performs no cache explosion, no in-memory caching performed.
        /// </summary>
        /// <param name="list"></param>
        private void ExplodeCacheNone(List<T> list)
        {
            //if ( list == null )
            //    return;

            // do nothing just return;
            return;
        }
        /// <summary>
        /// iterate through each item in the list and create lightly loaded entry in in-memory cache.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="expirationDate">The expiration date.</param>
        /// <param name="attemptRefreshDate">The attempt refresh date.</param>
        private void ExplodeCacheLight(List<T> list, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            if (list == null)
                return;

            // now that we have the list, install lightweight objects in memory cache, so they're part of CacheList
            // but haven't been loaded yet.
            foreach (var item in list)
                CacheLight(item, expirationDate, attemptRefreshDate);
        }

        private RestfulObject<T> CacheLight(T item, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            RestfulObject<T> obj = null;
            string uri = GetUri(item);

            // if the item in the list has been marked as deleted in the delta, then exclude it.
            var deleted = DeltaCache.Where(i => i.Uri == uri && i.Verb == HttpVerb.Delete);
            if (deleted.Count() > 0)
                return obj;

            if (CacheMethod != CacheMethodType.PersistOnly)
            {
                // only add if it's not already there.
                if (cache.ContainsKey(uri))
                {
                    obj = cache[uri];

                    // if object exists in cache, check for null, expired and not lazy loaded before updating it.
                    if (obj == null || obj.Object == null || (obj.ExpirationDate > DateTime.MinValue.ToUniversalTime() && obj.ExpirationDate < DateTime.UtcNow) || !obj.LazyLoaded || (obj.AttemptRefreshDate < DateTime.UtcNow && obj.AttemptRefreshDate > DateTime.MinValue.ToUniversalTime()))
                    {
                        cache[uri] = new RestfulObject<T>(item, uri)
                        {
                            LazyLoaded = false,
                            ExpirationDate = expirationDate,
                            AttemptRefreshDate = attemptRefreshDate
                        };
                    }
                }
                else
                {
                    cache.Add(uri, new RestfulObject<T>(item, uri)
                    {
                        LazyLoaded = false,
                        ExpirationDate = expirationDate,
                        AttemptRefreshDate = attemptRefreshDate
                    });
                }
            }
            return obj;
        }

        /// <summary>
        /// iterates each through each item in list and store directly in NRL Cache without placing object in the in-memory cache
        /// , this assumes the list contains all necessary information for direct caching
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="expirationDate">The expiration date.</param>
        /// <param name="attemptRefreshDate">The attempt refresh date.</param>
        private void ExplodeCachePersist(List<T> list, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            if (list == null)
                return;

            //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
            //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( QueueSerializationFormat, AuxilliaryTypes );

            // now that we have the list, install objects from list and into NRL but not into in-memory cache.
            foreach (var item in list)
                CachePersist(item, /*iSerializer,*/ expirationDate, attemptRefreshDate);
        }

        private RestfulObject<T> CachePersist(T item, /*ISerializer<T> iSerializer,*/ DateTime expirationDate, DateTime attemptRefreshDate)
        {
            if (ResourceStrategy != ResourceStrategyType.Cache)
                throw new NotSupportedException("CacheMethod of PersistOnly is not supported when provider ResourceStrategyType is not Cache");

            RestfulObject<T> obj = null;
            string uri = GetUri(item);

            // if the item in the list has been marked as deleted in the delta, then exclude it.
            var deleted = DeltaCache.Where(i => i.Uri == uri && i.Verb == HttpVerb.Delete);
            if (deleted.Count() > 0)
                return obj;

            StoreInNrlCache(item, /* Serializer, */ uri, expirationDate, attemptRefreshDate);

            return obj;
        }


        /// <summary>
        /// iterates each through each item in list and store directly in NRL Cache, this assumes the list contains all necessary information for direct caching
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="expirationDate">The expiration date.</param>
        /// <param name="attemptRefreshDate">The attempt refresh date.</param>
        private void ExplodeCacheDirect(List<T> list, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            if (list == null)
                return;

            //ISerializer<T> iSerializer = this.AuxilliaryTypes == null ?
            //    SerializerFactory.Create<T>( QueueSerializationFormat ) : SerializerFactory.Create<T>( QueueSerializationFormat, AuxilliaryTypes );

            // now that we have the list, install objects from list into in-memory cache and NRL
            foreach (var item in list)
                CacheDirect(item, /*iSerializer,*/ expirationDate, attemptRefreshDate);
        }
        private RestfulObject<T> CacheDirect(T item, /*ISerializer<T> iSerializer,*/ DateTime expirationDate, DateTime attemptRefreshDate)
        {
            //if ( ResourceStrategy != ResourceStrategyType.Cache )
            //    throw new NotSupportedException( "CacheMethod of Direct is not supported when provider ResourceStrategyType is not Cache" );

            RestfulObject<T> obj = null;
            string uri = GetUri(item);

            // if the item in the list has been marked as deleted in the delta, then exclude it.
            var deleted = DeltaCache.Where(i => i.Uri == uri && i.Verb == HttpVerb.Delete);
            if (deleted.Count() > 0)
            {
                Expire(uri, ExpireMethodType.RemoveAll);
                return obj;
            }

            if (ResourceStrategy == ResourceStrategyType.Cache)
                StoreInNrlCache(item, /*iSerializer,*/ uri, expirationDate, attemptRefreshDate);

            obj = new RestfulObject<T>(item, uri)
            {
                ExpirationDate = expirationDate,
                AttemptRefreshDate = attemptRefreshDate,
                LazyLoaded = true
            };

            cache[uri] = obj;
            return obj;
        }

        /// <summary>
        /// Stores the in NRL cache.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemUri">The item URI.</param>
        /// <param name="expirationDate">The expiration date.</param>
        /// <param name="attemptRefreshDate">The attempt refresh date.</param>
        protected void StoreInNrlCache(T item, /* ISerializer<T> iSerializer, */ string itemUri, DateTime expirationDate, DateTime attemptRefreshDate)
        {
            string uri = BaseUri.Insert(BaseUri.Length, itemUri);

            // added lock to alleviate rare condition where same object posted back from server
            // at same time (can occur with inconsistent serialization timings)
            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(uri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(uri);
            cacheIndexItem.Expiration = expirationDate;
            cacheIndexItem.AttemptToRefresh = attemptRefreshDate;

            lock (syncLock)
            {
                Serializer.SerializeObjectToFile(item, cacheIndex.GetCachePath(cacheIndexItem));
            }

            CacheIndex.SerializeCacheIndexImmediate(cacheIndex);
        }

        /// <summary>
        /// Retrieves a given URI from the NRL and stores in the in-memory cache.
        /// </summary>
        /// <param name="itemUri"></param>
        private void RetrieveFromNrlCache(string itemUri /*, ISerializer<T> iSerializer */ )
        {
            string uri = BaseUri.Insert(BaseUri.Length, itemUri);

            // added lock to alleviate rare condition where same object posted back from server
            // at same time (can occur with inconsistent serialization timings)
            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(uri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(uri);

            T obj = default(T);
            lock (syncLock)
            {
                obj = Serializer.DeserializeObjectFromFile(cacheIndex.GetCachePath(cacheIndexItem));
            }

            RestfulObject<T> restObj = new RestfulObject<T>(obj, itemUri)
            {
                ExpirationDate = cacheIndexItem.Expiration,
                AttemptRefreshDate = cacheIndexItem.AttemptToRefresh,
                LazyLoaded = true
            };

            // write to in-memory cache
            if (CacheMethod != CacheMethodType.PersistOnly)
                cache[itemUri] = restObj;
        }


        /// <summary>
        /// Iterates through each item in the list, and calls Get(item) for each, so fully loaded items are in the in-memory cache
        /// </summary>
        /// <param name="list"></param>
        private void ExplodeCacheRetrieve(List<T> list)
        {
            if (ResourceStrategy != ResourceStrategyType.Cache)
                throw new NotSupportedException("CacheMethodType of Retrieve is not supported when provider ResourceStrategyType is not Cache");

            if (list == null)
                return;

            // now that we have the list of lightly loaded objects, call get to retrieve full versions of each item
            // into the the cache.
            foreach (var item in list)
                CacheRetrieve(item);
        }

        private void CacheRetrieve(T item)
        {
            if (ResourceStrategy != ResourceStrategyType.Cache)
                throw new NotSupportedException("CacheMethodType of Retrieve is not supported when provider ResourceStrategyType is not Cache");

            string uri = GetUri(item);
            // if the item in the list has been marked as deleted in the delta, then exclude it.
            var deleted = DeltaCache.Where(i => i.Uri == uri && i.Verb == HttpVerb.Delete);
            if (deleted.Count() > 0)
                return;

            Get(item);
        }

        #endregion

        #region Provider Supported Rest Methods property

        private ProviderMethod _providerMethods = ProviderMethod.GET | ProviderMethod.POST | ProviderMethod.PUT | ProviderMethod.DELETE;
        /// <summary>
        /// Gets or sets the provider methods supported by the data provider.
        /// </summary>
        /// <value>The supported provider methods.  All provider methods, (GET, POST, PUT and DELETE), are supported by default.</value>
        protected ProviderMethod ProviderMethods
        {
            get
            {
                return _providerMethods;
            }
            set
            {
                _providerMethods = value;
            }
        }

        #endregion

        #region void Expire() overloads

        /// <summary>
        /// Determines whether the specified item is stale.
        /// </summary>
        /// <param name="obj">The item to check.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is stale; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStale(T obj)
        {
            RestfulObject<T> restfulObj = null;
            string uri = GetUri(MapParams(obj));

            //if ( queue.Count() > 0 )
            //{
            //    restfulObj = queue.Where( item => item.UriEndpoint == uri ).FirstOrDefault();

            //    if ( obj != default(RestfulObject<T>)  )
            //        return true
            //}

            // check in-memory cache
            if (cache.ContainsKey(uri))
                restfulObj = cache[uri];

            // if object is null, then load from Network Resource Library which manages the cache and expiration.
            if (restfulObj == null || restfulObj.Object == null || restfulObj.AttemptRefreshDate < DateTime.UtcNow)
                return true;

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(uri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(uri, false);
            if (cacheIndexItem == null)
                return false;

            if (cacheIndexItem.IsStale)
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is stale.
        /// </summary>
        /// <param name="parameters">The parameters of the item to check.</param>
        /// <returns>
        ///     <c>true</c> if the item is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsStale(Dictionary<string, string> parameters)
        {
            string uri = GetUri(ObjectAbsoluteUri, parameters);

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(uri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(uri, false);
            if (cacheIndexItem == null)
                return false;

            return cacheIndexItem.IsStale;
        }

        /// <summary>
        /// Determines whether this instance is stale.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsStale()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            return IsStale(parameters);
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsStale(string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            return IsStale(parameters);
        }
        /// <summary>
        /// Determines whether the item specified by the parameters provided is stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsStale(string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            return IsStale(parameters);
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <param name="parm2">The parm2.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsStale(string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            return IsStale(parameters);
        }
        /// <summary>
        /// Determines whether the item specified by the parameters provided is stale..
        /// </summary>
        /// <param name="parms">The parms.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsStale(params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            return IsStale(parameters);
        }

        /// <summary>
        /// Determines whether the specified item is expired.
        /// </summary>
        /// <param name="obj">The item to check.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is expired; otherwise, <c>false</c>.
        /// </returns>
        public bool IsExpired(T obj)
        {
            RestfulObject<T> restfulObj = null;
            string uri = GetUri(MapParams(obj));

            //if ( queue.Count() > 0 )
            //{
            //    restfulObj = queue.Where( item => item.UriEndpoint == uri ).FirstOrDefault();

            //    if ( obj != default(RestfulObject<T>)  )
            //        return true
            //}

            // check in-memory cache
            if (cache.ContainsKey(uri))
                restfulObj = cache[uri];

            // if object is null, then load from Network Resource Library which manages the cache and expiration.
            if (restfulObj == null || restfulObj.Object == null || restfulObj.ExpirationDate < DateTime.UtcNow)
                return true;

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(uri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(uri, false);
            if (cacheIndexItem == null)
                return false;

            if (cacheIndexItem.IsExpired)
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is expired.
        /// </summary>
        /// <param name="parameters">The parameters of the item to check.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsExpired(Dictionary<string, string> parameters)
        {
            string uri = GetUri(ObjectAbsoluteUri, parameters);

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(uri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(uri, false);
            if (cacheIndexItem == null)
                return false;

            return cacheIndexItem.IsExpired;
        }

        /// <summary>
        /// Determines whether this instance is expired.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsExpired()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            return IsExpired(parameters);
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is expired.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsExpired(string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            return IsExpired(parameters);
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is expired.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsExpired(string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            return IsExpired(parameters);
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is expired.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <param name="parm2">The parm2.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsExpired(string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            return IsExpired(parameters);
        }

        /// <summary>
        /// Determines whether the item specified by the parameters provided is expired.
        /// </summary>
        /// <param name="parms">The parms.</param>
        /// <returns>
        ///     <c>true</c> if the specified item is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsExpired(params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            return IsExpired(parameters);
        }

        /// <summary>
        /// Expires an item and removes it from cache.
        /// </summary>
        /// <param name="obj">the item to expire and remove.  
        /// This method removes cache metadata as well as the file from cache.</param>
        public void Expire(T obj)
        {
            Expire(obj, ExpireMethodType.ExpireRemove);
        }

        /// <summary>
        /// Expires an item and optionally removes it from cache.
        /// </summary>
        /// <param name="obj">object to expire</param>
        /// <param name="expireMethod">The expire method.</param>
        public void Expire(T obj, ExpireMethodType expireMethod)
        {
            Dictionary<string, string> parameters = MapParams(obj);

            string relUri = GetUri(ObjectRelativeUri, parameters);

            Expire(relUri, expireMethod);
        }

        /// <summary>
        /// Expires all items contained within a list and removes all associated item files from cache.
        /// </summary>
        /// <param name="list">List containing items to expire and remove.</param>
        public void Expire(List<T> list)
        {
            Expire(list, ExpireMethodType.ExpireRemove);
        }

        /// <summary>
        /// Expires all items contained within a list and optionally removes all associated item files from cache.
        /// </summary>
        /// <param name="list">List containing items to expire and remove.</param>
        /// <param name="expireMethod">The expire method.</param>
        public void Expire(List<T> list, ExpireMethodType expireMethod)
        {
            foreach (T item in list)
                Expire(item, expireMethod);
        }

        /// <summary>
        /// Expires an items and optionally removes it from cache.
        /// </summary>
        /// <param name="relUri">The item to expire and remove.</param>
        /// <param name="expireMethod">The expire method.</param>
        public void Expire(string relUri, ExpireMethodType expireMethod)
        {
            Expire(relUri, expireMethod, CachePeriod.Expired());
        }
        /// <summary>
        /// Expires the item specified by the URI, using the expiration method, and date provided.
        /// </summary>
        /// <param name="relUri">The relative URI of the item to expire.</param>
        /// <param name="expireMethod">The expiration method to use.</param>
        /// <param name="expireDate">The expiration date for the item.</param>
        public void Expire(string relUri, ExpireMethodType expireMethod, DateTime expireDate)
        {
            if (cache.ContainsKey(relUri))
            {
                switch (expireMethod)
                {
                    case ExpireMethodType.ExpireOnly:
                        cache[relUri].ExpirationDate = expireDate;
                        break;
                    case ExpireMethodType.ExpireRemove:
                        cache[relUri].ExpirationDate = expireDate;
                        break;
                    case ExpireMethodType.StaleCache:
                        cache[relUri].ExpirationDate = expireDate;
                        break;
                    case ExpireMethodType.RemoveAll:
                        cache.Remove(relUri);
                        break;
                    case ExpireMethodType.RemoveCache:
                        cache.Remove(relUri);
                        break;
                }
            }

            // if the provider ResourceStrategy is Cache, then also expire the item in cache.
            if (ResourceStrategy == ResourceStrategyType.Cache)
            {
                if (expireMethod == ExpireMethodType.RemoveCache)
                    return;

                string absUri = BaseUri.AppendPath(relUri);
                CacheIndex cacheIndex = CacheIndexMap.GetFromUri(absUri);
                CacheIndexItem cacheIndexItem = cacheIndex.Get(absUri, false);
                if (cacheIndexItem == null)
                    return;

                switch (expireMethod)
                {
                    case ExpireMethodType.ExpireOnly:
                        cacheIndexItem.Expire();
                        break;
                    case ExpireMethodType.StaleCache:
                        cacheIndexItem.Stale();
                        break;
                    case ExpireMethodType.ExpireRemove:
                        cacheIndex.RemoveCurrentCache(cacheIndexItem);
                        break;
                    case ExpireMethodType.RemoveAll:
                        cacheIndex.RemoveCurrentCache(cacheIndexItem);
                        cacheIndex.Remove(cacheIndexItem);
                        break;
                    //case ExpireMethodType.RemoveCache:
                    //    // do nothing, RemoveCache is for removing in-memory cache only while leaving NRL metadata/files intact
                    //    break;
                }
                CacheIndex.SerializeCacheIndex(cacheIndex);
            }
        }

        /// <summary>
        /// Expires and removes items from the in-memory cache, but leaves NRL metadata and cached files intact.
        /// </summary>
        public void ExpireCacheList()
        {
            ExpireCacheList(ExpireMethodType.RemoveCache);
        }
        /// <summary>
        /// Expires in-memory cache, based on the expire method provided.
        /// </summary>
        /// <param name="expireMethod">The expiration method to use.</param>
        public void ExpireCacheList(ExpireMethodType expireMethod)
        {
            List<RestfulObject<T>> cacheitems = cache.Values.ToList();
            foreach (RestfulObject<T> item in cacheitems)
            {
                Expire(item.UriEndpoint, expireMethod);
            }
        }

        #endregion

        #region void ExpireList() Overloads

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parameters">The parameters used to identify the list to check.</param>
        /// <returns>
        ///     <c>true</c> if the list is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListStale(Dictionary<string, string> parameters)
        {
            string listUri = GetUri(ListAbsoluteUri, parameters);

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(listUri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(listUri, false);
            if (cacheIndexItem == null)
                return false;
            return cacheIndexItem.IsStale;
        }


        /// <summary>
        /// Determines whether the list is stale.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the list is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListStale()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            return IsListStale(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <returns>
        ///     <c>true</c> if the list is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListStale(string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            return IsListStale(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <returns>
        ///     <c>true</c> if the list is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListStale(string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            return IsListStale(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <param name="parm2">The parm2.</param>
        /// <returns>
        ///     <c>true</c> if the list is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListStale(string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            return IsListStale(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parms">The parameters for the list to check.</param>
        /// <returns>
        ///     <c>true</c> if the list is stale; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListStale(params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            return IsListStale(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        ///     <c>true</c> if the list is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListExpired(Dictionary<string, string> parameters)
        {
            string listUri = GetUri(ListAbsoluteUri, parameters);

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(listUri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(listUri, false);
            if (cacheIndexItem == null)
                return false;

            return cacheIndexItem.IsExpired;
        }

        /// <summary>
        /// Determines whether the list is expired.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the list is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListExpired()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            return IsListExpired(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameter provided has become stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <returns>
        ///     <c>true</c> if the list is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListExpired(string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            return IsListExpired(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <returns>
        ///     <c>true</c> if the list is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListExpired(string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            return IsListExpired(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <param name="parm2">The parm2.</param>
        /// <returns>
        ///     <c>true</c> if the list is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListExpired(string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            return IsListExpired(parameters);
        }

        /// <summary>
        /// Determines whether the list specified by the parameters provided has become stale..
        /// </summary>
        /// <param name="parms">The parameters of the list to check.</param>
        /// <returns>
        ///     <c>true</c> if the list is expired; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsListExpired(params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            return IsListExpired(parameters);
        }

        /// <summary>
        /// Expires the list using the expire method and parameters provided.
        /// </summary>
        /// <param name="expireMethod">The expire method to use.</param>
        /// <param name="parameters">The parameters for the list to expire.</param>
        public virtual void ExpireList(ExpireMethodType expireMethod, Dictionary<string, string> parameters)
        {
            if (ResourceStrategy != ResourceStrategyType.Cache)
                return;

            string listUri = GetUri(ListAbsoluteUri, parameters);

            CacheIndex cacheIndex = CacheIndexMap.GetFromUri(listUri);
            CacheIndexItem cacheIndexItem = cacheIndex.Get(listUri, false);

            if (cacheIndexItem == null)
                return;

            switch (expireMethod)
            {
                case ExpireMethodType.ExpireOnly:
                    cacheIndexItem.Expire();
                    break;
                case ExpireMethodType.StaleCache:
                    cacheIndexItem.Stale();
                    break;
                case ExpireMethodType.ExpireRemove:
                    cacheIndex.RemoveCurrentCache(cacheIndexItem);
                    break;
                case ExpireMethodType.RemoveAll:
                    cacheIndex.RemoveCurrentCache(cacheIndexItem);
                    cacheIndex.Remove(cacheIndexItem);
                    break;
            }
            CacheIndex.SerializeCacheIndex(cacheIndex);
        }

        /// <summary>
        /// Expires the list.
        /// </summary>
        public virtual void ExpireList()
        {
            ExpireList(ExpireMethodType.ExpireRemove);
        }

        /// <summary>
        /// Expires the list usint the expire method provided.
        /// </summary>
        /// <param name="expireMethod">The expire method to use.</param>
        public virtual void ExpireList(ExpireMethodType expireMethod)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            ExpireList(expireMethod, parameters);
        }

        /// <summary>
        /// Expires the list using the parameter provided.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        public virtual void ExpireList(string parm0)
        {
            ExpireList(ExpireMethodType.ExpireRemove, parm0);
        }

        /// <summary>
        /// Expires the list using the expire method and parameter provided.
        /// </summary>
        /// <param name="expireMethod">The expire method.</param>
        /// <param name="parm0">The parm0.</param>
        public virtual void ExpireList(ExpireMethodType expireMethod, string parm0)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            ExpireList(expireMethod, parameters);
        }

        /// <summary>
        /// Expires the list using the parameters provided.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        public virtual void ExpireList(string parm0, string parm1)
        {
            ExpireList(ExpireMethodType.ExpireRemove, parm0, parm1);
        }

        /// <summary>
        /// Expires the list usint the expire method and parameters provided.
        /// </summary>
        /// <param name="expireMethod">The expire method to use.</param>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        public virtual void ExpireList(ExpireMethodType expireMethod, string parm0, string parm1)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            ExpireList(expireMethod, parameters);
        }

        /// <summary>
        /// Expires the list using the parameters provided.
        /// </summary>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <param name="parm2">The parm2.</param>
        public virtual void ExpireList(string parm0, string parm1, string parm2)
        {
            ExpireList(ExpireMethodType.ExpireRemove, parm0, parm1, parm2);
        }

        /// <summary>
        /// Expires the list usint the expire method and parameters provided.
        /// </summary>
        /// <param name="expireMethod">The expire method to use.</param>
        /// <param name="parm0">The parm0.</param>
        /// <param name="parm1">The parm1.</param>
        /// <param name="parm2">The parm2.</param>
        public virtual void ExpireList(ExpireMethodType expireMethod, string parm0, string parm1, string parm2)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(KeyParameter[0], parm0);
            parameters.Add(KeyParameter[1], parm1);
            parameters.Add(KeyParameter[2], parm2);
            ExpireList(expireMethod, parm0, parm1, parm2);
        }

        /// <summary>
        /// Expires the list using the parameters provided.
        /// </summary>
        /// <param name="parms">The parameters for the list to expire.</param>
        public virtual void ExpireList(params string[] parms)
        {
            ExpireList(ExpireMethodType.ExpireRemove, parms);
        }

        /// <summary>
        /// Expires the list usint the expire method and parameters provided.
        /// </summary>
        /// <param name="expireMethod">The expire method to use.</param>
        /// <param name="parms">The parameters of the list to expire.</param>
        public virtual void ExpireList(ExpireMethodType expireMethod, params string[] parms)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < parms.Length; i++)
            {
                parameters.Add(KeyParameter[i], parms[i]);
            }
            ExpireList(expireMethod, parameters);
        }

        #endregion
    }

    /// <summary>
    /// Specifies the cache methods available on a data provider.
    /// </summary>
    public enum CacheMethodType
    {
        /// <summary>
        /// None: No List Item Caching
        /// </summary>
        None,
        /// <summary>
        /// Light: place lightweight (i.e. unloaded) objects in CacheList but no NRL Caching
        /// </summary>
        Light,
        /// <summary>
        /// Direct: Separate List into individual objects and place directly into NRL Cache.
        /// </summary>
        Direct,
        /// <summary>
        /// Retrieve: Separate List into individual objects and call Get() on each. to store full objects in CacheList and NRL Cache.
        /// </summary>
        Retrieve,
        /// <summary>
        /// PersistOnly: Separate List into individual objects and place directly into NRL Cache, but not in in-memory cache.
        /// </summary>
        PersistOnly
    }

    /// <summary>
    /// Indicates the valid values for the expire method type.
    /// </summary>
    public enum ExpireMethodType
    {
        /// <summary>
        /// ExpireOnly: Expire InMemory cache and NRL metadata, do not remove NRL cached file
        /// </summary>
        ExpireOnly,
        /// <summary>
        /// ExpireRemove: Expire InMemory cache and NRL metadata, and remove NRL cached file
        /// </summary>
        ExpireRemove,
        /// <summary>
        /// RemoveAll: Remove InMemory cache and NRL, and remove NRL cached file
        /// </summary>
        RemoveAll,
        /// <summary>
        /// RemoveCache: Remove InMemory cache and leave NRL metadata and cached files intact.
        /// </summary>
        RemoveCache,
        /// <summary>
        /// StaleCache: Expire InMemory cache, mark NRL metadata as stale and leave NRL cached files intact.
        /// </summary>
        StaleCache
    }
}
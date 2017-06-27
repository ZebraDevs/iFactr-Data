using MonoCross;
using MonoCross.Utilities;
using MonoCross.Utilities.Serialization;
using MonoCross.Utilities.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;

namespace iFactr.Data
{
    /// <summary>
    /// Represents a RESTful queue for provider transaction processing.
    /// </summary>
    /// <typeparam name="T">The generic type of the queue.</typeparam>
    [DataContract]
    public class RestfulQueue<T> : SyncQueue<RestfulObject<T>>
    {
        /// <summary>
        /// Defines the delegate for the OnRequestComplete event.
        /// </summary>
        public delegate void RequestComplete(RestfulObject<T> obj, string verb);
        /// <summary>
        /// Defines the delegate for the OnRequestError event.
        /// </summary>
        public delegate void RequestError(RestfulObject<T> obj, string verb, HttpStatusCode error);
        /// <summary>
        /// Defines the delegate for the OnRequestFailed event.
        /// </summary>
        public delegate void RequestFailed(RestfulObject<T> obj, string verb, Exception ex);

        /// <summary>
        /// Occurs when a RESTful transaction is successfully completed.
        /// </summary>
        public event RequestComplete OnRequestComplete;
        /// <summary>
        /// Occurs when a RESTful transaction fails due to an application exception.
        /// </summary>
        public event RequestFailed OnRequestFailed;
        /// <summary>
        /// Occurs when a RESTful transaction receives an HTTP error from the server.
        /// </summary>
        public event RequestError OnRequestError;

#if !NETFX_CORE
        private Timer timer;
#endif
        private const int timerDelay = 2000;
        private object syncLock = new object();

        private Type[] auxTypes;
        /// <summary>
        /// Gets or sets the auxilliary types.
        /// </summary>
        /// <value>The auxilliary types.</value>
        public Type[] AuxilliaryTypes
        {
            get { return auxTypes; }
            set { auxTypes = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether transactions should be dequeued when an error occurs.
        /// </summary>
        /// <value><c>true</c> if transactions should be dequeued; otherwise, <c>false</c>.</value>
        [DataMember(Order = 1)]
        public bool DequeueOnError
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the base URI.
        /// </summary>
        /// <value>The base URI.</value>
        [DataMember(Order = 2)]
        public string BaseUri
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the relative URI.
        /// </summary>
        /// <value>The relative URI.</value>
        [DataMember(Order = 3)]
        public string RelativeUri
        {
            get;
            set;
        }
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
                // Also UPDATE RestfulCache class also.
                // Something along the lines of:
                // return (BaseUri + ":8080").AppendPath(RelativeUri);
                return BaseUri.AppendPath(RelativeUri);
                // return BaseUri.Insert( BaseUri.Length, RelativeUri );
            }
        }
        /// <summary>
        /// Format - serialization format of business objects contained by the queue.
        /// </summary>
        [DataMember(Order = 4)]
        public SerializationFormat Format
        {
            get;
            set;
        }

        /// <summary>
        /// QueueSerializationFormat - serialization format of the restful queue containing the business objects.
        /// </summary>
        public SerializationFormat QueueSerializationFormat
        {
            get;
            set;
        }

        private int _responseTimeout = 60000;
        /// <summary>
        /// Gets or sets the response timeout.
        /// </summary>
        /// <value>The response timeout value in milliseconds.</value>
        public int ResponseTimeout
        {
            get
            {
                return _responseTimeout;
            }
            set
            {
                _responseTimeout = value;
            }
        }

        private ISerializer<T> _serializer;
        /// <summary>
        /// Gets or sets the serializer for the queue.
        /// </summary>
        /// <value>The serializer for the queue.</value>
        public ISerializer<T> Serializer
        {
            get
            {
                if (_serializer == null)
                {
                    switch (Format)
                    {
                        case SerializationFormat.XML:
                        case SerializationFormat.JSON:
                        case SerializationFormat.ODATA:
                            this.Serializer = (this.AuxilliaryTypes == null) ?
                                SerializerFactory.Create<T>(Format) : SerializerFactory.Create<T>(Format, AuxilliaryTypes);
                            break;
                        case SerializationFormat.CUSTOM:
                            if (CustomSerializerType != null)
                            {
                                var instance = Activator.CreateInstance(CustomSerializerType);
                                if (instance is ISerializer<T>)
                                    _serializer = (ISerializer<T>)instance;
                            }
                            else
                            {
                                throw new ArgumentException("CUSTOM serializer format specified, but no CustomSerializer Type supplied");
                            }
                            break;
                    }
                }
                return _serializer;
            }
            set
            {
                _serializer = value;
            }
        }

        private ISerializer<RestfulObject<T>> _queueSerializer;
        /// <summary>
        /// Gets or sets the queue serializer.
        /// </summary>
        /// <value>The queue serializer.</value>
        public ISerializer<RestfulObject<T>> QueueSerializer
        {
            get
            {
                if (_queueSerializer != null)
                    return _queueSerializer;

                switch (QueueSerializationFormat)
                {
                    case SerializationFormat.CUSTOM:
                        if (CustomQueueSerializerType != null)
                        {
                            var instance = Activator.CreateInstance(CustomQueueSerializerType);
                            if (instance is ISerializer<RestfulObject<T>>)
                                _queueSerializer = (ISerializer<RestfulObject<T>>)instance;
                        }
                        else
                        {
                            throw new ArgumentException("CustomQueueSerializerType must be supplied when QueueSerializationFormat is CUSTOM");
                        }
                        break;
                    case SerializationFormat.XML:
                    case SerializationFormat.JSON:
                    default:
                        _queueSerializer = SerializerFactory.Create<RestfulObject<T>>(QueueSerializationFormat);
                        break;
                }

                return _queueSerializer;


                //if ( _queueSerializer == null )
                //{
                //    if ( QueueSerializationFormat == SerializationFormat.CUSTOM )
                //    {
                //    }
                //    else
                //        _queueSerializer = SerializerFactory.Create<RestfulObject<T>>( QueueSerializationFormat );

                //    //_queueSerializer = ( QueueSerializationFormat == SerializationFormat.CUSTOM )
                //    //                      ? SerializerFactory.Create<RestfulObject<T>>( SerializationFormat.XML )
                //    //                      : SerializerFactory.Create<RestfulObject<T>>( QueueSerializationFormat );
                //}
                //return _queueSerializer;
            }
            set { _queueSerializer = value; }
        }

        private Type _customSerializerType;
        /// <summary>
        /// Gets or sets the type of the custom serializer.
        /// </summary>
        /// <value>The type of the custom serializer.</value>
        public Type CustomSerializerType
        {
            get { return _customSerializerType; }
            private set
            {
                if (value == null)
                {
                    _customSerializerType = null;
                    _serializer = null;
                    return;
                }
                ValidateCustomSerializerType<T>(value);

                _customSerializerType = value;
            }
        }

        private Type _customQueueSerializerType;
        /// <summary>
        /// Gets or sets the type of the custom queue serializer.
        /// </summary>
        /// <value>The type of the custom queue serializer.</value>
        public Type CustomQueueSerializerType
        {
            get { return _customQueueSerializerType; }
            private set
            {
                if (value == null)
                {
                    _customQueueSerializerType = null;
                    _queueSerializer = null;
                    return;
                }

                ValidateCustomSerializerType<RestfulObject<T>>(value);

                _customQueueSerializerType = value;
            }
        }

        private static void ValidateCustomSerializerType<TS>(Type value)
        {
            // before allowing set to occur, confirm type is allowed as ISerializer<>
#if !NETCF
            var item = System.Reflection.IntrospectionExtensions.GetTypeInfo(value).ImplementedInterfaces
                .Where(x => System.Reflection.IntrospectionExtensions.GetTypeInfo(x).IsGenericType && x.GetGenericTypeDefinition() == typeof(ISerializer<>))
                .FirstOrDefault(t => System.Reflection.IntrospectionExtensions.GetTypeInfo(t).GenericTypeArguments.First().Name == typeof(TS).Name);
#else
            var item = value.GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISerializer<>))
                .FirstOrDefault(t => t.GetGenericArguments().First().Name == typeof(TS).Name);
#endif

            if (item == null)
                throw new Exception("Type of " + value.Name + " is not a valid ISerializer<" + typeof(TS).Name + ">");
        }

        private bool _enabled = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RestfulQueue&lt;T&gt;"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [DataMember(Order = 5)]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                if (value && this.Any())
                    TriggerTimer();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulQueue&lt;T&gt;"/> class.
        /// </summary>
        public RestfulQueue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="relativeUri">The relative URI.</param>
        public RestfulQueue(string baseUri, string relativeUri)
        {
            if (baseUri == null || relativeUri == null)
            {
                throw new ArgumentNullException("Neither baseUri nor relativeUri can be null");
            }
            BaseUri = baseUri;
            RelativeUri = relativeUri;
            QueueSerializationFormat = SerializationFormat.XML;
            RequestReturnsObject = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="relativeUri">The relative URI.</param>
        /// <param name="queueSerializationFormat">The queue serialization format.</param>
        public RestfulQueue(string baseUri, string relativeUri, SerializationFormat queueSerializationFormat)
            : this(baseUri, relativeUri, queueSerializationFormat, null, SerializationFormat.XML, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="relativeUri">The relative URI.</param>
        /// <param name="queueSerializationFormat">The queue serialization format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        public RestfulQueue(string baseUri, string relativeUri, SerializationFormat queueSerializationFormat, Type customQueueSerializationType)
            : this(baseUri, relativeUri, queueSerializationFormat, customQueueSerializationType, SerializationFormat.XML, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="relativeUri">The relative URI.</param>
        /// <param name="queueSerializationFormat">The queue serialization format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        /// <param name="serializationFormat">The serialization format.</param>
		public RestfulQueue(string baseUri, string relativeUri, SerializationFormat queueSerializationFormat, Type customQueueSerializationType, SerializationFormat serializationFormat)
		: this(baseUri, relativeUri, queueSerializationFormat, customQueueSerializationType, serializationFormat, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="relativeUri">The relative URI.</param>
        /// <param name="queueSerializationFormat">The queue serialization format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        /// <param name="serializationFormat">The serialization format.</param>
        /// <param name="customSerializationType">Type of the custom serialization.</param>
        public RestfulQueue(string baseUri, string relativeUri, SerializationFormat queueSerializationFormat,
                            Type customQueueSerializationType,
                            SerializationFormat serializationFormat,
		                    Type customSerializationType)
		: this(baseUri, relativeUri, queueSerializationFormat, customQueueSerializationType, serializationFormat, customSerializationType, -1) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestfulQueue&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="relativeUri">The relative URI.</param>
        /// <param name="queueSerializationFormat">The queue serialization format.</param>
        /// <param name="customQueueSerializationType">Type of the custom queue serialization.</param>
        /// <param name="serializationFormat">The serialization format.</param>
        /// <param name="customSerializationType">Type of the custom serialization.</param>
        /// <param name="responseTimeout">The response timeout.</param>
        public RestfulQueue(string baseUri, string relativeUri, SerializationFormat queueSerializationFormat,
            Type customQueueSerializationType,
            SerializationFormat serializationFormat,
            Type customSerializationType,
		    int responseTimeout)
        {
            if (serializationFormat == SerializationFormat.CUSTOM && customSerializationType == null)
                throw new ArgumentException("Parameter customSerializationType must not be null when serializationFormat is CUSTOM");
            if (queueSerializationFormat == SerializationFormat.CUSTOM && customQueueSerializationType == null)
                throw new ArgumentException("Parameter customQueueSerializationType must not be null when queueSerializationFormat is CUSTOM");
            if (baseUri == null || relativeUri == null)
            {
                throw new ArgumentNullException("Neither baseUri nor relativeUri can be null");
            }
            BaseUri = baseUri;
            RelativeUri = relativeUri;
            QueueSerializationFormat = queueSerializationFormat;
            CustomQueueSerializerType = customQueueSerializationType;
            Format = serializationFormat;
            CustomSerializerType = customSerializationType;
            RequestReturnsObject = true;
            ResponseTimeout = responseTimeout;
        }

        private void TriggerTimer()
        {
            if (!Enabled)
                return;
#if NETFX_CORE
            iApp.Thread.QueueIdle(o => 
            { 
                new ManualResetEvent(false).WaitOne(timerDelay);
                AttemptNextTransaction(); 
            });
#else
            if (timer != null)
                timer.Change(timerDelay, Timeout.Infinite);
            else
            {
                timer = new Timer(new TimerCallback((o) =>
                {
                    AttemptNextTransaction();
                }), null, timerDelay, Timeout.Infinite);
            }
#endif
        }

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public new void Enqueue(RestfulObject<T> item)
        {
            lock (syncLock) // Enqueue is locked until current queue is empted by AttemptNextTransaction()
            {
                //Device.Log.Debug("Enqueue Item: item: " + item.Object.ToString() + " type: " + typeof(T).ToString() );
                RestfulObject<T> queueitem = this.FirstOrDefault(exitem => exitem.UriEndpoint == item.UriEndpoint);
                if (queueitem != null)
                {
                    if (item.Verb == HttpVerb.Delete)
                    {
                        queueitem.Verb = HttpVerb.Delete;
                        //queueitem.Verb = HttpVerb.None; 
                        //base.Enqueue(item);
                    }
                    else
                    {
                        queueitem.Object = item.Object;
                    }
                }
                else
                {
                    base.Enqueue(item);
                }

            }
            TriggerTimer();
        }

        /// <summary>
        /// Attempts the next transaction.
        /// </summary>
        public void AttemptNextTransaction()
        {
            HttpStatusCode statusCode;
            bool exitLoop = false;

            lock (syncLock)
            {

                Device.Log.Debug(string.Format("AttemptNextTransaction:  count  {0}   exitloop: {1} ", this.Count, exitLoop));

                int qCount = this.Count;
                if (qCount <= 0)
                    return;

                while (this.Count > 0 && !exitLoop)
                {
                    RestfulObject<T> nextItem = Peek();

                    if (nextItem == null || nextItem.Verb == HttpVerb.None)
                    {
                        Device.Log.Debug(string.Format("AttemptNextTransaction:  next item is null or Verb == None"));
                        Dequeue();
                        continue;
                    }

                    try
                    {
                        Device.Log.Debug("Make Post Request: item: " + nextItem.Object.ToString() + " type: " + typeof(T).ToString());

                        RestfulObject<T> responseItem;
                        statusCode = MakeRequest(nextItem, out responseItem);

                        switch (statusCode)
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                            case HttpStatusCode.Accepted:
                            case HttpStatusCode.NoContent:
                                Device.Log.Debug("Queue Processing Success: result status " + statusCode.ToString() + " item: " + nextItem.ToString() + " type: " + typeof(T).ToString());
                                Dequeue();
                                SerializeQueue();
                                if (OnRequestComplete != null && responseItem != null)
                                        OnRequestComplete(responseItem, responseItem.Verb);
                                break;
                            case HttpStatusCode.Unauthorized:
                            case HttpStatusCode.ServiceUnavailable:
                            case HttpStatusCode.RequestTimeout:
                            case HttpStatusCode.BadGateway:
                            case (HttpStatusCode)(-1):   // application exception from post
                            case (HttpStatusCode)(-2):   // no response object from post
                                Device.Log.Debug("Halt Queue Processing: item: " + nextItem.ToString() + " type: " + typeof(T).ToString());
                                // do not remove from queue but halt processing to prevent repeated calls to server until authorized
                                exitLoop = true;
                                if (OnRequestError != null)
                                    OnRequestError(nextItem, nextItem.Verb, statusCode);
                                if (DequeueOnError)
                                {
                                    Device.Log.Debug("Removing Object From Queue: item: " + nextItem.ToString() + " type: " + typeof(T).ToString());
                                    Dequeue();
                                    SerializeQueue();
                                }
                                continue;
                            //break;
                            default:
                                Device.Log.Debug(string.Format("Restful Queue<{0}>: Received Status {1} for {2} to {3}.  Removing from Queue.", typeof(T).Name, statusCode, nextItem.Verb, nextItem.TransactionEndpoint));
                                if (OnRequestError != null)
                                    OnRequestError(nextItem, nextItem.Verb, statusCode);
                                Dequeue();
                                SerializeQueue();
                                break;
                        }
                    }
                    catch (Exception exc)
                    {
                        if (OnRequestFailed != null)
                            OnRequestFailed(nextItem, nextItem.Verb, exc);
                    }


                }
            }
        }

        private HttpStatusCode MakeRequest(RestfulObject<T> obj, out RestfulObject<T> responseObj)
        {
            //ISerializer<T> iSerializer = SerializerFactory.Create<T>( QueueSerializationFormat );

            //Device.Log.Debug (string.QueueSerializationFormat ("Request Body: {0}", iSerializer.SerializeObject (obj.Object, iFactr.Core.Utilities.EncryptionMode.NoEncryption)));
            byte[] postBytes = Serializer.SerializeObjectToBytes(obj.Object, EncryptionMode.NoEncryption);
            var headers = MergeHeaders(obj);
            var body = Serializer.SerializeObject(obj.Object);

            // add OData Accept header
            if (Format == SerializationFormat.ODATA && !obj.PutPostDeleteHeaders.Contains("Accept"))
                obj.PutPostDeleteHeaders.Add("Accept", "application/json");

            NetworkResponse retval = Device.Network.Poster.PostBytes(BaseUri.AppendPath(obj.TransactionEndpoint), postBytes, Serializer.ContentType, obj.Verb,
                headers, obj.Object, _responseTimeout);

            // if Rest returns type and verb Put/Post then convert response to type T 
            // and call event with object to pass to subscriber (e.g. a provider)
            responseObj = default(RestfulObject<T>);

            if (retval.StatusCode == HttpStatusCode.OK
                 || retval.StatusCode == HttpStatusCode.Created
                 || retval.StatusCode == HttpStatusCode.Accepted
                 || retval.StatusCode == HttpStatusCode.NoContent)
            {
                if (RequestReturnsObject)
                {
                    if (obj.Verb == HttpVerb.Post || (obj.Verb == HttpVerb.Put && Format != SerializationFormat.ODATA))
                    {
                        obj.ExpirationDate = retval.Expiration;
                        obj.AttemptRefreshDate = retval.AttemptToRefresh;

                        if (retval.ResponseBytes != null)
                        {
                            T returnObj = Serializer.DeserializeObject(retval.ResponseBytes, EncryptionMode.NoEncryption);
                            if (returnObj == null)
                            {
                                responseObj = obj.Clone(returnObj);
                                return retval.StatusCode;
                            }

                            responseObj = obj.Clone(returnObj);
                        }
                        else 
                        {
                            responseObj = obj.Clone(default(T));
                        }
                    }
                    else if (obj.Verb == HttpVerb.Delete || (obj.Verb == HttpVerb.Put && Format == SerializationFormat.ODATA))
                    {
                        responseObj = obj.Clone(obj.Object);  // set response object to return if DELETE or OData PUT
                    }
                }
                else
                {
                    responseObj = obj.Clone(obj.Object);

                    obj.ExpirationDate = retval.Expiration;
                    obj.AttemptRefreshDate = retval.AttemptToRefresh;
                }

                //if ( RequestReturnsObject && ( obj.Verb == HttpVerb.Post || obj.Verb == HttpVerb.Put ) )
                //{
                //    T returnObj = iSerializer.DeserializeObject( retval.ResponseString, Core.Utilities.EncryptionMode.NoEncryption );
                //    if ( returnObj == null )
                //        return retval.StatusCode;

                //    obj.ExpirationDate = retval.Expiration;
                //    obj.AttemptRefreshDate = retval.AttemptToRefresh;

                //    responseObj = obj.Clone( returnObj );
                //}

            }

            return retval.StatusCode;
        }

        /// <summary>
        /// Gets or sets a value indicating whether a queue request will return the modified item.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the modified object is returned; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Order = 6)]
        public bool RequestReturnsObject
        {
            get;
            set;
        }

        private Dictionary<string, string> MergeHeaders(RestfulObject<T> restObject)
        {
            var mergedHeaders = new Dictionary<string, string>();

            if (Device.RequestInjectionHeaders != null)
                mergedHeaders.AddRange(Device.RequestInjectionHeaders);


            if (restObject != null && restObject.PutPostDeleteHeaders != null)
                mergedHeaders.AddRange(restObject.PutPostDeleteHeaders);

            return mergedHeaders;
        }


        #region Queue Serialization Methods

        /// <summary>
        /// Deserializes the queue.
        /// </summary>
        public void DeserializeQueue()
        {
            lock (syncLock)
            {
                //SerializationFormat format = QueueSerializationFormat;
                //if ( format == SerializationFormat.CUSTOM )
                //    format = SerializationFormat.XML;

                // needs to use a different serializer for queue serialization to support RestfulObject<T>
                //ISerializer<RestfulObject<T>> iSerializer = SerializerFactory.Create<RestfulObject<T>>( format );
                List<RestfulObject<T>> queuelist = QueueSerializer.DeserializeListFromFile(QueueFileName);

                if (queuelist == null)
                    return;

                foreach (var item in queuelist)
                    Enqueue(item);
            }
        }

        /// <summary>
        /// Serializes the queue.
        /// </summary>
        public void SerializeQueue()
        {
            lock (syncLock)
            {
                Device.Log.Debug("Serializing queue to file:  type: " + typeof(T).ToString());

                //foreach( RestfulObject<T> item in this.ToList() )
                //	Device.Log.Debug("queue item to serialize " + item.Object.ToString() );

                if (this.Count > 0)
                {
                    // needs to use a different serializer for queue serialization to support RestfulObject<T>
                    //ISerializer<RestfulObject<T>> iSerializer = SerializerFactory.Create<RestfulObject<T>>( QueueSerializationFormat );
                    QueueSerializer.SerializeListToFile(this.ToList(), QueueFileName);
                }
                else
                    Device.File.Delete(QueueFileName);
            }
        }

        private string _queueFileName;
        private string QueueFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_queueFileName))
                {
                    _queueFileName = Device.SessionDataPath.AppendPath("Queue").AppendPath(typeof(T).Name + ".xml");
                    // ensure file's directory exists
                    Device.File.EnsureDirectoryExists(_queueFileName);
                }

                return _queueFileName;
            }
            set
            {
                _queueFileName = value;
            }
        }


        /// <summary>
        /// Removes serialized file for queue and discards all queue contents
        /// </summary>
        public void DiscardQueue()
        {
            lock (syncLock)
            {
                if (Device.File.Exists(QueueFileName))
                    Device.File.Delete(QueueFileName);

                while (this.Count() > 0)
                    this.Dequeue();
            }
        }

        #endregion

    }
}

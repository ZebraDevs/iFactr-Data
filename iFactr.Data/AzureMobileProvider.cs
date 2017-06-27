using MonoCross;
using MonoCross.Utilities;
using System;

namespace iFactr.Data
{
    /// <summary>
    /// This class represents an iFactr data provider for Azure Mobile Services data sources.
    /// </summary>
    /// <typeparam name="T">The generic object type for the provider.</typeparam>
    /// <remarks>
    /// The Provider&lt;T&gt; class provides the base implementation for all data
    /// providers, and implements the base list plus transaction methods, (CRUD).
    /// </remarks>
    public abstract class AzureMobileProvider<T> : Provider<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureMobileProvider&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="objectUri">The object URI.</param>
        /// <param name="listUri">The list URI.</param>
        /// <param name="keyParameters">The key parameters.</param>
        public AzureMobileProvider(string baseUri, string objectUri, string listUri, params string[] keyParameters)
            : base(baseUri, objectUri, listUri, MonoCross.Utilities.Serialization.SerializationFormat.JSON, keyParameters)
        {
            PostToListEndpoint = true;
        }
        /// <summary>
        /// Queues a transaction for processing a change operation on the server.  Requires ProvderMethod.PUT support on the provider.
        /// </summary>
        /// <param name="obj">The Object to be changed.</param>
        public override void Change(T obj)
        {
            if ((ProviderMethods & ProviderMethod.PUT) != ProviderMethod.PUT)
                throw new NotSupportedException("Provider Change method is not supported for " + this.GetType().Name);

            Expire(obj, ExpireMethodType.ExpireOnly);

            switch (Device.NetworkPostMethod)
            {
                case MonoCross.Utilities.Network.NetworkPostMethod.QueuedAsynchronous:
                    queue.Enqueue(new RestfulObject<T>(obj, "PATCH", GetUri(obj)) { PutPostDeleteHeaders = PutPostDeleteHeaders });
                    queue.SerializeQueue();
                    break;
                case MonoCross.Utilities.Network.NetworkPostMethod.ImmediateSynchronous:
                    NetworkResponse NetworkResponse = Utilities.Network.Patch<T>(obj, GetUri(ObjectAbsoluteUri, MapParams(obj)), MergedHeaders, Format, queue.CustomSerializerType);
                    ProcessNetworkResponse(NetworkResponse);
                    break;
            }
        }
    }
}

using iFactr.Data.Utilities.NetworkResource.ResourceStrategy.DirectStream;
using MonoCross.Utilities;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    internal class ResourceStrategyDirectStream : BaseResourceStrategy, IResourceStrategy
    {
        public override ResourceStrategyType Type
        {
            get { return ResourceStrategyType.DirectStream; }
        }

        public override ResourceResponse GetResponse( string uri )
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return GetResponse( uri, args );
        }
        public override ResourceResponse GetResponse( string uri, NetworkResourceArguments args )
        {
            ResourceResponseDirectStream response = new ResourceResponseDirectStream(uri, args);
            return response;
        }
    }
}

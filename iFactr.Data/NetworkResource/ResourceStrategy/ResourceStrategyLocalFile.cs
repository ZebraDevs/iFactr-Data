using iFactr.Data.Utilities.NetworkResource.ResourceStrategy.LocalFile;
using MonoCross.Utilities;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    internal class ResourceStrategyLocalFile : BaseResourceStrategy, IResourceStrategy
    {
        public override ResourceStrategyType Type
        {
            get
            {
                return ResourceStrategyType.LocalFile;
            }
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
            ResourceResponseLocalFile response = new ResourceResponseLocalFile( uri, args );
            return response;
        }
    }
}

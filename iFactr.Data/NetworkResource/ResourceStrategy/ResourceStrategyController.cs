using MonoCross.Utilities;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    internal class ResourceStrategyController
    {
        internal IResourceStrategy ResourceStrategy { get; private set; }

        internal ResourceStrategyController(ResourceStrategyType strategyType)
        {
            ResourceStrategy = ResourceStrategyFactory.Create(strategyType);
        }

        internal ResourceResponse GetResponse( string uri )
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return GetResponse( uri, args );
        }
        internal ResourceResponse GetResponse( string uri, NetworkResourceArguments args )
        {
            ResourceResponse response = ResourceStrategy.GetResponse(uri, args);
            return response;
        }

        internal static void Initialize()
        {
            // To-Do: find a more elegant way of iterating through an enum's values and initializing them.
            //foreach (string item in Enum.GetNames(typeof(ResourceStrategyType)))
            //{
            //    IResourceStrategy resourceStrategy =
            //        ResourceStrategyFactory.Create((ResourceStrategyType)Enum.Parse(typeof(ResourceStrategyType), item));

            //    resourceStrategy.Initialize();
            //}

            IResourceStrategy resourceStrategy = ResourceStrategyFactory.Create(ResourceStrategyType.Cache);
            resourceStrategy.Initialize();

            resourceStrategy = ResourceStrategyFactory.Create(ResourceStrategyType.DirectStream);
            resourceStrategy.Initialize();

            resourceStrategy = ResourceStrategyFactory.Create( ResourceStrategyType.LocalFile );
            resourceStrategy.Initialize();
        }
    }
}

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    internal static class ResourceStrategyFactory
    {
        internal static IResourceStrategy Create()
        {
            IResourceStrategy resourceStrategy = new ResourceStrategyCache();
            return resourceStrategy;
        }

        internal static IResourceStrategy Create (ResourceStrategyType resourceStrategyType)
        {
            IResourceStrategy resourceStrategy = new ResourceStrategyCache();

            switch (resourceStrategyType)
            {
                case ResourceStrategyType.Cache:
                    resourceStrategy = new ResourceStrategyCache();
                    break;
                case ResourceStrategyType.DirectStream:
                    resourceStrategy = new ResourceStrategyDirectStream();
                    break;
                case ResourceStrategyType.LocalFile:
                    resourceStrategy = new ResourceStrategyLocalFile();
                    break;
                default:
                    // throw exception ?
                    resourceStrategy = null;
                    break;
            }

            return resourceStrategy;
        }
    }
}

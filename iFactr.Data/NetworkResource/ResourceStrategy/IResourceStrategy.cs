namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy
{
    internal interface IResourceStrategy
    {
        void Initialize();
        ResourceStrategyType Type { get; }
        ResourceResponse GetResponse( string uri );
        ResourceResponse GetResponse( string uri, NetworkResourceArguments args );
    }
}

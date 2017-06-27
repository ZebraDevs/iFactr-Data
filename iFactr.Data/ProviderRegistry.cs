using System;
using System.Collections.Generic;

namespace iFactr.Data
{
    /// <summary>
    /// A registry for data providers, indexed on Type.
    /// </summary>
    public class ProviderRegistry : Dictionary<Type, IDataProvider>
    {
        /// <summary>
        /// Registers the specified data provider.
        /// </summary>
        /// <param name="provider">The provider to be registered.</param>
        public void Register(IDataProvider provider)
        {
            Add(provider.ProviderType, provider);
        }
        /// <summary>
        /// Gets the provider for the Type specified.
        /// </summary>
        /// <param name="providerType">Type of the data provider to get.</param>
        /// <returns></returns>
        public IDataProvider GetProvider(Type providerType)
        {
            IDataProvider retval = this[providerType];
            if (retval == null)
            {
                foreach (IDataProvider provider in this.Values)
                {
                    if (provider is ICompositeDataProvider)
                    {
                        retval = ((ICompositeDataProvider)provider).Providers.GetProvider(providerType);
                        if (retval != null)
                            return retval;
                    }
                }
            }
            return retval;
        }
    }
}
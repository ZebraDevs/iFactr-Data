using iFactr.Data.Utilities.NetworkResource.ResourceStrategy;
using iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache;
using MonoCross.Utilities;
using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace iFactr.Data.Utilities.NetworkResource
{
    /// <summary>
    /// Contains Utility XDocument extension methods.
    /// </summary>
    public static class XDocumentExtensions
    {
        /// <summary>
        /// extension method to load url into Xdocument from Network Resource Library cache (refreshing if needed)
        /// </summary>
        /// <param name="doc">Xdocument</param>
        /// <param name="url">URL for resource being requested</param>
        /// <param name="cachePeriod">TimeSpan to retain cached resource, if not specified on server</param>
        /// <returns></returns>
        public static XDocument Load(this XDocument doc, string url, TimeSpan cachePeriod)
        {
            return doc.Load(url, cachePeriod, false);
        }

        /// <summary>
        /// extension method to load url into Xdocument from Network Resource Library cache (refreshing if needed)
        /// </summary>
        /// <param name="doc">Xdocument</param>
        /// <param name="url">URL for resource being requested</param>
        /// <param name="cachePeriod">TimeSpan to retain cached resource, if not specified on server</param>
        /// <param name="prefetch">Indicates whether resource should be maintained by prefetcher</param>
        /// <returns></returns>
        public static XDocument Load(this XDocument doc, string url, TimeSpan cachePeriod, bool prefetch)
        {
            ResourceResponseCache response = null;
            try
            {
                ResourceRequest request = NetworkResourceLibrary.Instance.GetResourceRequest(url, ResourceStrategyType.Cache);
                response = (ResourceResponseCache)request.GetResponse(60000);

                if (response.CacheIndexItem.Expiration == DateTime.MinValue.ToUniversalTime())
                    response.CacheIndexItem.Expiration = DateTime.UtcNow.Add(cachePeriod);

                response.CacheIndexItem.PreFetch = prefetch;

                return XDocument.Load(new StringReader(Device.File.ReadString(response.GetResponseFileName(), EncryptionMode.NoEncryption)));
            }
            catch (XmlException)
            {
                if (response != null)
                    response.CacheIndex.RemoveCurrentCache(response.CacheIndexItem);
                return null;
            }
            catch (WebException)
            {
                return null;
            }

        }

        /// <summary>
        /// extension method to load url into Xdocument from Network Resource Library cache (refreshing if needed)
        /// </summary>
        /// <param name="element">XElement</param>
        /// <param name="url">URL for resource being requested</param>
        /// <param name="cachePeriod">TimeSpan to retain cached resource, if not specified on server</param>
        /// <returns></returns>
        public static XElement Load(this XElement element, string url, TimeSpan cachePeriod)
        {
            return element.Load(url, cachePeriod, false);
        }

        /// <summary>
        /// extension method to load url into Xdocument from Network Resource Library cache (refreshing if needed)
        /// </summary>
        /// <param name="element">XElement</param>
        /// <param name="url">URL for resource being requested</param>
        /// <param name="cachePeriod">TimeSpan to retain cached resource, if not specified on server</param>
        /// <param name="prefetch">Indicates whether resource should be maintained by prefetcher</param>
        /// <returns></returns>
        public static XElement Load(this XElement element, string url, TimeSpan cachePeriod, bool prefetch)
        {
            ResourceResponseCache response = null;
            try
            {
                ResourceRequest request = NetworkResourceLibrary.Instance.GetResourceRequest(url, ResourceStrategyType.Cache);
                response = (ResourceResponseCache)request.GetResponse(60000);

                if (response.CacheIndexItem.Expiration == DateTime.MinValue.ToUniversalTime())
                    response.CacheIndexItem.Expiration = DateTime.UtcNow.Add(cachePeriod);

                response.CacheIndexItem.PreFetch = prefetch;

                return XElement.Load(new StringReader(Device.File.ReadString(response.GetResponseFileName(), EncryptionMode.NoEncryption)));
            }
            catch (XmlException)
            {
                if (response != null)
                    response.CacheIndex.RemoveCurrentCache(response.CacheIndexItem);
                return null;
            }
            catch (WebException)
            {
                return null;
            }
        }
    }
}

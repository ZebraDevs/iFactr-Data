
using iFactr.Data.Utilities.NetworkResource;
using iFactr.Data.Utilities.NetworkResource.ResourceStrategy;
using iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache;
using MonoCross;
using MonoCross.Utilities;
using MonoCross.Utilities.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Xml;
using System.Xml.Linq;

namespace iFactr.Data.Utilities
{
    /// <summary>
    /// Represents the HTTP Network Utility.
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Loads the XML.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public static XDocument LoadXML(string url)
        {
            Uri address = new Uri(url);
            HttpRequestCachePolicy policy = new HttpRequestCachePolicy(HttpRequestCacheLevel.CacheOnly);
            HttpWebRequest.DefaultCachePolicy = policy;

            HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
            request.CachePolicy = policy;
            request.Method = "GET";
            request.ContentType = "application/xml; charset=utf-8";
            request.KeepAlive = false;
            request.UserAgent = "Mozilla/5.0";

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    XDocument loaded = XDocument.Parse(streamReader.ReadToEnd());
                    return loaded;
                }
            }
        }

        /// <summary>
        /// method to initiate Prefetch processing based on provided manifest uri.
        /// </summary>
        /// <param name="manifestUri"></param>
        public static void InitiatePrefetch(string manifestUri)
        {
            // if cache is not supported then don't bother prefetching.
            if (Device.NetworkGetMethod == MonoCross.Utilities.Network.NetworkGetMethod.NoCache)
            {
                Device.Log.Debug("Cache is not supported, prefetch request ignored.");
                return;
            }

            CacheManifest cacheManifest = CacheManifest.CreateFromUri(manifestUri);
            CacheIndexMap.Add(cacheManifest);
            Device.Thread.QueueIdle(CacheIndexMap.PreFetchIndexes);
            Device.Thread.QueueIdle(CacheIndexMap.CleanIndexes);
        }

        /// <summary>
        /// method to initiate Clean index processing
        /// </summary>
        public static void InitiateCleanIndex()
        {
            // if cache is not supported then don't bother prefetching.
            if (Device.NetworkGetMethod == MonoCross.Utilities.Network.NetworkGetMethod.NoCache)
            {
                Device.Log.Debug("Cache is not supported, clean index request ignored.");
                return;
            }
            Device.Thread.QueueIdle(CacheIndexMap.CleanIndexes);
        }
        #region Get<T> Methods

        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <returns></returns>
        public static T Get<T>(string uri)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get<T>(uri, args, ResourceStrategyType.DirectStream, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The arguments for the request.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, NetworkResourceArguments args)
        {
            return Get<T>(uri, args, ResourceStrategyType.DirectStream, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The arguments for the request.</param>
        /// <param name="type">The resource strategy type.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, NetworkResourceArguments args, ResourceStrategyType type)
        {
            return Get<T>(uri, args, type, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The arguments for the request.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, NetworkResourceArguments args, SerializationFormat format)
        {
            return Get<T>(uri, args, ResourceStrategyType.DirectStream, format);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="type">The resource strategy type.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, ResourceStrategyType type)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get<T>(uri, args, type, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, SerializationFormat format)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get<T>(uri, args, ResourceStrategyType.DirectStream, format);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="type">The resource strategy type.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, ResourceStrategyType type, SerializationFormat format)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get<T>(uri, args, type, format);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The arguments for the request.</param>
        /// <param name="type">The resource strategy type.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, NetworkResourceArguments args, ResourceStrategyType type, SerializationFormat format)
        {
            return Get<T>(uri, args, type, format, null);
        }
        /// <summary>
        /// Gets a resource using the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The arguments for the request.</param>
        /// <param name="type">The resource strategy type.</param>
        /// <param name="format">The serialization format.</param>
        /// <param name="customSerializerType">Type of the custom serializer.</param>
        /// <returns></returns>
        public static T Get<T>(string uri, NetworkResourceArguments args, ResourceStrategyType type, SerializationFormat format, Type customSerializerType)
        {
            if (string.IsNullOrEmpty(uri))
                return default(T);

            ResourceRequest request = NetworkResourceLibrary.Instance.GetResourceRequest(uri, type, args);
            ResourceResponse response = request.GetResponse(args.TimeoutMilliseconds);

            ISerializer<T> iSerializer = SerializerFactory.Create<T>(format, customSerializerType);
            T obj = iSerializer.DeserializeObject(response.GetResponseBytes());

            return obj;

        }

        #endregion

        #region Get Methods

        /// <summary>
        /// Gets the resource.
        /// </summary>
        /// <param name="uri">The URI of the resource to get.</param>
        /// <returns></returns>
        public static string Get(string uri)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get(uri, args, ResourceStrategyType.DirectStream, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets the specified resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The network resource arguments for the request.</param>
        /// <returns></returns>
        public static string Get(string uri, NetworkResourceArguments args)
        {
            return Get(uri, args, ResourceStrategyType.DirectStream, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets the specified resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The network resource arguments for the request.</param>
        /// <param name="type">The strategy type for the resource.</param>
        /// <returns></returns>
        public static string Get(string uri, NetworkResourceArguments args, ResourceStrategyType type)
        {
            return Get(uri, args, type, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets the specified resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The network resource arguments for the request.</param>
        /// <param name="format">The serialization format for the request.</param>
        /// <returns></returns>
        public static string Get(string uri, NetworkResourceArguments args, SerializationFormat format)
        {
            return Get(uri, args, ResourceStrategyType.DirectStream, format);
        }
        /// <summary>
        /// Gets the specified resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="type">The strategy type for the resource.</param>
        /// <returns></returns>
        public static string Get(string uri, ResourceStrategyType type)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get(uri, args, type, SerializationFormat.XML);
        }
        /// <summary>
        /// Gets the specified resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="format">The serialization format for the request.</param>
        /// <returns></returns>
        public static string Get(string uri, SerializationFormat format)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get(uri, args, ResourceStrategyType.DirectStream, format);
        }
        /// <summary>
        /// Gets the specified resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="type">The strategy type for the resource.</param>
        /// <param name="format">The serialization format for the request.</param>
        /// <returns></returns>
        public static string Get(string uri, ResourceStrategyType type, SerializationFormat format)
        {
            NetworkResourceArguments args = new NetworkResourceArguments()
            {
                Headers = Device.RequestInjectionHeaders,
                CacheStaleMethod = CacheStaleMethod.Deferred
            };
            return Get(uri, args, type, format);
        }
        /// <summary>
        /// Gets the specified resource.
        /// </summary>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="args">The network resource arguments for the request.</param>
        /// <param name="type">The strategy type for the resource.</param>
        /// <param name="format">The serialization format for the request.</param>
        /// <returns></returns>
        public static string Get(string uri, NetworkResourceArguments args, ResourceStrategyType type, SerializationFormat format)
        {
            if (string.IsNullOrEmpty(uri))
                return string.Empty;

            ResourceRequest request = NetworkResourceLibrary.Instance.GetResourceRequest(uri, type, args);
            ResourceResponse response = request.GetResponse(args.TimeoutMilliseconds);

            return response.GetResponseString();
        }

        #endregion

        #region Patch<T> Methods

        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <returns></returns>
        public static NetworkResponse Patch<T>(T obj, string uri)
        {
            return Patch<T>(obj, uri, Device.RequestInjectionHeaders, SerializationFormat.XML);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <returns></returns>
        public static NetworkResponse Patch<T>(T obj, string uri, IDictionary<string, string> headers)
        {
            return Patch<T>(obj, uri, headers, SerializationFormat.XML);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static NetworkResponse Patch<T>(T obj, string uri, SerializationFormat format)
        {
            return Patch<T>(obj, uri, Device.RequestInjectionHeaders, format);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static NetworkResponse Patch<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format)
        {
            return Patch<T>(obj, uri, headers, format, null);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format.</param>
        /// <param name="customSerializerType">Type of the custom serializer.</param>
        /// <returns></returns>
        public static NetworkResponse Patch<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format, Type customSerializerType)
        {
            if (string.IsNullOrEmpty(uri))
                return null;

            ISerializer<T> iSerializer = SerializerFactory.Create<T>(format, customSerializerType);
            byte[] bytes = iSerializer.SerializeObjectToBytes(obj, EncryptionMode.NoEncryption);

            NetworkResponse NetworkResponse = Device.Network.Poster.PostBytes(uri, bytes, iSerializer.ContentType, "PATCH", headers);

            return NetworkResponse;
        }

        #endregion

        #region Post<T> Methods

        /// <summary>
        /// Posts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to post.</param>
        /// <param name="uri">The resource URI.</param>
        /// <returns></returns>
        public static NetworkResponse Post<T>(T obj, string uri)
        {
            return Post<T>(obj, uri, Device.RequestInjectionHeaders, SerializationFormat.XML);
        }
        /// <summary>
        /// Posts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to post.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <returns></returns>
        public static NetworkResponse Post<T>(T obj, string uri, IDictionary<string, string> headers)
        {
            return Post<T>(obj, uri, headers, SerializationFormat.XML);
        }
        /// <summary>
        /// Posts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to post.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static NetworkResponse Post<T>(T obj, string uri, SerializationFormat format)
        {
            return Post<T>(obj, uri, Device.RequestInjectionHeaders, format);
        }
        /// <summary>
        /// Posts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to post.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static NetworkResponse Post<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format)
        {
            return Post<T>(obj, uri, headers, format, null);
        }
        /// <summary>
        /// Posts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object to post.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format.</param>
        /// <param name="customSerializerType">Type of the custom serializer.</param>
        /// <returns></returns>
        public static NetworkResponse Post<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format, Type customSerializerType)
        {
            if (string.IsNullOrEmpty(uri))
                return null;

            ISerializer<T> iSerializer = SerializerFactory.Create<T>(format, customSerializerType);
            byte[] bytes = iSerializer.SerializeObjectToBytes(obj, EncryptionMode.NoEncryption);

            NetworkResponse NetworkResponse = Device.Network.Poster.PostBytes(uri, bytes, iSerializer.ContentType, "POST", headers);

            return NetworkResponse;
        }

        #endregion

        #region Put<T> Methods

        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <returns></returns>
        public static NetworkResponse Put<T>(T obj, string uri)
        {
            return Put<T>(obj, uri, Device.RequestInjectionHeaders, SerializationFormat.XML);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <returns></returns>
        public static NetworkResponse Put<T>(T obj, string uri, IDictionary<string, string> headers)
        {
            return Put<T>(obj, uri, headers, SerializationFormat.XML);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static NetworkResponse Put<T>(T obj, string uri, SerializationFormat format)
        {
            return Put<T>(obj, uri, Device.RequestInjectionHeaders, format);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format.</param>
        /// <returns></returns>
        public static NetworkResponse Put<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format)
        {
            return Put<T>(obj, uri, headers, format, null);
        }
        /// <summary>
        /// Puts a resource to the URI specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The objet to put.</param>
        /// <param name="uri">The resource URI.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format.</param>
        /// <param name="customSerializerType">Type of the custom serializer.</param>
        /// <returns></returns>
        public static NetworkResponse Put<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format, Type customSerializerType)
        {
            if (string.IsNullOrEmpty(uri))
                return null;

            ISerializer<T> iSerializer = SerializerFactory.Create<T>(format, customSerializerType);
            byte[] bytes = iSerializer.SerializeObjectToBytes(obj, EncryptionMode.NoEncryption);

            NetworkResponse NetworkResponse = Device.Network.Poster.PostBytes(uri, bytes, iSerializer.ContentType, "PUT", headers);

            return NetworkResponse;
        }

        #endregion

        #region Delete<T> Methods

        /// <summary>
        /// Deletes the specified resource.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The resource to delete in the request.</param>
        /// <param name="uri">The URI of the resource.</param>
        /// <returns></returns>
        public static NetworkResponse Delete<T>(T obj, string uri)
        {
            return Delete<T>(obj, uri, Device.RequestInjectionHeaders, SerializationFormat.XML);
        }
        /// <summary>
        /// Deletes the specified resource.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The resource to delete in the request.</param>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <returns></returns>
        public static NetworkResponse Delete<T>(T obj, string uri, Dictionary<string, string> headers)
        {
            return Delete<T>(obj, uri, headers, SerializationFormat.XML);
        }
        /// <summary>
        /// Deletes the specified resource.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The resource to be deleted on the request.</param>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="format">The serialization format for the request.</param>
        /// <returns></returns>
        public static NetworkResponse Delete<T>(T obj, string uri, SerializationFormat format)
        {
            return Delete<T>(obj, uri, Device.RequestInjectionHeaders, format);
        }
        /// <summary>
        /// Deletes the specified resource.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The resource to be deleted on the request.</param>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format for the request.</param>
        /// <returns></returns>
        public static NetworkResponse Delete<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format)
        {
            return Delete<T>(obj, uri, headers, format, null);
        }
        /// <summary>
        /// Deletes the specified resource.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The resource to be deleted on the request.</param>
        /// <param name="uri">The URI of the resource.</param>
        /// <param name="headers">The headers for the request.</param>
        /// <param name="format">The serialization format for the request.</param>
        /// <param name="customSerializerType">Type of the custom serializer.</param>
        /// <returns></returns>
        public static NetworkResponse Delete<T>(T obj, string uri, IDictionary<string, string> headers, SerializationFormat format, Type customSerializerType)
        {
            if (string.IsNullOrEmpty(uri))
                return null;

            ISerializer<T> iSerializer = SerializerFactory.Create<T>(format, customSerializerType);
            byte[] bytes = iSerializer.SerializeObjectToBytes(obj, EncryptionMode.NoEncryption);

            NetworkResponse NetworkResponse = Device.Network.Poster.PostBytes(uri, bytes, iSerializer.ContentType, "DELETE", headers);

            return NetworkResponse;
        }

        #endregion
    }
}

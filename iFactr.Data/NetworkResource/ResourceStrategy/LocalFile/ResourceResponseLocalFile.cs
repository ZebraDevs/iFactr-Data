using MonoCross.Utilities;
using System;

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.LocalFile
{
    /// <summary>
    /// Represents a local resoures response file.
    /// </summary>
    public class ResourceResponseLocalFile : ResourceResponse
    {
        /// <summary>
        /// ctor for File resource response class
        /// </summary>
        /// <param name="uri">Full Uri of resource request</param>
        /// <param name="args">The args.</param>
        internal ResourceResponseLocalFile( string uri, NetworkResourceArguments args )
        {
            Uri = uri;
            NetworkResourceArguments = args;

            // default NetworkResponse to a generic response.
            ReturnStatus = new MonoCross.NetworkResponse()
            {
                Message = "NetworkResponse not relevant for local file strategy.",
                ResponseString = string.Empty,
                StatusCode = System.Net.HttpStatusCode.OK
            };
        }

        /// <summary>
        /// returns Uri for response object. Direct stream doesn't have a natural file name.
        /// </summary>
        /// <returns></returns>
        public override string GetResponseFileName()
        {
            return Uri;
        }

        //public override Stream GetResponseStream()
        //{
        //    return Device.File.Read(Uri);
        //}

        /// <summary>
        /// Gets the response string.
        /// </summary>
        /// <returns></returns>
        public override string GetResponseString()
        {
            try
            {
                string cachedFile = GetResponseFileName();
                if ( string.IsNullOrEmpty( cachedFile ) || !Device.File.Exists( cachedFile ) )
                    return string.Empty;
                return Device.File.ReadString( cachedFile );
            }
            catch ( Exception e )
            {
                // Let the user know what went wrong.
                Device.Log.Error("The file could not be read:");
                Device.Log.Error(e.Message);
            }

            return null;
        }

        /// <summary>
        /// Gets the response byte array.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetResponseBytes()
        {
            try
            {
                string cachedFile = GetResponseFileName();
                if (string.IsNullOrEmpty(cachedFile) || !Device.File.Exists(cachedFile))
                    return new byte[0];
                return Device.File.Read(cachedFile);
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Device.Log.Error("The file could not be read:");
                Device.Log.Error(e.Message);
                throw;
            }

        }
    }
}

namespace iFactr.Data.Utilities.NetworkResource.ResourceStrategy.Cache
{
    /// <summary>
    /// Represents cache encryption information.
    /// </summary>
    public abstract class CacheEncryption
    {
        //To-Do: implement encryption algorithms allowing for target-specific overrides
        /// <summary>
        /// Encrypts the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public virtual string Encrypt(string data)
        {
            return data;
        }

        /// <summary>
        /// Decrypts the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public virtual string Decrypt(string data)
        {
            return data;
        }
    }
}

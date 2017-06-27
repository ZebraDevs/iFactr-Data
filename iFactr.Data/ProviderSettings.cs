namespace iFactr.Data
{
    /// <summary>
    /// Encryption settings for data providers.
    /// </summary>
    public static class ProviderSettings
    {
        /// <summary>
        /// Gets or sets the encryption key.
        /// </summary>
        /// <value>The encryption key.</value>
        public static string EncryptionKey
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the encryption salt.
        /// </summary>
        /// <value>The encryption salt.</value>
        public static byte[] EncryptionSalt
        {
            get;
            set;
        }
    }
}

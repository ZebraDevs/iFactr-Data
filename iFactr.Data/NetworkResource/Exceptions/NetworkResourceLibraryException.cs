using System;

namespace iFactr.Data.Utilities.NetworkResource
{
    /// <summary>
    /// Represents a network resource library exception.
    /// </summary>
#if !SILVERLIGHT && !NETCF && !NETFX_CORE && !PCL
    [Serializable]
#endif
    public class NetworkResourceLibraryException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkResourceLibraryException"/> class.
        /// </summary>
        public NetworkResourceLibraryException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkResourceLibraryException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public NetworkResourceLibraryException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkResourceLibraryException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public NetworkResourceLibraryException(string message, Exception inner) : base(message, inner) { }
#if !PCL && !NETCF
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkResourceLibraryException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="info"/> parameter is null.
        /// </exception>
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">
        /// The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0).
        /// </exception>
        protected NetworkResourceLibraryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
#endif
    }
}

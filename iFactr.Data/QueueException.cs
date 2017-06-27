using System;

namespace iFactr.Data
{
    /// <summary>
    /// Represents a RESTful queue exception.
    /// </summary>
#if !SILVERLIGHT && !NETFX_CORE && !PCL
    [Serializable]
#endif
    public class QueueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueException"/> class.
        /// </summary>
        public QueueException()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public QueueException(string message)
            : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public QueueException(string message, Exception inner)
            : base(message, inner)
        {
        }
        //public QueueException(
        //  System.Runtime.Serialization.SerializationInfo info,
        //  System.Runtime.Serialization.StreamingContext context )
        //    : base( info, context )
        //{
        //}

    }
}

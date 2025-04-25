using System;
using System.Runtime.Serialization;

namespace NavigationPlatform.Application.Common.Exceptions
{
    /// <summary>
    /// Exception for resources that once existed but have been removed, matching HTTP 410 Gone
    /// </summary>
    [Serializable]
    public class GoneException : Exception
    {
        public GoneException() : base("The requested resource is no longer available")
        {
        }

        public GoneException(string message) : base(message)
        {
        }

        public GoneException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GoneException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
} 
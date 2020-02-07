using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Dfc.Session.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class InvalidSessionException : Exception
    {
        public InvalidSessionException() : base()
        {
        }

        public InvalidSessionException(string message) : base(message)
        {
        }

        public InvalidSessionException(string message, Exception exception) : base(message, exception)
        {
        }

        protected InvalidSessionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
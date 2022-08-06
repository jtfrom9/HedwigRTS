#nullable enable

using System;
using System.Runtime.Serialization;

namespace Hedwig.RTSCore
{
    public class InvalidConditionException : Exception
    {
        public InvalidConditionException() : base() { }
        public InvalidConditionException(string message) : base(message) { }
        public InvalidConditionException(string message, Exception innerException)
        : base(message, innerException)
        { }
        protected InvalidConditionException(SerializationInfo info, StreamingContext context)
                : base(info, context)
        {
        }
    }
}
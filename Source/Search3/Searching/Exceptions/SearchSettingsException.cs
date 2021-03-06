using System;
using System.Runtime.Serialization;

namespace Search3.Searching.Exceptions
{
    public abstract class SearchSettingsException : Exception
    {
        protected SearchSettingsException()
        {
        }

        protected SearchSettingsException(Exception inner)
            : this(null, inner)
        {

        }

        protected SearchSettingsException(string message, Exception inner = null)
            : base(message, inner)
        {

        }

        protected SearchSettingsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
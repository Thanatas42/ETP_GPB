using System;
using System.Runtime.Serialization;

namespace ETPlibrary.ETPGPB.Common.Exceptions
{
    /// <summary>
    /// Ошибка загрузки коннектора к системе обмена.
    /// </summary>
    [Serializable]
    public class ExchangeSystemConnectorResolveException : Exception
    {
        public ExchangeSystemConnectorResolveException()
        {
        }

        public ExchangeSystemConnectorResolveException(string message)
            : base(message)
        {
        }

        public ExchangeSystemConnectorResolveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ExchangeSystemConnectorResolveException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

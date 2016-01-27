using System;
using System.Runtime.Serialization;

namespace PricingManager.DataContracts
{
    public class TicketNotValidException : Exception
    {
        public TicketNotValidException()
        {
        }

        public TicketNotValidException(string message) : base(message)
        {
        }

        public TicketNotValidException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TicketNotValidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
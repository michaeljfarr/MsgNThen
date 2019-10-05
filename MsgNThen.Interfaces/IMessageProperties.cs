using System.Collections.Generic;

namespace MsgNThen.Interfaces
{
    public class MessageProperties
    {
        /// <summary>Application correlation identifier.</summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// Message header field table. Is of type <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        public IDictionary<string, object> Headers { get; set; }

        /// <summary>Application message Id.</summary>
        public string MessageId { get; set; }
    }
}
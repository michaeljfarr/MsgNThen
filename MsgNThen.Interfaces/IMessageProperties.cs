using System.Collections.Generic;

namespace MsgNThen.Interfaces
{
    public interface IMessageProperties
    {
        /// <summary>Application correlation identifier.</summary>
        string CorrelationId { get; set; }
        /// <summary>
        /// Message header field table. Is of type <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        IDictionary<string, object> Headers { get; set; }

        /// <summary>Application message Id.</summary>
        string MessageId { get; set; }
    }
}
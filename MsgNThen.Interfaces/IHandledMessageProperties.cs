using System;
using System.Collections.Generic;

namespace MsgNThen.Interfaces
{
    public interface IHandledMessageProperties 
    {
        /// <summary>Application Id.</summary>
        string AppId { get; }

        /// <summary>MIME content encoding.</summary>
        string ContentEncoding { get; }

        /// <summary>MIME content type.</summary>
        string ContentType { get; }

        /// <summary>Application correlation identifier.</summary>
        string CorrelationId { get;  }

        /// <summary>Non-persistent (1) or persistent (2).</summary>
        byte DeliveryMode { get;  }

        /// <summary>Message expiration specification.</summary>
        string Expiration { get;  }

        /// <summary>
        /// Message header field table.
        /// </summary>
        IDictionary<string, object> Headers { get;  }

        /// <summary>Application message Id.</summary>
        string MessageId { get;  }

        /// <summary>Message priority, 0 to 9.</summary>
        byte Priority { get;  }

        /// <summary>Destination to reply to.</summary>
        string ReplyTo { get;  }

        /// <summary>Message timestamp.</summary>
        DateTime Timestamp { get; }

        /// <summary>Message type name.</summary>
        string Type { get; }

        /// <summary>User Id.</summary>
        string UserId { get; }
    }
}
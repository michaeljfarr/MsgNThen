using System;
using System.Collections.Generic;
using System.Text;

namespace MsgNThen.Interfaces
{
    public static class HeaderConstants
    {
        public const string MessageId = "MessageId";
        public const string MessageGroupId = "MessageGroupId";
        public const string AppId = "AppId";
        public const string ReplyTo = "ReplyTo";
        public const string CorrelationId = "CorrelationId";
        public const string UserId = "UserId";
    }

    public static class QueryConstants
    {
        public const string S3CannedACL = "S3CannedACL";
    }
}

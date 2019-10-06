using System;
using System.Collections.Generic;
using MsgNThen.Interfaces;

namespace MsgNThen.Redis.NThen
{
    public interface IMessageGroupHandler
    {
        void StartMessageGroup(Guid groupId);
        void SetMessageGroupCount(Guid groupId, int messageCount, SimpleMessage andThen);
        void CompleteMessageGroupTransmission(Guid groupId);
        void MessagesPrepared(Guid groupId, IEnumerable<Guid> messages);
        void MessageHandled(Guid groupId, Guid messageId);
    }
}
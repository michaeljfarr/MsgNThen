using System;
using System.Collections.Generic;
using System.Text;
using MsgNThen.Interfaces;
using Xunit;

namespace MsgNThen.Redis.Tests
{
    public class MessageGroupHandlerTests
    {
        [Fact]
        public void Test()
        {
            IMessageGroupHandler messageGroupHandler = null;
            var groupId = Guid.NewGuid();
            var messageIds = new[] {Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()};
            messageGroupHandler.StartMessageGroup(groupId);
            messageGroupHandler.MessagesPrepared(groupId, messageIds);
            var msg = new SimpleMessage();
            messageGroupHandler.SetMessageGroupCount(groupId, messageIds.Length, msg);
            messageGroupHandler.CompleteMessageGroupTransmission(groupId);
            foreach (var messageId in messageIds)
            {
                messageGroupHandler.MessageHandled(groupId, messageId);
                messageGroupHandler.MessageHandled(groupId, messageId);
            }
        }
        
    }
}

using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Bandwidth_SMS_Client.Models;

namespace Bandwidth_SMS_Client.Events
{
    public class ConversationEvent : PubSubEvent<ConversationEventPayload>
    {
    }

    public class ConversationEventPayload
    {
        public enum ConversationEventType
        {
            Created, Deleted
        }
        public ConversationEventType EventType { get; set; }
        public Conversation ConversationItem { get; set; }
    }
}

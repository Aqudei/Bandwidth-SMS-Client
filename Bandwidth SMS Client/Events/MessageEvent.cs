using System;
using System.Collections.Generic;
using System.Text;
using Bandwidth_SMS_Client.Models;
using Prism.Events;

namespace Bandwidth_SMS_Client.Events
{
    public class MessageEvent : PubSubEvent<MessageEventPayload>
    {
       
    }

    public class MessageEventPayload
    {
        public enum MessageEventType
        {
            Created, Deleted
        }

        public MessageEventType EventType { get; set; }
        public MessageItem MessageItem { get; set; }
    }
}

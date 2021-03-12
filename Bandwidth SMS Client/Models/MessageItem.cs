using System;
using System.Collections.Generic;
using System.Text;

namespace Bandwidth_SMS_Client.Models
{
    public class MessageItem
    {
        public string Body { get; set; }
        public string MessageType { get; set; }
        public string MFrom { get; set; }
        public string To { get; set; }
        public string Status { get; set; }
        public DateTime MessageDate { get; set; }
        public string MessageBwid { get; set; }
        public string GroupingPhone => MessageType == "OUTGOING" ? To : MFrom;
    }
}

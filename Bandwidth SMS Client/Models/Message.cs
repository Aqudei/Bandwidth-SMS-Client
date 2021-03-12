using System;

namespace Bandwidth_SMS_Client.Models
{
    class Message
    {
        public string Body { get; set; }
        public bool IsNew { get; set; }
        public DateTime MessageDate { get; set; }
        public MessageThread User { get; set; }
        public int Id { get; set; }
    }
}

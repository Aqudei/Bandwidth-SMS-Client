using System.Collections.Generic;

namespace Bandwidth_SMS_Client.Models
{
    public class MessageThread
    {
        public string Fullname { get; set; }
        public int Id { get; set; }
        public string Recipient { get; set; }
        public IEnumerable<MessageItem> MessageItems { get; set; }
        public string Avatar { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Bandwidth_SMS_Client.Models;
using Prism.Events;

namespace Bandwidth_SMS_Client.Events
{
    public class ContactUpdated : PubSubEvent<ContactUpdatedPayload>
    {
    }

    public class ContactUpdatedPayload
    {
        public enum UpdateTypes
        {
            Created, Updated, Removed
        }


        public UpdateTypes UpdateType { get; set; }
        public Contact Contact { get; set; }
    }
}

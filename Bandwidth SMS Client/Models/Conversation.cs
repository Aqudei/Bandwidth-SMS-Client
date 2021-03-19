using System;
using System.Collections.Generic;
using System.Text;

namespace Bandwidth_SMS_Client.Models
{
    public class Conversation : IEquatable<Conversation>
    {
        public string Avatar { get; set; }
        public string PhoneNumber { get; set; }
        public string OwnerPhone { get; set; }
        public int Id { get; set; }

        public bool Equals(Conversation other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Conversation) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}

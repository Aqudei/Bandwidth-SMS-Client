using System;
using System.Collections.Generic;
using System.Text;
using Prism.Mvvm;

namespace Bandwidth_SMS_Client.Models
{
    public class MessageItem : BindableBase, IEquatable<MessageItem>
    {
        public int Id { get; set; }
        private bool _isNew = false;
        public string Body { get; set; }
        public string Message_Type { get; set; }
        public string MFrom { get; set; }
        public string To { get; set; }
        public string Status { get; set; }
        public DateTime? Message_Date { get; set; }
        public string Message_Bwid { get; set; }
        public string GroupingPhone => Message_Type == "OUTGOING" ? To : MFrom;

        public bool IsNew
        {
            get => _isNew;
            set => SetProperty(ref _isNew, value);
        }

        public bool Equals(MessageItem other)
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
            return Equals((MessageItem)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}

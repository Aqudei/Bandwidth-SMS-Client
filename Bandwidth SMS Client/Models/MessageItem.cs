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
        private string _name;
        public string Body { get; set; }
        public string MessageType { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Status { get; set; }
        public DateTime? MessageDate { get; set; }
        public string MessageBWID { get; set; }
        public string GroupingPhone => MessageType == "OUTGOING" ? To : From;
        public string DisplayName
        {
            get
            {
                string displayName;

                if (MessageType == "OUTGOING")
                {
                    displayName = "Me";
                }
                else
                {
                    displayName = Name;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = From;
                    }
                }

                return displayName;
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                SetProperty(ref _name, value);
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

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

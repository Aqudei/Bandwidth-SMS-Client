using System;
using Prism.Mvvm;

namespace Bandwidth_SMS_Client.Models
{
    public class Conversation : BindableBase, IEquatable<Conversation>
    {
        private string _avatar;
        private DateTime _updatedAt;
        private bool _hasNew;

        public string Avatar
        {
            get => _avatar;
            set => SetProperty(ref _avatar, value);
        }

        public bool HasNew
        {
            get => _hasNew;
            set => SetProperty(ref _hasNew, value);
        }

        public string PhoneNumber { get; set; }
        public string OwnerPhone { get; set; }
        public int Id { get; set; }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

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
            if (obj.GetType() != GetType()) return false;
            return Equals((Conversation)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
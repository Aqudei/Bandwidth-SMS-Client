using System;
using System.Diagnostics;
using PhoneNumbers;
using Prism.Mvvm;

namespace Bandwidth_SMS_Client.Models
{
    public class Conversation : BindableBase, IEquatable<Conversation>
    {
        private string _avatar;
        private DateTime _updatedAt;
        private bool _hasNew;
        private string _name;
        private int _messageCount;
        public string PhoneNumber { get; set; }
        public string OwnerPhone { get; set; }
        public int Id { get; set; }
        public string DisplayName => string.IsNullOrEmpty(Name) ? "UNKNOWN" : Name;
        public Contact Contact { get; set; }

        public int MessageCount
        {
            get => _messageCount;
            set => SetProperty(ref _messageCount, value);
        }

        public string FormattedPhone
        {
            get
            {
                try
                {
                    var phoneNumberUtil = PhoneNumberUtil.GetInstance();
                    var parsed = phoneNumberUtil.Parse(PhoneNumber, "US");
                    return phoneNumberUtil.Format(parsed, PhoneNumberFormat.INTERNATIONAL).Trim("+".ToCharArray()).Replace(" ", "-");
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{e} | {e.Message}");
                    return PhoneNumber;
                }
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
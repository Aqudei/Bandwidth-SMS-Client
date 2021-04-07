using System;
using System.Collections.Generic;
using System.Text;
using Prism.Mvvm;

namespace Bandwidth_SMS_Client.Models
{
    public class Contact : BindableBase, IEquatable<Contact>
    {
        private string _name;
        private string _avatar;
        private string _phoneNumber;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Avatar
        {
            get => _avatar;
            set => SetProperty(ref _avatar, value);
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public DateTime DateCreated { get; set; }
        public int Id { get; set; }

        public bool Equals(Contact other)
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
            return Equals((Contact) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}

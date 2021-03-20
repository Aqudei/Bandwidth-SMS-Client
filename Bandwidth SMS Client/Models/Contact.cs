using System;
using System.Collections.Generic;
using System.Text;
using Prism.Mvvm;

namespace Bandwidth_SMS_Client.Models
{
    public class Contact : BindableBase
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
    }
}

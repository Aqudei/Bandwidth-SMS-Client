using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Bandwidth_SMS_Client.Models;
using Prism.Mvvm;
using Prism.Regions;

namespace Bandwidth_SMS_Client.ViewModels
{
    class ContactsViewModel : BindableBase, INavigationAware
    {
        private readonly SMSClient _smsClient;
        private Dispatcher _dispatcher;
        private ObservableCollection<Contact> _contacts = new ObservableCollection<Contact>();
        public ICollectionView Contacts => CollectionViewSource.GetDefaultView(_contacts);

        public ContactsViewModel(SMSClient smsClient)
        {
            _smsClient = smsClient;
            _dispatcher = Application.Current.Dispatcher;
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Task.Run(async () =>
            {
                var contacts = await _smsClient.ListContactsAsync();
                _dispatcher.Invoke(() => _contacts.AddRange(contacts));
            });
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
    }
}

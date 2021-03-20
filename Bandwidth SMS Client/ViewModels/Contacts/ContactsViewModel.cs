using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Bandwidth_SMS_Client.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace Bandwidth_SMS_Client.ViewModels.Contacts
{
    class ContactsViewModel : BindableBase, INavigationAware
    {
        private readonly SMSClient _smsClient;
        private readonly IRegionManager _regionManager;
        private readonly Dispatcher _dispatcher;
        private readonly ObservableCollection<Contact> _contacts = new ObservableCollection<Contact>();
        private DelegateCommand<Contact> _editContactCommand;
        private Contact _selectedContact;
        public ICollectionView Contacts => CollectionViewSource.GetDefaultView(_contacts);

        public Contact SelectedContact
        {
            get => _selectedContact;
            set => SetProperty( ref _selectedContact , value);
        }

        public ContactsViewModel(SMSClient smsClient, IRegionManager regionManager)
        {
            _smsClient = smsClient;
            _regionManager = regionManager;
            _dispatcher = Application.Current.Dispatcher;
        }

        public DelegateCommand<Contact> EditContactCommand => _editContactCommand ??= new DelegateCommand<Contact>(DoEditContact);

        private void DoEditContact(Contact contact)
        {
            var navigationParameters = new NavigationParameters { { "contact", contact } };
            _regionManager.RequestNavigate("ActionRegion", "ContactEditor", navigationParameters);
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

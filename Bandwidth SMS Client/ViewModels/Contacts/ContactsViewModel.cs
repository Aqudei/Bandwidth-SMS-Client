using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace Bandwidth_SMS_Client.ViewModels.Contacts
{
    class ContactsViewModel : BindableBase, INavigationAware
    {
        private readonly SMSClient _smsClient;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dispatcher _dispatcher;
        private readonly ObservableCollection<Contact> _contacts = new ObservableCollection<Contact>();
        private DelegateCommand<Contact> _editContactCommand;
        private Contact _selectedContact;
        private string _search;
        public ICollectionView Contacts => CollectionViewSource.GetDefaultView(_contacts);

        public string Search
        {
            get => _search;
            set => SetProperty(ref _search, value);
        }

        public Contact SelectedContact
        {
            get => _selectedContact;
            set => SetProperty(ref _selectedContact, value);
        }

        public ContactsViewModel(SMSClient smsClient, IRegionManager regionManager,
            IEventAggregator eventAggregator)
        {
            _smsClient = smsClient;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _dispatcher = Application.Current.Dispatcher;
            _eventAggregator.GetEvent<ContactUpdated>().Subscribe(ContactUpdated);
            PropertyChanged += ContactsViewModel_PropertyChanged;
        }

        private void ContactsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (nameof(Search) == e.PropertyName)
            {
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    Contacts.Filter = s => s is Contact contact &&
                                           contact.PhoneNumber.Contains(Search);
                }
            }
        }

        private void ContactUpdated(ContactUpdatedPayload obj)
        {
            if (_contacts.Contains(obj.Contact))
                _contacts.Remove(obj.Contact);
            _contacts.Add(obj.Contact);
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

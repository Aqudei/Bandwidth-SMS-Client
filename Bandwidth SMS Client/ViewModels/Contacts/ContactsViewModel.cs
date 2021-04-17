using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.ViewModels.Contacts
{
    class ContactsViewModel : BindableBase, INavigationAware
    {
        private readonly SMSClient _smsClient;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IDialogService _dialogService;
        private readonly Dispatcher _dispatcher;
        private readonly ObservableCollection<Contact> _contacts = new ObservableCollection<Contact>();
        private DelegateCommand<Contact> _editContactCommand;
        private Contact _selectedContact;
        private string _search;
        private DelegateCommand _newContactCommand;
        private DelegateCommand<Contact> _deleteContactCommand;
        private string _mediaPath;
        private DelegateCommand<Contact> _newMessageCommand;

        public ICollectionView Contacts => CollectionViewSource.GetDefaultView(_contacts);

        public DelegateCommand NewContactCommand => _newContactCommand ??= new DelegateCommand(DoNewContact);

        private void DoNewContact()
        {
            _regionManager.RequestNavigate(RegionNames.ActionRegion, "ContactEditor");
        }


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
            IEventAggregator eventAggregator, IDialogCoordinator dialogCoordinator, IDialogService dialogService)
        {
            _smsClient = smsClient;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _dialogCoordinator = dialogCoordinator;
            _dialogService = dialogService;
            _dispatcher = Application.Current.Dispatcher;
            _eventAggregator.GetEvent<ContactUpdated>().Subscribe(ContactUpdated);
            PropertyChanged += ContactsViewModel_PropertyChanged;

            _mediaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BandwidthSMS");
            Directory.CreateDirectory(_mediaPath);
        }

        private void ContactsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (nameof(Search) == e.PropertyName)
            {
                if (!string.IsNullOrWhiteSpace(Search))
                {

                    Contacts.Filter = s =>
                    {
                        var contact = s as Contact;

                        var include = contact != null &&
                            contact.PhoneNumber.Contains(Search);


                        if (contact != null && !string.IsNullOrWhiteSpace(contact.Name))
                        {
                            include |= contact.Name.Contains(Search);
                        }

                        return include;
                    };
                }
                else
                {
                    Contacts.Filter = s => true;
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
        public DelegateCommand<Contact> DeleteContactCommand => _deleteContactCommand ??= new DelegateCommand<Contact>(DoDeleteContact);
        public DelegateCommand<Contact> NewMessageCommand => _newMessageCommand ??= new DelegateCommand<Contact>(DoNewMessage);

        private void DoNewMessage(Contact contact)
        {
            var param = new DialogParameters { { "recipient", contact.PhoneNumber } };
            _dialogService.ShowDialog("SMSComposer", param, null);
        }

        private async void DoDeleteContact(Contact contact)
        {
            try
            {
                await _smsClient.DeleteContactAsync(contact);
                _contacts.Remove(contact);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void DoEditContact(Contact contact)
        {
            var navigationParameters = new NavigationParameters { { "contact", contact } };
            _regionManager.RequestNavigate("ActionRegion", "ContactEditor", navigationParameters);
        }


        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Task.Run(async () =>
            {
                var wait = await _dialogCoordinator.ShowProgressAsync(this, "Please wait",
                    "Fetching contacts from server");
                wait.SetIndeterminate();
                var contacts = await _smsClient.ListContactsAsync();
                var enumerable = contacts as Contact[] ?? contacts.ToArray();

                await _dispatcher.Invoke(async () =>
                {
                    _contacts.Clear();
                    _contacts.AddRange(enumerable);
                    await wait.CloseAsync();
                });
            });
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            var view = _regionManager.Regions["ActionRegion"].ActiveViews.FirstOrDefault();
            if (view != null)
                _regionManager.Regions["ActionRegion"].Deactivate(view);

        }
    }
}

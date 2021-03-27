using System;
using System.Linq;
using AutoMapper;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace Bandwidth_SMS_Client.ViewModels.Contacts
{
    class ContactEditorViewModel : BindableBase, INavigationAware
    {
        private readonly IMapper _mapper;
        private readonly IRegionManager _regionManager;
        private readonly SMSClient _smsClient;
        private readonly IEventAggregator _eventAggregator;
        private string _name;
        private string _avatar;
        private string _phoneNumber;
        private DateTime _dateCreated;
        private DelegateCommand _saveCommand;

        public ContactEditorViewModel(IMapper mapper, IRegionManager regionManager,
            SMSClient smsClient, IEventAggregator eventAggregator)
        {
            _mapper = mapper;
            _regionManager = regionManager;
            _smsClient = smsClient;
            _eventAggregator = eventAggregator;
        }
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

        public DateTime DateCreated
        {
            get => _dateCreated;
            set => SetProperty(ref _dateCreated, value);
        }

        public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(DoSave);
        public int Id { get; set; }
        private void DoSave()
        {
            try
            {
                var contact = _mapper.Map<Contact>(this);
                var updateType = contact.Id == 0
                    ? ContactUpdatedPayload.UpdateTypes.Created
                    : ContactUpdatedPayload.UpdateTypes.Updated;
                contact = _smsClient.SaveContact(contact);
                _eventAggregator.GetEvent<ContactUpdated>().Publish(new ContactUpdatedPayload
                {
                    Contact = contact,
                    UpdateType = updateType
                });

                var view = _regionManager.Regions["ActionRegion"].ActiveViews.FirstOrDefault();
                _regionManager.Regions["ActionRegion"].Deactivate(view);
            }
            catch
            {
                // ignored
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var contact = navigationContext.Parameters.GetValue<Contact>("contact");
            _mapper.Map(contact, this);
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

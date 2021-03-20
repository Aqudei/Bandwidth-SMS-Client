using System;
using System.Linq;
using AutoMapper;
using Bandwidth_SMS_Client.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace Bandwidth_SMS_Client.ViewModels.Contacts
{
    class ContactEditorViewModel : BindableBase, INavigationAware
    {
        private readonly IMapper _mapper;
        private readonly IRegionManager _regionManager;
        private string _name;
        private string _avatar;
        private string _phoneNumber;
        private DateTime _dateCreated;
        private DelegateCommand _saveCommand;

        public ContactEditorViewModel(IMapper mapper, IRegionManager regionManager)
        {
            _mapper = mapper;
            _regionManager = regionManager;
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

        private void DoSave()
        {
            var view = _regionManager.Regions["ActionRegion"].ActiveViews.FirstOrDefault();
            _regionManager.Regions["ActionRegion"].Deactivate(view);
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

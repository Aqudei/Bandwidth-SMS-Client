using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Bandwidth_SMS_Client.Models;
using Bandwidth_SMS_Client.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;
        private readonly SMSClient _smsClient;
        private Message _selectedMessage;
        private string _message;
        private DelegateCommand _sendCommand;
        private MessageThread _selectedThread;
        public ObservableCollection<MessageThread> MessageThreads { get; set; } = new ObservableCollection<MessageThread>();
        public ObservableCollection<MessageItem> Messages { get; set; } = new ObservableCollection<MessageItem>();

        public MessageThread SelectedThread
        {
            get => _selectedThread;
            set => SetProperty(ref _selectedThread, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public Message SelectedMessage
        {
            get => _selectedMessage;
            set => SetProperty(ref _selectedMessage, value);
        }

        public DelegateCommand SendCommand => _sendCommand ??=
            new DelegateCommand(DoSend);

        private void DoSend()
        {
            _smsClient.SendMessage(SelectedThread.Recipient, Message);
            Message = "";
        }

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService, SMSClient smsClient)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _smsClient = smsClient;
            _dialogService.ShowDialog("Login", result =>
            {
                if (result.Result == ButtonResult.Abort)
                {
                    Application.Current.Shutdown();
                }
            });

            try
            {

                MessageThreads.AddRange(_smsClient.GetThreads());
            }
            catch
            {
                //ignored
            }


            this.PropertyChanged += MainWindowViewModel_PropertyChanged;
        }

        private void MainWindowViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedThread))
            {
                Messages.Clear();
                Messages.AddRange(SelectedThread.MessageItems);
            }
        }
    }
}

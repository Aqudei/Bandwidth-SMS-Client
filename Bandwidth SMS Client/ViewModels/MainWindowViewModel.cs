using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using Bandwidth_SMS_Client.Views;
using Prism.Commands;
using Prism.Events;
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
        private readonly IEventAggregator _eventAggregator;
        private Message _selectedMessage;
        private string _message;
        private DelegateCommand _sendCommand;
        private MessageThread _selectedThread;
        private DelegateCommand _newMessageCommand;
        private DelegateCommand<MessageItem> _deleteMessageCommand;
        public ObservableCollection<MessageThread> MessageThreads { get; set; } = new ObservableCollection<MessageThread>();
        public ObservableCollection<MessageItem> Messages { get; set; } = new ObservableCollection<MessageItem>();

        public DelegateCommand NewMessageCommand => _newMessageCommand ??= new DelegateCommand(DoNewMessage);

        private void DoNewMessage()
        {
            _dialogService.ShowDialog("SMSComposer");
        }


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

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService,
            SMSClient smsClient, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _smsClient = smsClient;
            _eventAggregator = eventAggregator;

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

            _eventAggregator.GetEvent<MessageEvent>().Subscribe(MessageEventHandler, ThreadOption.UIThread);
            PropertyChanged += MainWindowViewModel_PropertyChanged;
        }

        public DelegateCommand<MessageItem> DeleteMessageCommand => _deleteMessageCommand ??= new DelegateCommand<MessageItem>(DoDeleteMessage);

        private void DoDeleteMessage(MessageItem message)
        {
            try
            {
                _smsClient.DeleteMessage(message.Id);
                Messages.Remove(message);
            }
            catch (Exception e)
            {
                //Ignored
            }
        }

        private void MessageEventHandler(MessageEventPayload e)
        {

            if (e.EventType == MessageEventPayload.MessageEventType.Created)
            {
                if (Messages.Any(m => m.Message_Bwid == e.MessageItem.Message_Bwid))
                {
                    var messageItem = Messages.First(m => m.Message_Bwid == e.MessageItem.Message_Bwid);
                    Messages.Remove(messageItem);
                }

                Messages.Add(e.MessageItem);
            }
        }

        //private void _smsClient_MessageUpdate(object sender, MessageEvent e)
        //{
        //    if (e.EventType == MessageEvent.MessageEventType.Created)
        //    {
        //        var messageItem = Messages.First(m => m.Message_Bwid == e.MessageItem.Message_Bwid);
        //        if (messageItem != null)
        //        {
        //            Messages.Remove(messageItem);
        //        }

        //        Messages.Add(e.MessageItem);
        //    }
        //}

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

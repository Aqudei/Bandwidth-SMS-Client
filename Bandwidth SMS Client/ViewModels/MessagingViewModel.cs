using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using AutoMapper;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.ViewModels
{
    class MessagingViewModel : BindableBase, INavigationAware
    {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;
        private readonly SMSClient _smsClient;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;
        private Message _selectedMessage;
        private string _message;
        private DelegateCommand _sendCommand;
        private DelegateCommand _newMessageCommand;
        private DelegateCommand<MessageItem> _deleteMessageCommand;
        private readonly Dispatcher _dispatcher;
        private Conversation _selectedConversation;

        private readonly ObservableCollection<Conversation> _conversations = new ObservableCollection<Conversation>();
        private DelegateCommand<Conversation> _deleteConversationCommand;
        public ICollectionView Conversations => CollectionViewSource.GetDefaultView(_conversations);

        public Conversation SelectedConversation
        {
            get => _selectedConversation;
            set => SetProperty(ref _selectedConversation, value);
        }

        public ObservableCollection<MessageItem> Messages { get; set; } = new ObservableCollection<MessageItem>();

        public DelegateCommand NewMessageCommand => _newMessageCommand ??= new DelegateCommand(DoNewMessage);

        public DelegateCommand<Conversation> DeleteConversationCommand =>
            _deleteConversationCommand ??= new DelegateCommand<Conversation>(DoDeleteConversation);

        private async void DoDeleteConversation(Conversation conversation)
        {
            var result = await _dialogCoordinator.ShowMessageAsync(this, "SMSTrifecta",
                $"Are you sure you want to delete conversation thread with {conversation.PhoneNumber}?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Negative)
            {
                return;
            }

            await _smsClient.DeleteConversationAsync(conversation);
        }

        private void DoNewMessage()
        {
            _dialogService.ShowDialog("SMSComposer");
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

        private async void DoSend()
        {
            await _smsClient.SendMessageAsync(SelectedConversation.PhoneNumber, Message);
            Message = "";
        }

        public MessagingViewModel(IRegionManager regionManager, IDialogService dialogService,
            SMSClient smsClient, IEventAggregator eventAggregator, IMapper mapper, IDialogCoordinator dialogCoordinator)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _smsClient = smsClient;
            _eventAggregator = eventAggregator;
            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
            _dispatcher = Application.Current.Dispatcher;

            try
            {
                Task.Run(async () =>
                {
                    var conversations = await _smsClient.ListConversationsAsync();
                    var progress = await _dialogCoordinator.ShowProgressAsync(this, "Trifecta SMS", "Loading SMS");

                    await _dispatcher.Invoke(async () =>
                    {
                        _conversations.AddRange(conversations);
                        await progress.CloseAsync();
                    });
                });
            }
            catch
            {
                //ignored
            }

            PropertyChanged += MainWindowViewModel_PropertyChanged;
            _smsClient.MessageEvent += _smsClient_MessageEvent;
            _smsClient.ConversationEvent += _smsClient_ConversationEvent;
        }

        private void _smsClient_ConversationEvent(object sender, ConversationEventPayload e)
        {
            switch (e.EventType)
            {
                case ConversationEventPayload.ConversationEventType.Updated:
                    {
                        var conversation = _conversations.FirstOrDefault(c => c.Id == e.ConversationItem.Id);
                        if (conversation != null)
                        {
                            _dispatcher.Invoke(() => _mapper.Map(e.ConversationItem, conversation));
                        }

                        break;
                    }
                case ConversationEventPayload.ConversationEventType.Created:
                    {
                        if (!Conversations.Contains(e.ConversationItem))
                            _dispatcher.Invoke(() => _conversations.Add(e.ConversationItem));
                        break;
                    }
            }
        }

        private void _smsClient_MessageEvent(object sender, MessageEventPayload e)
        {
            switch (e.EventType)
            {
                case MessageEventPayload.MessageEventType.Created:
                    {
                        var conversationNumber = e.MessageItem.MessageType == "INCOMING" ? e.MessageItem.From : e.MessageItem.To;
                        var conversation = _conversations.FirstOrDefault(c => c.PhoneNumber == conversationNumber);

                        if (conversation != null)
                        {
                            _dispatcher.Invoke(() => conversation.HasNew = true);
                        }

                        if (conversation != null && SelectedConversation != null && SelectedConversation.Equals(conversation))
                        {
                            _dispatcher.Invoke(() => Messages.Add(e.MessageItem));
                        }

                    }
                    break;
                case MessageEventPayload.MessageEventType.Deleted:
                    {
                        var conversationNumber = e.MessageItem.MessageType == "INCOMING" ? e.MessageItem.From : e.MessageItem.To;
                        var conversation = _conversations.FirstOrDefault(c => c.PhoneNumber == conversationNumber);

                        if (conversation != null && SelectedConversation != null && SelectedConversation.Equals(conversation))
                        {
                            _dispatcher.Invoke(() => Messages.Remove(e.MessageItem));
                        }
                    }
                    break;
            }
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

        private void MainWindowViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedConversation) && SelectedConversation != null)
            {
                Messages.Clear();
                SelectedConversation.HasNew = false;
                Task.Run(async () =>
                {
                    var progress =
                        await _dialogCoordinator.ShowProgressAsync(this, "Please wait...", "Fetching messages");
                    var messages = await _smsClient.ListMessagesAsync(SelectedConversation.Id);
                    await _dispatcher.Invoke(async () =>
                    {
                        Messages.AddRange(messages);
                        await progress.CloseAsync();
                    });
                });

            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {

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

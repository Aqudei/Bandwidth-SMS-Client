using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AutoMapper;
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
        private readonly IMapper _mapper;
        private Message _selectedMessage;
        private string _message;
        private DelegateCommand _sendCommand;
        private MessageThread _selectedThread;
        private DelegateCommand _newMessageCommand;
        private DelegateCommand<MessageItem> _deleteMessageCommand;
        private Dispatcher _dispatcher;
        private Conversation _selectedConversation;
        public ObservableCollection<Conversation> Conversations { get; set; } = new ObservableCollection<Conversation>();

        public Conversation SelectedConversation
        {
            get => _selectedConversation;
            set => SetProperty(ref _selectedConversation, value);
        }

        public ObservableCollection<MessageItem> Messages { get; set; } = new ObservableCollection<MessageItem>();

        public DelegateCommand NewMessageCommand => _newMessageCommand ??= new DelegateCommand(DoNewMessage);

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

        private void DoSend()
        {
            _smsClient.SendMessage(SelectedConversation.PhoneNumber, Message);
            Message = "";
        }

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService,
            SMSClient smsClient, IEventAggregator eventAggregator, IMapper mapper)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _smsClient = smsClient;
            _eventAggregator = eventAggregator;
            _mapper = mapper;
            _dispatcher = Application.Current.Dispatcher;

            _dialogService.ShowDialog("Login", result =>
            {
                if (result.Result == ButtonResult.Abort)
                {
                    Application.Current.Shutdown();
                }
            });

            try
            {
                Conversations.AddRange(_smsClient.ListConversations());
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
                        var conversation = Conversations.FirstOrDefault(c => c.Id == e.ConversationItem.Id);
                        if (conversation != null)
                        {
                            _dispatcher.Invoke(() => _mapper.Map(e.ConversationItem, conversation));
                        }

                        break;
                    }
                case ConversationEventPayload.ConversationEventType.Created:
                    {
                        if (!Conversations.Contains(e.ConversationItem))
                            _dispatcher.Invoke(() => Conversations.Add(e.ConversationItem));
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
                        var conversation = Conversations.FirstOrDefault(c => c.PhoneNumber == conversationNumber);

                        if (conversation != null && SelectedConversation != null && SelectedConversation.Equals(conversation))
                        {
                            _dispatcher.Invoke(() => Messages.Add(e.MessageItem));
                        }

                    }
                    break;
                case MessageEventPayload.MessageEventType.Deleted:
                    {
                        var conversationNumber = e.MessageItem.MessageType == "INCOMING" ? e.MessageItem.From : e.MessageItem.To;
                        var conversation = Conversations.FirstOrDefault(c => c.PhoneNumber == conversationNumber);

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
                Task.Run(async () =>
                {
                    var conversations = await _smsClient.ListMessagesAsync(SelectedConversation.Id);
                    _dispatcher.Invoke(() =>
                    {
                        Messages.AddRange(conversations);
                    });
                });

            }
        }
    }
}

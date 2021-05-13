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
using AutoMapper;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.ViewModels
{
    internal class MessagingViewModel : BindableBase, INavigationAware, IActiveAware
    {
        private readonly ObservableCollection<Conversation> _conversations = new ObservableCollection<Conversation>();
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IDialogService _dialogService;
        private readonly Dispatcher _dispatcher;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMapper _mapper;
        private readonly IRegionManager _regionManager;
        private readonly SMSClient _smsClient;

        private DelegateCommand<Conversation> _addToContactsCommand;
        private DelegateCommand _attachMediaCommand;
        private string _attachment;
        private DelegateCommand<Conversation> _deleteConversationCommand;
        private DelegateCommand<MessageItem> _deleteMessageCommand;
        private string _message;
        private DelegateCommand _newMessageCommand;
        private DelegateCommand _removeMediaCommand;
        private Conversation _selectedConversation;
        private Message _selectedMessage;
        private DelegateCommand _sendCommand;
        private DelegateCommand<string> _downloadOpenCommand;
        private readonly string _mediaDirectory;

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

            _mediaDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bandwidth SMS Media");
            Directory.CreateDirectory(_mediaDirectory);

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
            _smsClient.ContactUpdatedEvent += _smsClient_ContactUpdatedEvent;
        }

        public ICollectionView Conversations => CollectionViewSource.GetDefaultView(_conversations);

        public DelegateCommand<string> DownloadOpenCommand => _downloadOpenCommand ??= new DelegateCommand<string>(DoDownloadOpen);

        private void DoDownloadOpen(string file)
        {
            var fileName = Path.Combine(_mediaDirectory, Path.GetFileName(file));
            if (File.Exists(fileName))
            {
                var fileOpener = new Process
                {
                    StartInfo = { FileName = "explorer", Arguments = "\"" + fileName + "\"" }
                };
                fileOpener.Start();
                return;
            }

            var remoteUri = $"http://sms.tripbx.com:8080/{file}";
            // Create a new WebClient instance.
            using var myWebClient = new WebClient();
            myWebClient.DownloadFile(remoteUri, fileName);
        }

        public Conversation SelectedConversation
        {
            get => _selectedConversation;
            set => SetProperty(ref _selectedConversation, value);
        }

        public ObservableCollection<MessageItem> Messages { get; set; } = new ObservableCollection<MessageItem>();

        public DelegateCommand NewMessageCommand => _newMessageCommand ??= new DelegateCommand(DoNewMessage);

        public DelegateCommand<Conversation> DeleteConversationCommand =>
            _deleteConversationCommand ??= new DelegateCommand<Conversation>(DoDeleteConversation);

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
            new DelegateCommand(DoSend, () => !string.IsNullOrWhiteSpace(Message) || !string.IsNullOrWhiteSpace(Attachment))
                .ObservesProperty(() => Message)
                .ObservesProperty(() => Attachment);

        public DelegateCommand<MessageItem> DeleteMessageCommand =>
            _deleteMessageCommand ??= new DelegateCommand<MessageItem>(DoDeleteMessage);

        public DelegateCommand<Conversation> AddToContactsCommand =>
            _addToContactsCommand ??= new DelegateCommand<Conversation>(DoAddToContacts);

        public DelegateCommand AddMediaCommand => _attachMediaCommand ??= new DelegateCommand(DoAttachMediaCommand, () => string.IsNullOrWhiteSpace(Attachment))
            .ObservesProperty(() => Attachment);

        public DelegateCommand RemoveMediaCommand => _removeMediaCommand ??= new DelegateCommand(() => Attachment = "", () => !string.IsNullOrWhiteSpace(Attachment))
            .ObservesProperty(() => Attachment);


        public string Attachment
        {
            get => _attachment;
            set => SetProperty(ref _attachment, value);
        }

        public bool IsActive { get; set; }
        public event EventHandler IsActiveChanged;

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

        private async void DoDeleteConversation(Conversation conversation)
        {
            var result = await _dialogCoordinator.ShowMessageAsync(this, "SMSTrifecta",
                $"Are you sure you want to delete conversation thread with {conversation.PhoneNumber}?",
                MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Negative) return;

            try
            {
                await _smsClient.DeleteConversationAsync(conversation);
                _conversations.Remove(conversation);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e} | {e.Message}");
            }
        }

        private void DoNewMessage()
        {
            _dialogService.ShowDialog("SMSComposer");
        }

        private async void DoSend()
        {
            if (!string.IsNullOrWhiteSpace(Attachment))
            {
                await _smsClient.SendMessageAsync(SelectedConversation.PhoneNumber, Message, Attachment);
            }
            else
            {
                await _smsClient.SendMessageAsync(SelectedConversation.PhoneNumber, Message);
            }
            Message = "";
            Attachment = "";
        }

        private void _smsClient_ContactUpdatedEvent(object sender, ContactUpdatedPayload e)
        {
            _dispatcher.Invoke(() =>
            {
                if (e.UpdateType == ContactUpdatedPayload.UpdateTypes.Created ||
                    e.UpdateType == ContactUpdatedPayload.UpdateTypes.Updated)
                {
                    var conversation = _conversations.FirstOrDefault(c => c.PhoneNumber == e.Contact.PhoneNumber);
                    if (conversation != null) conversation.Name = e.Contact.Name;
                }
            });
        }

        private void _smsClient_ConversationEvent(object sender, ConversationEventPayload e)
        {
            switch (e.EventType)
            {
                case ConversationEventPayload.ConversationEventType.Updated:
                    {
                        var conversation = _conversations.FirstOrDefault(c => c.Id == e.ConversationItem.Id);
                        if (conversation != null) _dispatcher.Invoke(() => _mapper.Map(e.ConversationItem, conversation));

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
                        var conversationNumber =
                            e.MessageItem.MessageType == "INCOMING" ? e.MessageItem.From : e.MessageItem.To;
                        var conversation = _conversations.FirstOrDefault(c => c.PhoneNumber == conversationNumber);

                        if (conversation != null) _dispatcher.Invoke(() => conversation.HasNew = true);

                        if (conversation != null && SelectedConversation != null &&
                            SelectedConversation.Equals(conversation))
                            _dispatcher.Invoke(() => Messages.Add(e.MessageItem));
                    }
                    break;
                case MessageEventPayload.MessageEventType.Deleted:
                    {
                        var conversationNumber =
                            e.MessageItem.MessageType == "INCOMING" ? e.MessageItem.From : e.MessageItem.To;
                        var conversation = _conversations.FirstOrDefault(c => c.PhoneNumber == conversationNumber);

                        if (conversation != null && SelectedConversation != null &&
                            SelectedConversation.Equals(conversation))
                            _dispatcher.Invoke(() => Messages.Remove(e.MessageItem));
                    }
                    break;
            }
        }

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

        private void DoAddToContacts(Conversation conversation)
        {
            if (conversation.Contact == null)
            {
                var parameters = new NavigationParameters
                {
                    {
                        "contact", new Contact
                        {
                            Added = true,
                            PhoneNumber = conversation.PhoneNumber
                        }
                    }
                };

                _regionManager.RequestNavigate(RegionNames.ActionRegion, "ContactEditor", parameters);
            }
            else
            {
                // Modify
                // var navigationParameters = new NavigationParameters { { "contact", contact } };
                conversation.Contact.Added = true;
                var parameters = new NavigationParameters { { "contact", conversation.Contact } };
                _regionManager.RequestNavigate(RegionNames.ActionRegion, "ContactEditor", parameters);
            }
        }

        private void DoAttachMediaCommand()
        {
            var dialog = new CommonOpenFileDialog
            {
                Multiselect = false
            };
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok) Attachment = dialog.FileName;
        }

        private void MainWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
    }
}
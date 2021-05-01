using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.ViewModels
{
    class SMSComposerViewModel : BindableBase, IDialogAware
    {
        public string Attachment
        {
            get => _attachment;
            set => SetProperty(ref _attachment, value);
        }

        private DelegateCommand _closeCommand;
        private DelegateCommand _sendCommand;
        private readonly SMSClient _smsClient;
        private string _message;
        private string _recipient;
        private string _attachment;
        private DelegateCommand _attachMediaCommand;
        private DelegateCommand _removeMediaCommand;

        public DelegateCommand AddMediaCommand => _attachMediaCommand ??= new DelegateCommand(DoAttachMediaCommand, () => string.IsNullOrWhiteSpace(Attachment))
            .ObservesProperty(() => Attachment);

        public DelegateCommand RemoveMediaCommand => _removeMediaCommand ??= new DelegateCommand(() => Attachment = "", () => !string.IsNullOrWhiteSpace(Attachment))
            .ObservesProperty(() => Attachment);

        private void DoAttachMediaCommand()
        {
            var dialog = new CommonOpenFileDialog
            {
                Multiselect = false
            };
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok) Attachment = dialog.FileName;
        }
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string Recipient
        {
            get => _recipient;
            set => SetProperty(ref _recipient, value);
        }

        public DelegateCommand CloseCommand => _closeCommand ??= new DelegateCommand(DoClose);
        public DelegateCommand SendCommand => _sendCommand ??= new DelegateCommand(DoSend);

        private async void DoSend()
        {
            if (string.IsNullOrWhiteSpace(Attachment))
            {
                await _smsClient.SendMessageAsync(Recipient, Message);
            }
            else
            {
                await _smsClient.SendMessageAsync(Recipient, Message, Attachment);
            }
            RequestClose?.Invoke(null);
        }

        private void DoClose()
        {
            RequestClose?.Invoke(null);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var success = parameters.TryGetValue<string>("recipient", out var recipient);
            if (success)
            {
                Recipient = recipient;
            }
        }

        public SMSComposerViewModel(SMSClient smsClient)
        {
            _smsClient = smsClient;
        }

        public string Title { get; } = "Compose SMS";
        public event Action<IDialogResult> RequestClose;
    }
}

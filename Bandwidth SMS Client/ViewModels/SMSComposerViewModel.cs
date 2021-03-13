using System;
using System.Collections.Generic;
using System.Text;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.ViewModels
{
    class SMSComposerViewModel : BindableBase, IDialogAware
    {
        private DelegateCommand _closeCommand;
        private DelegateCommand _sendCommand;
        private readonly SMSClient _smsClient;
        private string _message;
        private string _recipient;

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

        private void DoSend()
        {
            _smsClient.SendMessage(Recipient, Message);
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

        }

        public SMSComposerViewModel(SMSClient smsClient)
        {
            _smsClient = smsClient;
        }

        public string Title { get; } = "Compose SMS";
        public event Action<IDialogResult> RequestClose;
    }
}

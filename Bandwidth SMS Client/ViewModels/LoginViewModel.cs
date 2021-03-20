using System;
using System.Diagnostics;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.ViewModels
{
    internal class LoginViewModel : BindableBase, IDialogAware
    {
        private readonly SMSClient _client;
        private DelegateCommand _exitCommand;
        private DelegateCommand _loginCommand;
        private string _password = "Espelimbergo";
        private string _username = "archie";

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public DelegateCommand ExitCommand
        {
            get { return _exitCommand ??= new DelegateCommand(DoExit); }
        }


        public DelegateCommand LoginCommand
        {
            get { return _loginCommand ??= new DelegateCommand(DoLogin); }
        }

        private async void DoLogin()
        {
            try
            {
                await _client.LoginAsync(Username, Password);
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void DoExit()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Abort));
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

        public string Title { get; } = "Login";
        public event Action<IDialogResult> RequestClose;


        public LoginViewModel(SMSClient client)
        {
            _client = client;
        }
    }
}
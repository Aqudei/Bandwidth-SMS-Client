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
using Bandwidth_SMS_Client.Views;
using MahApps.Metro.Controls.Dialogs;
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
        private readonly Dispatcher _dispatcher;
        private DelegateCommand<string> _navigateCommand;

        public DelegateCommand<string> NavigateCommand => _navigateCommand ??= new DelegateCommand<string>(DoNavigate);

        private void DoNavigate(string viewName)
        {
            _regionManager.RequestNavigate("MainRegion", viewName);
        }

        public MainWindowViewModel(IDialogService dialogService, IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _dispatcher = Application.Current.Dispatcher;

            dialogService.ShowDialog("Login", result =>
            {
                if (result.Result == ButtonResult.Abort)
                {
                    Application.Current.Shutdown();
                }
            });

            regionManager.RegisterViewWithRegion("MainRegion", typeof(Messaging));
        }
    }
}

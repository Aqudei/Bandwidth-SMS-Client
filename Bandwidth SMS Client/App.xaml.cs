using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using Bandwidth_SMS_Client.Models;
using Bandwidth_SMS_Client.ViewModels;
using Bandwidth_SMS_Client.Views;
using Prism.Events;
using Prism.Ioc;
using Prism.Unity;

namespace Bandwidth_SMS_Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialogWindow<MyMetroWindow>();
            containerRegistry.RegisterDialog<Login, LoginViewModel>();
            containerRegistry.RegisterDialog<SMSComposer, SMSComposerViewModel>();
            containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
            containerRegistry.RegisterSingleton<SMSClient>();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<Conversation, Conversation>());
            containerRegistry.RegisterInstance(config.CreateMapper());
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
    }
}

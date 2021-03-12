using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Prism.Services.Dialogs;

namespace Bandwidth_SMS_Client.Views
{
    /// <summary>
    /// Interaction logic for MyMetroWindow.xaml
    /// </summary>
    public partial class MyMetroWindow : IDialogWindow
    {
        public MyMetroWindow()
        {
            InitializeComponent();
        }

        public IDialogResult Result { get; set; }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReceiverPage : Page
    {

        

        public ReceiverPage()
        {
            this.InitializeComponent();
            this.InitUI();
        }

        internal void InitUI()
        {
            TitleTxt.Text = "Transfer Files";
            senderDeviceIcon.Glyph = "\uE7F8";
            receiverDeviceIcon.Glyph = "\uE7F8";
            senderDeviceName.Text = App._server._serverNickname;
            receiverDeviceName.Text = "Not Set";
            transferStatus.ShowPaused = true;

            if (App.target_device != null)
            {
                // Handle File Transfer
                receiverDeviceName.Text = App.target_device.deviceName;
                //transferInfoBar.Visibility = Visibility.Collapsed;
                transferInfoBar.Title = "Choosing your files / folders";
                transferInfoBar.Message = "Select the files / folders you want to transfer to. ";
            } else
            {
                transferInfoBar.Severity = InfoBarSeverity.Warning; 
                transferInfoBar.Title = "Device Not Selected";
                transferInfoBar.Message = "Please go to Computers to setup target device. ";
            }

        }



    }
}

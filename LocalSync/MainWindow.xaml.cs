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
using Microsoft.UI.Windowing;
using LocalSync.Helper;
using Microsoft.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        int fileCountIndex = 0;
        public MainWindow()
        {
            this.InitializeComponent();
            this.InitUI();
            this.InitEvent();
        }

        internal void InitUI()
        {
            this.LoadLocalizedStrings();
            this.SetupIcon();
            // Page Starting at Homepage as default
            contentFrame.Navigate(typeof(HomePage));
            Nav.SelectedItem = HomeNav;
            this.Title = "Local Sync";
            this.AppWindow.SetIcon("Assets/WindowLogoscale-128.ico");
            WindowHelper.TrackWindow(this);
            this.SetupTitleBar();

        }

        public void setStatusNav(bool status)
        {
            Nav.IsEnabled = status;
        }

        private void SetupTitleBar()
        {
            //ThemeHelper.Initialize();
            ElementTheme currentTheme = ThemeHelper.ActualTheme;
            Window window = WindowHelper._activeWindows[0];
            if (currentTheme == ElementTheme.Dark)
            {
                TitleBarHelper.SetForegroundColor(window, Colors.Black);
            }
            else if (currentTheme == ElementTheme.Light) {
                TitleBarHelper.SetForegroundColor(window, Colors.White);
            }
        }

        private void LoadLocalizedStrings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView

            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            this.HomeNav.Content = resourceMap.GetValue("HomeNavUid/Content", resourceContext).ValueAsString;
            this.ReceiveNav.Content = resourceMap.GetValue("ReceiveNavUid/Content", resourceContext).ValueAsString;
            this.SenderNav.Content = resourceMap.GetValue("SenderNavUid/Content", resourceContext).ValueAsString;
            this.computerSharingNav.Content = resourceMap.GetValue("computerSharingNavUid/Content", resourceContext).ValueAsString;

        }

        internal void InitEvent()
        {
            this.Closed += OnWindowClosed;
        }

        internal void SetupIcon()
        {
            // Computer Sharing
            FontIcon computerSharingNav_Icon = new FontIcon();
            computerSharingNav_Icon.Glyph = "\uF385";
            computerSharingNav.Icon = computerSharingNav_Icon;

            // My Sharing
            FontIcon mySharing_Icon = new FontIcon();
            mySharing_Icon.Glyph = "\uEC27";
            SenderNav.Icon = mySharing_Icon; 

            // Public Sharing
            FontIcon publicSharing_Icon = new FontIcon();
            publicSharing_Icon.Glyph = "\uE704"; 
            ReceiveNav.Icon = publicSharing_Icon;
        }

        // When Window Closed 
        private async void OnWindowClosed(object sender, WindowEventArgs e)
        {
            await App.discoveryClient.SendCloseRequestAsync(App._server._serverIp, App._server._serverNickname);
        }

        public void sendNotification(string device_ip, int total)
        {
            fileCountIndex++;
            if (fileCountIndex == total)
            {
                App.SendNotificationWithActivation("File Transfer", $"{fileCountIndex} Files have been transferred from {device_ip}", "Receive");
                fileCountIndex = 0;
            }
            
        }

        public void senderNotification()
        {
            App.SendNotificationWithActivation("Success!", "File Sent Successfully", "Send");
            
        }

        public void navSwitchTo(string page)
        {
            switch (page)
            {
                case "Home":
                    contentFrame.Navigate(typeof(HomePage));
                    Nav.SelectedItem = HomeNav;
                    break;

                case "Receive":
                    contentFrame.Navigate(typeof(ReceivePage));
                    Nav.SelectedItem = ReceiveNav;
                    break;

                case "Send":
                    contentFrame.Navigate(typeof(SenderPage));
                    Nav.SelectedItem = SenderNav;
                    break;

                case "Computers":
                    contentFrame.Navigate(typeof(OtherComputerSharing));
                    Nav.SelectedItem = computerSharingNav;
                    break;

                default:
                    contentFrame.Navigate(typeof(HomePage));
                    Nav.SelectedItem = HomeNav;
                    break;
            }
        }


        private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                string tag = args.SelectedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "Home":
                        contentFrame.Navigate(typeof(HomePage)); 
                        break;

                    case "Receive":
                        contentFrame.Navigate(typeof(ReceivePage));
                        break;

                    case "Send":
                        contentFrame.Navigate(typeof(SenderPage));
                        break;

                    case "Computers":
                        contentFrame.Navigate(typeof(OtherComputerSharing));
                        break;
                    case "Settings":
                        contentFrame.Navigate(typeof(SettingPage));
                        break; 

                    default:
                        contentFrame.Navigate(typeof(HomePage));
                        break;
                }
            }
        }
    }
}

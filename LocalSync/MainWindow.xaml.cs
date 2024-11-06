using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using LocalSync.Helper;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainWindow : Window
    {
        int fileCountIndex = 0;
        private Dictionary<Type, NavigationViewItem> pageToMenuItemMap = new Dictionary<Type, NavigationViewItem>();
        public MainWindow()
        {
            this.InitializeComponent();
            contentFrame.Navigated += OnNavigated;
            pageToMenuItemMap.Add(typeof(HomePage), HomeNav);
            pageToMenuItemMap.Add(typeof(ReceivePage), ReceiveNav);
            pageToMenuItemMap.Add(typeof(SenderPage), SenderNav);
            pageToMenuItemMap.Add(typeof(OtherComputerSharing), computerSharingNav);
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
            SetTitleBar(AppTitleBar); 
            this.Title = "Local Sync";
            this.AppWindow.SetIcon("Assets/WindowLogoscale-128.ico");
            WindowHelper.TrackWindow(this);
            this.SetupTitleBar();

        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {

            var itemType = e.SourcePageType;

            if (itemType == typeof(SettingPage))
            {
                Nav.SelectedItem = Nav.SettingsItem;
            }
            else if (pageToMenuItemMap.TryGetValue(itemType, out var menuItem))
            {
                Nav.SelectedItem = menuItem;
            }

            // Check is go back available
            Nav.IsBackEnabled = contentFrame.CanGoBack;
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (contentFrame.CanGoBack)
            {
                contentFrame.GoBack();
            }
        }

        public void setStatusNav(bool status)
        {
            Nav.IsEnabled = status;
        }

        private void SetupTitleBar()
        {
            ExtendsContentIntoTitleBar = true;
            //ThemeHelper.Initialize();
            ElementTheme currentTheme = ThemeHelper.ActualTheme;
            Window window = WindowHelper._activeWindows[0];
            //if (currentTheme == ElementTheme.Dark)
            //{
            //    //TitleBarHelper.SetForegroundColor(window, Colors.Black);
            //}
            //else if (currentTheme == ElementTheme.Light) {
            //    //TitleBarHelper.SetForegroundColor(window, Colors.White);
            //}
            TitleBarHelper.SetForegroundColor(window, Colors.Transparent);
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
                var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
                var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                string title = resourceMap.GetValue("FileSentSuccessTitleNotification", resourceContext).ValueAsString;
                string msg = resourceMap.GetValue("FileSentSuccessMessageNotification", resourceContext).ValueAsString;
                App.SendNotificationWithActivation(title, $"{fileCountIndex} {msg} {device_ip}", "Receive");
                fileCountIndex = 0;
            }
            
        }

        public void senderNotification()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            string title = resourceMap.GetValue("FileSenderSuccessTitleNotification", resourceContext).ValueAsString;
            string msg = resourceMap.GetValue("FileSenderSuccessMessageNotification", resourceContext).ValueAsString;
            App.SendNotificationWithActivation(title, msg, "Send");
            
        }

        public void navSwitchTo(string page, bool drilllin = false)
        {
            switch (page)
            {
                case "Home":
                    if (drilllin)
                    {
                        contentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
                    } else
                    {
                        contentFrame.Navigate(typeof(HomePage));
                    }
                    //Nav.SelectedItem = HomeNav;
                    break;

                case "Receive":
                    if (drilllin)
                    {
                        contentFrame.Navigate(typeof(ReceivePage), null, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        contentFrame.Navigate(typeof(ReceivePage));
                    }
                    //Nav.SelectedItem = ReceiveNav;
                    break;

                case "Send":
                    if (drilllin)
                    {
                        contentFrame.Navigate(typeof(SenderPage), null, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        contentFrame.Navigate(typeof(SenderPage));
                    }
                    //Nav.SelectedItem = SenderNav;
                    break;

                case "Computers":
                    if (drilllin)
                    {
                        contentFrame.Navigate(typeof(OtherComputerSharing), null, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        contentFrame.Navigate(typeof(OtherComputerSharing));
                    }
                    //Nav.SelectedItem = computerSharingNav;
                    break;

                case "Settings":
                    if (drilllin)
                    {
                        contentFrame.Navigate(typeof(SettingPage), null, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        contentFrame.Navigate(typeof(SettingPage));
                    }
                    //Nav.SelectedItem = Nav.SettingsItem;
                    break;

                default:
                    if (drilllin)
                    {
                        contentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        contentFrame.Navigate(typeof(HomePage));
                    }
                    //Nav.SelectedItem = HomeNav;
                    break;
            }
        }

        //public void navSwitchTo(Type page)
        //{
        //    contentFrame.Navigate(page);
        //}

        public void ReloadLanguage()
        {
            LoadLocalizedStrings();
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

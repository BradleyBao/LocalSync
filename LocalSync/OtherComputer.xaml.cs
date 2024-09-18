using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;
using LocalSync.Modules;
using Windows.Networking.Connectivity;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OtherComputerSharing : Page
    {
        UdpDiscoveryClient discoveryClient;
        private bool _isFirstLoad = true;
        private DispatcherTimer _refreshTimer;
        private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        private DispatcherTimer _progressTimer;

        public OtherComputerSharing()
        {
            this.InitializeComponent();
            this.InitUI();

            CheckNetworkStatus();

            this.SearchForServer();
            //this.RefreshDeviceList();
        }

        internal void CheckNetworkStatus()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile == null || profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.None)
            {
                // 无网络
                DisplayNoNetworkAccessMessage();
            } else if(profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.LocalAccess || 
                profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.ConstrainedInternetAccess)
            {
                // 无法访问
                LocalAccessNetworkAccessMessage();
            } else if (profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            {
                InternetAccessNetworkAccessMessage();
            }
            CheckFireWall();
        }

        internal void CheckFireWall()
        {
            if (!App.IsFirewallRuleAllowed())
            {
                networkDeviceInfoBar.IsOpen = true;
                networkDeviceInfoBar.Severity = InfoBarSeverity.Error;
                networkDeviceInfoBarLearnmore.Visibility = Visibility.Visible;
                var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
                var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                networkDeviceInfoBar.Title = resourceMap.GetValue("FireWallDeniedTitle", resourceContext).ValueAsString;
                networkDeviceInfoBar.Message = resourceMap.GetValue("FireWallDeniedMsg", resourceContext).ValueAsString;
                networkDeviceInfoBarLearnmore.Content = resourceMap.GetValue("LearnMore", resourceContext).ValueAsString;
                string url_of_firewallerror_msg = resourceMap.GetValue("url_of_firewallerror_msg", resourceContext).ValueAsString;
                networkDeviceInfoBarLearnmore.NavigateUri = new Uri(url_of_firewallerror_msg);
            }
        }

        internal void DisplayNoNetworkAccessMessage()
        {
            networkDeviceInfoBar.IsOpen = true;
            networkDeviceInfoBar.Severity = InfoBarSeverity.Error;
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            networkDeviceInfoBar.Title = resourceMap.GetValue("NetworkAccessDeniedTitle", resourceContext).ValueAsString;
            networkDeviceInfoBar.Message = resourceMap.GetValue("NetworkAccessDeniedMsg", resourceContext).ValueAsString;
        }

        internal void LocalAccessNetworkAccessMessage()
        {
            networkDeviceInfoBar.IsOpen = true;
            networkDeviceInfoBar.Severity = InfoBarSeverity.Informational;
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            networkDeviceInfoBar.Title = resourceMap.GetValue("NetworkAccessLocalTitle", resourceContext).ValueAsString;
            networkDeviceInfoBar.Message = resourceMap.GetValue("NetworkAccessLocalMsg", resourceContext).ValueAsString;

        }

        internal void InternetAccessNetworkAccessMessage()
        {
            networkDeviceInfoBar.IsOpen = true;
            networkDeviceInfoBar.Severity = InfoBarSeverity.Success;
            networkDeviceInfoBarLearnmore.Visibility = Visibility.Visible;
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            networkDeviceInfoBar.Title = resourceMap.GetValue("NetworkAccessInternetTitle", resourceContext).ValueAsString;
            networkDeviceInfoBar.Message = resourceMap.GetValue("NetworkAccessInternetMsg", resourceContext).ValueAsString;
            networkDeviceInfoBarLearnmore.Content = resourceMap.GetValue("unable_to_detect_devices", resourceContext).ValueAsString;
            string url_of_firewallerror_msg = resourceMap.GetValue("url_of_success_but_unable_to_detect_msg", resourceContext).ValueAsString;
            networkDeviceInfoBarLearnmore.NavigateUri = new Uri(url_of_firewallerror_msg);
        }

        internal void InitUI()
        {
            //TitleTxt.Text = "Other Computers";
            LoadLocalizedStrings();
            this.RefreshDeviceList();
            this.Loaded += Page_Loaded;

        }

        private void LoadLocalizedStrings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView

            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            this.TitleTxt.Text = resourceMap.GetValue("ComputersTitle/Text", resourceContext).ValueAsString;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitUI();

            // 只有第一次加载时执行 RefreshDeviceList
            if (_isFirstLoad)
            {
                this.SearchForServer();
                //this.RefreshDeviceList();
                _isFirstLoad = false;
            }

            // 创建并启动定时器，每30秒刷新一次设备列表
            //_refreshTimer = new DispatcherTimer();
            //_refreshTimer.Tick += ResyncDevicesAsync;
            //_refreshTimer.Interval = TimeSpan.FromSeconds(10);
            //_refreshTimer.Start();

            //_progressTimer = new DispatcherTimer();
            //_progressTimer.Interval = TimeSpan.FromSeconds(3);
            //_progressTimer.Tick += showProgressBar_Tick;
            
        }

        //private void showProgressBar_Tick(object sender, object e) {
        //    //syncProgressDevices.IsIndeterminate = false;
        //    _progressTimer.Stop();
        //}

        //private async void ResyncDevicesAsync(object sender, object e)
        //{
        //    //syncProgressDevices.ShowPaused = false;
        //    //_progressTimer.Start();
        //    //syncProgressDevices.IsIndeterminate = true;
        //    //await App.discoveryClient.SendDiscoveryMessageAsync(App._server._serverIp, App._server._serverNickname);
        //    // TODO: 服务器每30秒发送请求检查设备是否在线
            
        //}

        internal void TestComputers()
        {
            DeviceGrids.ItemsSource = new ObservableCollection<Modules.OtherComputersGrid>
            {
                new Modules.OtherComputersGrid("PC1", "1.1.1.1"),
                new Modules.OtherComputersGrid("PC2", "2.1.1.1"),
                new Modules.OtherComputersGrid("PC3", "3.1.1.1"),
                new Modules.OtherComputersGrid("PC4", "4.1.1.1"),
            };
        }

        public void RefreshDeviceList()
        {
            //DeviceGrids.ItemsSource = null;
            dispatcherQueue.TryEnqueue(() =>
            {
                //messageTextBlock.Text = serverMessage;
                try
                {
                    DeviceGrids.ItemsSource = null;
                    DeviceGrids.ItemsSource = App._server._discoveredDevices;
                }
                catch (Exception ex) {
                    
                }
                

            });
            
            
        }

        internal void SearchForServer()
        {
            
            App._server.DevicesUpdated += RefreshDeviceList;
            App.discoveryClient.DevicesUpdated += RefreshDeviceList;


        }
        
        public void DeviceGrid_SelectItem(object sender, RoutedEventArgs e) 
        {
            if (sender is FrameworkElement element && element.DataContext is OtherComputersGrid tappedItem)
            {
                // 处理点击事件，并访问 tappedItem 对象
                //var deviceName = tappedItem.deviceName;
                //var deviceIP = tappedItem.deviceIP;
                // 执行其他操作
                App.target_device = tappedItem;
                Frame.Navigate(typeof(SenderPage));
                App.mainWindow.navSwitchTo("Send");
            }
        }

    }
}

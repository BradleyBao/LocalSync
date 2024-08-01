using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Net;
using System.Net.Sockets;
using LocalSync;
using LocalSync.Modules;
using System.Collections.ObjectModel;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Windows.AppLifecycle;
using System.Threading;
using System.Reflection;
using LocalSync.Helper;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.System;
using Windows.Storage;
using System.Globalization;
using Windows.Globalization;
using Windows.ApplicationModel.Resources.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>

        public static TcpFileServer _server;
        public static UdpDiscoveryClient discoveryClient;
        public static OtherComputersGrid target_device;
        public static bool transferring_files = false;
        public static MainWindow mainWindow;
        public static ResourceContext resourceContext;


        public static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public static FileTransferManager fileTransferManager = new FileTransferManager(tcpPort: 5000, maxConcurrentTransfers: 5);

        public App()
        {
            this.InitializeComponent();
            
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Mutex _mutex = new Mutex(false, "LocalSyncApp");
            if (_mutex.WaitOne(0, false) == false)
            {
                App.Current.Exit();
                return;
            }
            LoadSettings();
            //SetLanguage("zh-CN");
            SetLanguage((string)localSettings.Values["Language"] ?? "en-US");
            mainWindow = new MainWindow();
            
            mainWindow.Activate();
            
            //LoadSettings();

            

            // Starting Server 
            //await StartServer();
            await Task.WhenAll(StartServer(), StartUdpDiscovery(), StartTransferFileReponsder());
        }

        public static void SetLanguage(string languageCode)
        {
            resourceContext = new Windows.ApplicationModel.Resources.Core.ResourceContext();
            //resourceContext.QualifierValues["Language"] = "en-US";
            resourceContext.QualifierValues["Language"] = languageCode;
            //var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
        }

        static async Task StartServer()
        {
            int port = 8080;
            int discoveryPort = 8888;
            string serverNickname = (string)localSettings.Values["PcName"] ?? System.Net.Dns.GetHostName();

            _server = new TcpFileServer(port, discoveryPort, serverNickname);
            

            if (!_server.IsRunning())
            {
                // Start Running Server if it is not started
                await _server.StartAsync();
            }
            
        }

        static async Task StartUdpDiscovery()
        {
            int discoveryPort = 8888;
            discoveryClient = new UdpDiscoveryClient(discoveryPort);
            await discoveryClient.SendHeartbeatAsync();
        }

        static async Task StartTransferFileReponsder()
        {
            await fileTransferManager.ReceiveFilesAsync();
        }

        public static void SendNotificationWithActivation(string title, string message, string pageName)
        {
            var toastContent = new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddArgument("page", pageName)
                .GetToastContent();

            var toastNotification = new ToastNotification(toastContent.GetXml());

            // 显示通知
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        }

        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
            {
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            }
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }

        private void LoadSettings()
        {
            
            var themeIndex = (int)(localSettings.Values["PageStyleIndex"] ?? 0);
        }



    }
}

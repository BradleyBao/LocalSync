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
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // 获取窗口句柄
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

                // 如果窗口最小化，将其还原
                ShowWindow(hwnd, SW_RESTORE);
                mainWindow?.Activate();
                mainWindow.navSwitchTo("Receive");
            };
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

            if(mainWindow == null)
            {
                mainWindow = new MainWindow();
                mainWindow.Activate();
            }

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
            toastNotification.Activated += NotificationHandle;

            // 显示通知
            ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotification);
            
        }

        private static void NotificationHandle(ToastNotification notification, object args) 
        {
            // 获取窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);

            // 如果窗口最小化，将其还原
            ShowWindow(hwnd, SW_RESTORE);
            mainWindow?.Activate();
            mainWindow.navSwitchTo("Receive");
        }

        const int SW_RESTORE = 9;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

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

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true; // Prevent the application from crashing
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.SetObserved();
        }

        private async void HandleException(Exception ex)
        {
            if (ex != null)
            {
                string logFilePath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "CrashReport.log");
                string logContent = $"[{DateTime.Now}] {ex.ToString()}\n";
                await System.IO.File.AppendAllTextAsync(logFilePath, logContent);
                ContentDialog dialog = new ContentDialog();
                dialog.XamlRoot = mainWindow.Content.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.Title = "Application Crashed! ";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = $"Crash information has been logged in {logFilePath}. You may restart the app. ";

                _ = await dialog.ShowAsync();
            }
        }

    }
}

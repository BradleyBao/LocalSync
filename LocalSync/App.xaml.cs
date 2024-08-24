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
using System.Threading.Tasks;
using LocalSync.Modules;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;
using Windows.Storage;
using Windows.Globalization;
using Windows.ApplicationModel.Resources.Core;
using NetFwTypeLib;


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

        // IMPORTANT SETTING 
        public static int discoveryPort = 8888;
        public static int hostPort = 8080;
        public static int transferPort = 5000;
        public static int testPort = 9999;

        
        public static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public static FileTransferManager fileTransferManager;

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
            SetLanguage((string)localSettings.Values["Language"] ?? GetAppLanguage());

            if(mainWindow == null)
            {
                mainWindow = new MainWindow();
                mainWindow.Activate();
            }
            string serverNickname = (string)localSettings.Values["PcName"] ?? System.Net.Dns.GetHostName();
            await Task.WhenAll(StartServer(), StartUdpDiscovery(), StartTransferFileReponsder());
        }

        public static string GetAppLanguage()
        {
            // Supported Language List
            var supportedLanguages = new List<string> { "en-US", "zh-CN" };

            // First Choice Language of this device 
            var systemLanguage = ApplicationLanguages.Languages[0];

            // Is supported? 
            if (supportedLanguages.Contains(systemLanguage))
            {
                return systemLanguage; // Return if supported
            }
            else
            {
                // If not supported
                return "en-US"; // Return English
            }
        }

        public static void SetLanguage(string languageCode)
        {
            resourceContext = new Windows.ApplicationModel.Resources.Core.ResourceContext();
            if (languageCode.Equals("auto"))
            {
                languageCode = GetAppLanguage(); 
            }
            resourceContext.QualifierValues["Language"] = languageCode;
            //var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");


        }

        public static bool IsFirewallRuleAllowed()
        {
            string appPath = Process.GetCurrentProcess().MainModule.FileName;
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            foreach (INetFwRule rule in firewallPolicy.Rules)
            {
                // 检查规则是否与您的应用程序相关
                if (rule.ApplicationName != null && rule.ApplicationName.Equals(appPath, StringComparison.OrdinalIgnoreCase))
                {
                    // 检查规则是否允许流量
                    if (rule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static async Task StartServer()
        {
            string serverNickname = (string)localSettings.Values["PcName"] ?? System.Net.Dns.GetHostName();
            _server = new TcpFileServer(App.hostPort, App.discoveryPort, serverNickname);
            
            if (!_server.IsRunning())
            {
                // Start Running Server if it is not started
                await _server.StartAsync();
            }
            
        }

        static async Task StartUdpDiscovery()
        {
            discoveryClient = new UdpDiscoveryClient(App.discoveryPort);
            await discoveryClient.SendHeartbeatAsync();
        }

        static async Task StartTransferFileReponsder()
        {
            fileTransferManager = new FileTransferManager(tcpPort: App.transferPort, maxConcurrentTransfers: 5); 
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
            int.TryParse((string)localSettings.Values["ServerPort"] ?? "8080", out int serverPort);
            App.hostPort = serverPort;

            int.TryParse((string)localSettings.Values["DiscoverPort"] ?? "8888", out int discoverPort);
            App.discoveryPort = discoverPort;

            int.TryParse((string)localSettings.Values["TransferPort"] ?? "5000", out int transferPort);
            App.transferPort = transferPort;
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Microsoft.UI;
using LocalSync.Helper;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using LocalSync.Modules;
using System.Reflection;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 


    public sealed partial class SettingPage : Page
    {
        public SettingPage()
        {
            this.InitializeComponent();
            this.InitUI();
            this.LoadSettings();
        }

        internal void InitUI()
        {
            // TODO: Setup Setting
            //TitleTxt.Text = "Setting"; 
            this.LoadLocalizedStrings();
            InitIcon();
            VersionInfo.Text = GetAppVersion();
        }

        private string GetAppVersion()
        {
            // 获取应用程序的Assembly版本信息
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private void InitIcon()
        {
            FontIcon deviceNameIcon = new FontIcon();
            deviceNameIcon.Glyph = "\uE70C";
            pcNameSettingCard.HeaderIcon = deviceNameIcon;

            FontIcon languageIcon = new FontIcon();
            languageIcon.Glyph = "\uF2B7";
            LanguageSettingCard.HeaderIcon = languageIcon;

            FontIcon savedPathIcon = new FontIcon();
            savedPathIcon.Glyph = "\uE838";
            savePathFolderSettingCard.HeaderIcon = savedPathIcon;
        }

        private void LoadLocalizedStrings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView

            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            this.TitleTxt.Text = resourceMap.GetValue("SettingTitle/Text", resourceContext).ValueAsString;
            this.pcNameSettingCard.Header = resourceMap.GetValue("pcNameHeadingUid/Header", resourceContext).ValueAsString;
            this.pcNameSettingCard.Description = resourceMap.GetValue("pcNameHeadingUid/Description", resourceContext).ValueAsString;
            this.savePathFolderSettingCard.Header = resourceMap.GetValue("savePathFolderHeadingUid/Header", resourceContext).ValueAsString;
            this.savePathFolderSettingCard.Description = resourceMap.GetValue("savePathFolderHeadingUid/Description", resourceContext).ValueAsString;
            this.LanguageSettingCard.Header = resourceMap.GetValue("LanguageSettingHeadingUid/Header", resourceContext).ValueAsString;
            this.LanguageSettingCard.Description = resourceMap.GetValue("LanguageSettingHeadingUid/Description", resourceContext).ValueAsString;
            this.PickSavedPath.Content = resourceMap.GetValue("PickSavedPath_Uid/Content", resourceContext).ValueAsString;
            this.ResetPickSavedPath.Content = resourceMap.GetValue("ResetPickSavedPath_Uid/Content", resourceContext).ValueAsString;
            this.PickSavedPathTextOutput.Header = resourceMap.GetValue("ResetSavedPathHint", resourceContext).ValueAsString; 
            this.AboutSettingHeader.Text = resourceMap.GetValue("AboutSettingHeaderUID/Text", resourceContext).ValueAsString; 
        }

        private void LoadSettings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            var localSettings = ApplicationData.Current.LocalSettings;
            PcNameTextBox.Text = (string)localSettings.Values["PcName"] ?? string.Empty;
            savePathFolderSettingCard.Description = resourceMap.GetValue("currentSavedPath", resourceContext).ValueAsString + (string)localSettings.Values["SaveFolderPath"]+"\\LocalSync Transfer" ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+"\\LocalSync Transfer";

            chooseLanguageComboBox((string)localSettings.Values["Language"] ?? "en-US"); 
            
            
            //ReceiveFilePortTextBox.Text = (string)localSettings.Values["ReceiveFilePort"] ?? "DefaultPort";
            //BroadcastPortTextBox.Text = (string)localSettings.Values["BroadcastPort"] ?? "DefaultPort";
        }

        private void chooseLanguageComboBox(string tagName)
        {
            LanguageComboBox.SelectionChanged -= LanguageComboBox_SelectionChanged;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                string tag = item.Tag?.ToString(); // 获取 ComboBoxItem 的 Tag 属性
                if (tag != null && tag.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                {
                    LanguageComboBox.SelectedItem = item; // 设置选择项为当前遍历到的项
                    break; // 找到匹配的项后退出循环
                }
            }

            LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
        }

        private void PcNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 提供一个接口或方法来更改PC名字
            string newPcName = PcNameTextBox.Text;
            App._server._serverNickname = newPcName; 
            SaveSetting("PcName", newPcName);
        }

        private void ResetPickSavedPath_Click(object sender, RoutedEventArgs e)
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            string savedFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            savePathFolderSettingCard.Description = resourceMap.GetValue("ResetSavedPathHintHeader", resourceContext).ValueAsString + savedFolderPath + "\\LocalSync Transfer";
            PickSavedPathTextOutput.Header = resourceMap.GetValue("ResetSavedPathSuccess", resourceContext).ValueAsString;
            App.fileTransferManager.savedFolderPath = savedFolderPath;
            ReceivePage.savedFolderPath = savedFolderPath;
            SaveSetting("SaveFolderPath", savedFolderPath);
        }


        private async void PickSavedPath_Click(object sender, RoutedEventArgs e)
        {

            // Create a folder picker
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

            // See the sample code below for how to make the window accessible from the App class.
            var window = App.mainWindow;

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the folder picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your folder picker
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
                var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                savePathFolderSettingCard.Description = resourceMap.GetValue("ResetSavedPathHintHeader", resourceContext).ValueAsString + folder.Path + "\\LocalSync Transfer";
                string newSavedPath = folder.Path;
                App.fileTransferManager.savedFolderPath = newSavedPath;
                ReceivePage.savedFolderPath = newSavedPath;
                SaveSetting("SaveFolderPath", newSavedPath);
            }
            else
            {
                
            }
        }

        private void ReceiveFilePortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 提示用户更改接收文件端口可能导致设备识别不出
            //string newPort = ReceiveFilePortTextBox.Text;
            //SaveSetting("ReceiveFilePort", newPort);
        }

        private void BroadcastPortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 提示用户更改广播端口可能导致设备识别不出
            //string newPort = BroadcastPortTextBox.Text;
            //SaveSetting("BroadcastPort", newPort);
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedLanguage = comboBox.SelectedItem as ComboBoxItem;
            string languageCode = selectedLanguage.Tag.ToString();

            App.SetLanguage(languageCode);

            // Reload the application or notify the user to restart the application
            
            SaveSetting("Language", languageCode);
            //LanguageComboBox.Text = languageCode;

            //string title = "";
            //string content = ""; 

            //switch (languageCode)
            //{
            //    case "en-US":
            //        title = "Restart Required";
            //        content = "Please restart the application to apply the language change."; 
            //        break;

            //    case "zh-CN":
            //        title = "需要重新启动应用";
            //        content = "部分设置可能需要重新启动应用";
            //        break;

            //    default:
            //        title = "Restart Required";
            //        content = "Please restart the application to apply the language change, and the language you choosed is not identified? ";
            //        break;
            //}

            //ContentDialog dialog = new ContentDialog();

            //// XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            //dialog.XamlRoot = this.XamlRoot;
            //dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            //dialog.Title = title;
            //dialog.PrimaryButtonText = "OK";
            //dialog.DefaultButton = ContentDialogButton.Primary;
            //dialog.Content = content;

            //_ = await dialog.ShowAsync();

            App.mainWindow.ReloadLanguage();

            App.mainWindow.navSwitchTo("Home"); 

        }

        private void SaveSetting(string key, string value)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
        }
    }
}

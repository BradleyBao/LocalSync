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
using Windows.Globalization;

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
            ClickSavePortChangeSettingCardBtn.IsEnabled = false;
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

            FontIcon allPortIcon = new FontIcon();
            allPortIcon.Glyph = "\uEA37";
            allPortSettingCard.HeaderIcon = allPortIcon;

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
            this.GeneralSettingHeader.Text = resourceMap.GetValue("GeneralSettingHeaderUid/Text", resourceContext).ValueAsString; 
            this.allPortSettingCard.Header = resourceMap.GetValue("allPortSettingCardUid/Header", resourceContext).ValueAsString; 
            this.allPortSettingCard.Description = resourceMap.GetValue("allPortSettingCardUid/Description", resourceContext).ValueAsString; 
            this.portChangeWarning.Title = resourceMap.GetValue("portChangeWarningUid/Title", resourceContext).ValueAsString; 
            this.AdvancedSettingHeader.Text = resourceMap.GetValue("AdvancedSettingHeaderUid/Text", resourceContext).ValueAsString; 
            this.serverPortSettingCard.Header = resourceMap.GetValue("serverPortSettingCardUid/Header", resourceContext).ValueAsString; 
            this.serverPortSettingCard.Description = resourceMap.GetValue("serverPortSettingCardUid/Description", resourceContext).ValueAsString;
            this.discoveryPortSettingCard.Header = resourceMap.GetValue("discoveryPortSettingCardUid/Header", resourceContext).ValueAsString;
            this.discoveryPortSettingCard.Description = resourceMap.GetValue("discoveryPortSettingCardUid/Description", resourceContext).ValueAsString;
            this.transferPortSettingCard.Header = resourceMap.GetValue("transferPortSettingCardUid/Header", resourceContext).ValueAsString;
            this.transferPortSettingCard.Description = resourceMap.GetValue("transferPortSettingCardUid/Description", resourceContext).ValueAsString;
            this.ClickSavePortChangeSettingCard.Header = resourceMap.GetValue("ClickSavePortChangeSettingCardUid/Header", resourceContext).ValueAsString;
            this.ClickSavePortChangeSettingCard.Description = resourceMap.GetValue("ClickSavePortChangeSettingCardUid/Description", resourceContext).ValueAsString;
            portChangeWarningHyperLink.Content = resourceMap.GetValue("LearnMore", resourceContext).ValueAsString;
            string url_of_firewallerror_msg = resourceMap.GetValue("url_of_change_port_setting_msg", resourceContext).ValueAsString;
            portChangeWarningHyperLink.NavigateUri = new Uri(url_of_firewallerror_msg);

            this.ClickSavePortChangeSettingCardBtn.Content = resourceMap.GetValue("Save", resourceContext).ValueAsString;
            this.ResetSavePortChangeSettingCardBtn.Content = resourceMap.GetValue("Reset", resourceContext).ValueAsString;

            this.ResetSavePortChangeSettingCard.Header = resourceMap.GetValue("ResetSavePortChangeSettingCardUid/Header", resourceContext).ValueAsString;
            this.ResetSavePortChangeSettingCard.Description = resourceMap.GetValue("ResetSavePortChangeSettingCardUid/Description", resourceContext).ValueAsString;

        }

        private void LoadSettings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            var localSettings = ApplicationData.Current.LocalSettings;
            PcNameTextBox.Text = (string)localSettings.Values["PcName"] ?? string.Empty;
            savePathFolderSettingCard.Description = resourceMap.GetValue("currentSavedPath", resourceContext).ValueAsString + (string)localSettings.Values["SaveFolderPath"]+"\\LocalSync Transfer" ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+"\\LocalSync Transfer";
            
            int.TryParse((string)localSettings.Values["ServerPort"] ?? "8080", out int serverPort);
            serverPortTxt.Value = (double)serverPort;

            int.TryParse((string)localSettings.Values["DiscoverPort"] ?? "8888", out int discoverPort);
            discoveryPortTxt.Value = (double)discoverPort;

            int.TryParse((string)localSettings.Values["TransferPort"] ?? "5000", out int transferPort);
            transferPortTxt.Value = (double)transferPort;

            chooseLanguageComboBox((string)localSettings.Values["Language"] ?? "auto"); 
            
            
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

        private void ClickSavePort_Click(object sender, RoutedEventArgs e)
        {
            string serverPort = ((int)serverPortTxt.Value).ToString();
            string discoverPort = ((int)discoveryPortTxt.Value).ToString(); 
            string transferPort = ((int)transferPortTxt.Value).ToString();

            SaveSetting("ServerPort", serverPort);
            SaveSetting("DiscoverPort", discoverPort);
            SaveSetting("TransferPort", transferPort);

            ClickSavePortChangeSettingCardBtn.IsEnabled = false;
            App.hostPort = (int)serverPortTxt.Value;
            App.discoveryPort = (int)discoveryPortTxt.Value;
            App.transferPort = (int)transferPortTxt.Value;
            ClickSavePortDialog();
        }

        private void ResetSavePort_Click(Object sender, RoutedEventArgs e)
        {
            SaveSetting("ServerPort", "8080");
            SaveSetting("DiscoverPort", "8888");
            SaveSetting("TransferPort", "5000");
            serverPortTxt.Value = 8080;
            discoveryPortTxt.Value = 8888;
            transferPortTxt.Value = 5000;
            ClickSavePortDialog();
        }

        internal async void ClickSavePortDialog()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = resourceMap.GetValue("ClickSavePortDialogTitle", resourceContext).ValueAsString; ;
            dialog.PrimaryButtonText = "OK";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = resourceMap.GetValue("ClickSavePortDialogContent", resourceContext).ValueAsString; ;

            _ = await dialog.ShowAsync();
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

            App.mainWindow.ReloadLanguage();
            App.mainWindow.navSwitchTo("Home"); 
        }

        private void SaveSetting(string key, string value)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
        }

        internal bool IsPortNotChanged()
        {
            if (serverPortTxt != null && discoveryPortTxt != null && transferPortTxt != null)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                string serverPortValue = (string)localSettings.Values["ServerPort"] ?? "8080";
                string serverPortValueNew = ((int)serverPortTxt.Value).ToString();

                string discoverPortValue = (string)localSettings.Values["DiscoverPort"] ?? "8888";
                string discoverPortValueNew = ((int)discoveryPortTxt.Value).ToString();

                string transferFileportValue = (string)localSettings.Values["TransferPort"] ?? "5000";
                string transferFileportValueNew = ((int)transferPortTxt.Value).ToString();

                return serverPortValue.Equals(serverPortValueNew) && discoverPortValue.Equals(discoverPortValueNew) && transferFileportValue.Equals(transferFileportValueNew);
            }
            return true;
        }

        internal bool IsPortConflicted()
        {
            if (serverPortTxt != null && discoveryPortTxt != null && transferPortTxt != null)
            {
                string serverPortValueNew = ((int)serverPortTxt.Value).ToString();
                string discoverPortValueNew = ((int)discoveryPortTxt.Value).ToString();
                string transferFileportValueNew = ((int)transferPortTxt.Value).ToString();
                return serverPortValueNew.Equals(discoverPortValueNew) && discoverPortValueNew.Equals(transferFileportValueNew); 
            }
            return false;
        }

        internal void PortTxtChange()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");

            if (!IsPortNotChanged() && ClickSavePortChangeSettingCardBtn != null)
            {
                ClickSavePortChangeSettingCardBtn.IsEnabled = true;
                portChangeWarning.Title = resourceMap.GetValue("PortUserChangedTitle", resourceContext).ValueAsString;
                portChangeWarning.Severity = InfoBarSeverity.Informational;
                portChangeWarningHyperLink.Visibility = Visibility.Collapsed;
            } else
            {
                portChangeWarning.Title = resourceMap.GetValue("portChangeWarningUid/Title", resourceContext).ValueAsString;
                portChangeWarning.Severity = InfoBarSeverity.Warning;
                portChangeWarningHyperLink.Visibility = Visibility.Visible;
            }
        }

        private void serverPortTxt_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            PortTxtChange();
        }

        private void discoveryPortTxt_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            PortTxtChange();
        }

        private void transferPortTxt_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            PortTxtChange();
        }
    }
}

using LocalSync.Modules;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using static System.Net.WebRequestMethods;
using Windows.ApplicationModel.Resources;
using System.ComponentModel.DataAnnotations;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SenderPage : Page
    {

        public static ObservableCollection<Modules.File> file_send_list = new ObservableCollection<Modules.File>();
        public static ObservableCollection<Modules.Folder> folder_send_list = new ObservableCollection<Modules.Folder>();
        

        public SenderPage()
        {
            this.InitializeComponent();
            this.InitUI();
        }

        private async void startTransfer(object sender, RoutedEventArgs e)
        {
            App.transferring_files = true;
            App.mainWindow.setStatusNav(false); 
            transferStatus.ShowPaused = false;
            transferStatus.IsIndeterminate = false;
            OtherComputersGrid targetDeivce = App.target_device;
            string targetIp = targetDeivce.deviceIP;

            List<string> path_files = file_send_list.Select(file => file.GetFilePath()).ToList();
            List<string> path_folders = folder_send_list.Select(folder => folder.GetFolderPath()).ToList();


            List<string> paths = path_files.Concat(path_folders).ToList();
            var progress = new Progress<double>(value =>
            {
                transferStatus.Value = value;
            });
            startBtn.IsEnabled = false;
            await App.fileTransferManager.TransferFilesAndFoldersAsync(targetIp, paths, progress);
            startBtn.IsEnabled = true;
            transferStatus.IsIndeterminate = false;
            App.mainWindow.setStatusNav(true);
            App.mainWindow.senderNotification();
        }

        private async void pickFolders(object sender, RoutedEventArgs e)
        {
            pickFilesOutput.Text = "";
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();
            var window = App.mainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your folder picker
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                pickFilesOutput.Text = "Picked folder: " + folder.Name;
                this.AddFolderToList(folder);
            }
            else
            {
                pickFilesOutput.Text = "Operation cancelled.";
            }

            
        }

        private async void pickFiles(object sender, RoutedEventArgs e)
        {
            pickFilesOutput.Text = "";
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            var window = App.mainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add("*");
            IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                StringBuilder output = new StringBuilder("Follwing Files have been added:\n");
                foreach (StorageFile file in files)
                {
                    output.Append(file.Name + "\n");
                }
                pickFilesOutput.Text = output.ToString();
                this.AddFilesToList(files);
            }
            else
            {
                pickFilesOutput.Text = "Operation cancelled.";
            }

            

        }

        internal void AddFilesToList(IReadOnlyList<StorageFile> files)
        {
            foreach (StorageFile file in files)
            {
                FileInfo this_file_info = new FileInfo(file.Path);
                DateTime this_file_last_modified_time = this_file_info.LastWriteTime;
                Modules.File this_file = new Modules.File(System.IO.Path.GetFileName(file.Path),
                    this_file_last_modified_time,
                    System.IO.Path.GetExtension(file.Path),
                    this_file_info.Length, file.Path);
                file_send_list.Add(this_file);
            }

            ObservableCollection<LocalSync.Modules.DataType> dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(
                file_send_list.Cast<LocalSync.Modules.DataType>().Concat(folder_send_list.Cast<LocalSync.Modules.DataType>())
            );

            UnifiedListView.ItemsSource = dataTypeList;
        }

        internal void AddFolderToList(StorageFolder folder)
        {
            DirectoryInfo this_folder_info = new DirectoryInfo(folder.Path);
            string name = System.IO.Path.GetFileNameWithoutExtension(folder.Path);
            Folder this_folder = new Folder(name, this_folder_info.LastWriteTime, folder.Path);
            folder_send_list.Add(this_folder);

            ObservableCollection<LocalSync.Modules.DataType> dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(
                file_send_list.Cast<LocalSync.Modules.DataType>().Concat(folder_send_list.Cast<LocalSync.Modules.DataType>())
            );

            UnifiedListView.ItemsSource = dataTypeList;
        }

        internal void InitUI()
        {
            //TitleTxt.Text = "Transfer Files";
            LoadLocalizedStrings();
            senderDeviceIcon.Glyph = "\uE7F8";
            receiverDeviceIcon.Glyph = "\uE7F8";
            senderDeviceName.Text = App._server._serverNickname;
            //receiverDeviceName.Text = "Not Set";
            transferStatus.ShowPaused = true;
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");

            if (App.target_device != null)
            {
                // Handle File Transfer
                startBtn.IsEnabled = true;
                if (App.transferring_files)
                {
                    transferStatus.ShowPaused = false;
                    transferStatus.IsIndeterminate = false;
                    startBtn.IsEnabled = false;
                    transferStatus.IsIndeterminate = true;
                } else
                {
                    transferStatus.ShowPaused = true;
                }
                receiverDeviceName.Text = App.target_device.deviceName;
                transferInfoBar.Visibility = Visibility.Collapsed;
                //this.transferInfoBar.Title = resourceMap.GetValue("transferInfoBarUID/Title", resourceContext).ValueAsString;
                //this.transferInfoBar.Message = resourceMap.GetValue("transferInfoBarUID/Message", resourceContext).ValueAsString;
            } else
            {
                startBtn.IsEnabled = false;
                transferInfoBar.Severity = InfoBarSeverity.Warning; 
                //transferInfoBar.Title = "Device Not Selected";
                //transferInfoBar.Message = "Please go to Computers to setup target device. ";
                this.transferInfoBar.Title = resourceMap.GetValue("transferInfoBarUID/Title", resourceContext).ValueAsString;
                this.transferInfoBar.Message = resourceMap.GetValue("transferInfoBarUID/Message", resourceContext).ValueAsString;
            }

            

            if (file_send_list != null || folder_send_list != null)
            {
                ObservableCollection<LocalSync.Modules.DataType> dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(
                file_send_list.Cast<LocalSync.Modules.DataType>().Concat(folder_send_list.Cast<LocalSync.Modules.DataType>())
                );
                UnifiedListView.ItemsSource = dataTypeList;
            }

        }

        // TODO
        private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (UnifiedListView.SelectedItems != null)
            {

                foreach (Modules.DataType selectedItem in UnifiedListView.SelectedItems)
                {
                    foreach (Modules.File eachFile in file_send_list)
                    {
                        if (eachFile.Name == selectedItem.Name) {
                            file_send_list.Remove(eachFile);
                            break;
                        }
                    }

                    foreach (Modules.Folder eachFolder in folder_send_list)
                    {
                        if (eachFolder.Name == selectedItem.Name)
                        {
                            folder_send_list.Remove(eachFolder);
                            break;
                        }
                    }
                }

                ObservableCollection<LocalSync.Modules.DataType> dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(
                file_send_list.Cast<LocalSync.Modules.DataType>().Concat(folder_send_list.Cast<LocalSync.Modules.DataType>())
                );
                UnifiedListView.ItemsSource = dataTypeList;


            }

            

        }

        private void LoadLocalizedStrings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            this.TitleTxt.Text = resourceMap.GetValue("SenderTitle/Text", resourceContext).ValueAsString;
            this.receiverDeviceName.Text = resourceMap.GetValue("RecvDeviceNameText/Text", resourceContext).ValueAsString;
            this.startBtn.Content = resourceMap.GetValue("startBtnUid/Content", resourceContext).ValueAsString;
            this.ChooseDropDownButton.Content = resourceMap.GetValue("ChooseDropDownButtonUid/Content", resourceContext).ValueAsString;
            this.pickFilesBtn.Text = resourceMap.GetValue("pickFilesBtnUid/Text", resourceContext).ValueAsString;
            this.pickFoldersBtn.Text = resourceMap.GetValue("pickFoldersBtnUid/Text", resourceContext).ValueAsString;
            //this.FolderListViewHeading.Text = resourceMap.GetValue("FolderListViewHeadingUid/Text", resourceContext).ValueAsString;
            this.FileListViewHeading.Text = resourceMap.GetValue("RecvFileHeading/Text", resourceContext).ValueAsString;
            
        }



    }
}

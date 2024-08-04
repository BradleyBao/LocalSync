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
using LocalSync.Modules;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using System.Security.AccessControl;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Data;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class ReceivePage : Page
    {
        // Current Folder Path
        private string current_folder_path;
        private ObservableCollection<Modules.File> file_list = new ObservableCollection<Modules.File>();
        private ObservableCollection<Modules.Folder> folder_list = new ObservableCollection<Modules.Folder>();
        private ObservableCollection<LocalSync.Modules.DataType> dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(); 
        public static string savedFolderPath = (string)App.localSettings.Values["SaveFolderPath"] ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public ReceivePage()
        {
            this.InitializeComponent();
            this.InitUI();
        }

        internal void InitUI()
        {
            //TitleTxt.Text = "Received Files";
            LoadLocalizedStrings();
            this.SetupIcon();
            this.InitGetAllFiles();
        }

        private void LoadLocalizedStrings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView

            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            this.TitleTxt.Text = resourceMap.GetValue("RecvTitle/Text", resourceContext).ValueAsString;
            //this.FolderListViewHeading.Text = resourceMap.GetValue("RecvFolderHeading/Text", resourceContext).ValueAsString;
            this.FileListViewHeading.Text = resourceMap.GetValue("RecvFileHeading/Text", resourceContext).ValueAsString;
            this.OpenContainingFolder.Label = resourceMap.GetValue("OpenContainingFolderUid/Label", resourceContext).ValueAsString;
            this.DeleteFiles.Label = resourceMap.GetValue("DeleteFilesUid/Label", resourceContext).ValueAsString;
            this.DeleteFileInfoBar.Title = resourceMap.GetValue("DeleteFileInfoBar_Uid/Title", resourceContext).ValueAsString;
            this.DeleteFileInfoBar.Message = resourceMap.GetValue("DeleteFileInfoBar_Uid/Message", resourceContext).ValueAsString;
        }

        internal void CreateSharedFolder(string folderPath, string shareName)
        {

            // TODO Create a shared folder in the local Lan 

            string netShareCommand = $"net share {shareName}={folderPath} /grant:everyone,full";

            ExecuteCommand(netShareCommand); 
        }

        private static void ExecuteCommand(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C {command}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Sync Folder has been created
        }

        internal void InitGetAllFiles()
        {
            string folderPath = Path.Combine(savedFolderPath, "LocalSync Transfer");
            this.current_folder_path = folderPath;

            // Check if folder already exists
            if (!Directory.Exists(folderPath)) {
                // If not, create folder
                Directory.CreateDirectory(folderPath);
            }

            // Create Files and add to ListView 
            // Add Files to List
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                FileInfo this_file_info = new FileInfo(file);
                DateTime this_file_last_modified_time = this_file_info.LastWriteTime;
                Modules.File this_file = new Modules.File(Path.GetFileName(file),
                    this_file_last_modified_time,
                    Path.GetExtension(file), 
                    this_file_info.Length, file);
                file_list.Add(this_file);
            }

            

            // Create Folders and add to ListView 
            // Add Folders to List folder_list
            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders) {
                DirectoryInfo this_folder_info = new DirectoryInfo(subfolder);
                string name = Path.GetFileNameWithoutExtension(subfolder); 
                Folder this_folder = new Folder(name, this_folder_info.LastWriteTime, subfolder);
                folder_list.Add(this_folder);
            }

            //this.AddFolderToList();
            this.AddFilesToList();

            this.CreateSharedFolder(folderPath, "LocalSyncFolder"); 
        }

        private void OpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            string path = this.current_folder_path; 
            
            Process.Start(new ProcessStartInfo()
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            }); 
        }

        internal void AddFilesToList()
        {
            dataTypeList.Clear();
            dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(
                file_list.Cast<LocalSync.Modules.DataType>().Concat(folder_list.Cast<LocalSync.Modules.DataType>())
            );
            FileList.ItemsSource = dataTypeList;
        }

        //internal void AddFolderToList()
        //{
        //    FolderListView.ItemsSource = folder_list;
        //}

        internal void SetupIcon()
        {
            // Grid View Icon
            FontIcon gridView_Icon = new FontIcon();
            gridView_Icon.Glyph = "\uF0E2";
            GridViewBtn.Icon = gridView_Icon;

            FontIcon openFolder_Icon = new FontIcon(); 
            openFolder_Icon.Glyph = "\uE838";
            OpenContainingFolder.Icon = openFolder_Icon;
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e) {
            if (FileList.SelectedItems != null)
            {
                foreach (Modules.DataType selectedItem in FileList.SelectedItems)
                {
                    foreach (Modules.File eachFile in file_list)
                    {
                        if (eachFile.Name == selectedItem.Name)
                        {
                            file_list.Remove(eachFile);
                            try
                            {
                                System.IO.File.Delete(eachFile.GetFilePath());
                            }
                            catch (Exception ex) {
                                DeleteFileInfoBar.IsOpen = true; 
                            }
                            
                            break;
                        }
                    }

                    foreach (Modules.Folder eachFolder in folder_list)
                    {
                        if (eachFolder.Name == selectedItem.Name)
                        {
                            folder_list.Remove(eachFolder);
                            try
                            {
                                Directory.Delete(eachFolder.GetFolderPath(), true);
                            }
                            catch (Exception ex) {
                                DeleteFileInfoBar.IsOpen = true;
                            }
                            break;
                        }
                    }
                }

                this.AddFilesToList();
                //this.UpdateLayout();

            }
        }

        internal void testFolder()
        {
            Address_Bar.ItemsSource = new ObservableCollection<Folder>
            {
                new Folder("TestFolder1", DateTime.Now, "C:/"),
                new Folder("TestFolder2", DateTime.Now, "C:/"),
                new Folder("TestFolder3", DateTime.Now, "C:/"),
                new Folder("TestFolder4", DateTime.Now, "C:/")
            };


            FileList.ItemsSource = new ObservableCollection<Modules.File>
            {
                new Modules.File("hello.txt", DateTime.Now, "Text File", 123, "C:/"),
                new Modules.File("hello1.txt", DateTime.Now, "Text File", 1234, "C:/"),
                new Modules.File("hello2.txt", DateTime.Now, "Text File", 1323, "C:/"),
                new Modules.File("hello3.txt", DateTime.Now, "Text File", 1423, "C:/"),
            };
        }

        private void UploadFiles_Enter(object sender, DragEventArgs e)
        {
            
        }

        private void UploadFiles_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }

        private async void UploadFiles(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is StorageFile file)
                    {
                        // Handle the dropped file (e.g., get its path)
                        string filePath = file.Path;
                        
                        // ...
                    }
                }
            }
        }



    }


}

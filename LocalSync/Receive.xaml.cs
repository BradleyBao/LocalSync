using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Linq;
using LocalSync.Modules;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Diagnostics;
using System.Data;

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
        private ObservableCollection<LocalSync.Modules.ExplorerItem> all_explorerItem = new ObservableCollection<ExplorerItem>();

        public ReceivePage()
        {
            this.InitializeComponent();
            this.InitUI();
        }

        internal void InitUI()
        {
            //TitleTxt.Text = "Received Files";
            LoadLocalizedStrings();
            OpenThisFile.IsEnabled = false;
            DeleteFiles.IsEnabled = false;
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
            this.OpenThisFile.Label = resourceMap.GetValue("OpenThisFileUid/Label", resourceContext).ValueAsString;
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

        private ObservableCollection<Modules.File> GetSubFiles(string folder_path)
        {

            ObservableCollection<Modules.File> this_folder_files = new ObservableCollection<Modules.File>();

            if (Directory.Exists(folder_path)) 
            {
                string[] files = Directory.GetFiles(folder_path, "*.*", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    FileInfo this_file_info = new FileInfo(file);
                    DateTime this_file_last_modified_time = this_file_info.LastWriteTime;
                    Modules.File this_file = new Modules.File(Path.GetFileName(file),
                        this_file_last_modified_time,
                        Path.GetExtension(file),
                        this_file_info.Length, file);
                    this_folder_files.Add(this_file);
                }
            }

            return this_folder_files;
        }

        private ObservableCollection<Modules.Folder> GetSubFolders(string folder_path)
        {
            ObservableCollection<Modules.Folder> this_folder_folders = new ObservableCollection<Modules.Folder>();
            if (Directory.Exists(folder_path))
            {
                string[] subfolders = Directory.GetDirectories(folder_path);
                foreach (string subfolder in subfolders)
                {
                    DirectoryInfo this_folder_info = new DirectoryInfo(subfolder);
                    string name = Path.GetFileNameWithoutExtension(subfolder);
                    Folder this_folder = new Folder(name, this_folder_info.LastWriteTime, subfolder);
                    this_folder_folders.Add(this_folder);
                }
            }

            return this_folder_folders;
        }

        private void UpdateAllFiles_Folders(string folder_path)
        {
            file_list = GetSubFiles(folder_path);
            folder_list = GetSubFolders(folder_path);

            AddFilesToList();
        }

        private ObservableCollection<Modules.ExplorerItem> GetSubFilesFolderItems(string folder_path)
        {
            ObservableCollection<Modules.ExplorerItem> this_file_folder_folders = new ObservableCollection<Modules.ExplorerItem>();
            if (Directory.Exists(folder_path))
            {
                string[] subfolders = Directory.GetDirectories(folder_path);
                foreach (string subfolder in subfolders)
                {
                    DirectoryInfo this_folder_info = new DirectoryInfo(subfolder);
                    string name = Path.GetFileNameWithoutExtension(subfolder);

                    ExplorerItem this_folder_tree = new ExplorerItem()
                    {
                        Name = name,
                        Path = subfolder,
                        Type = ExplorerItem.ExplorerItemType.Folder,
                        IsExpanded = HasChildren(subfolder),
                    };
                    this_file_folder_folders.Add(this_folder_tree);
                }

                //string[] files = Directory.GetFiles(folder_path, "*.*", SearchOption.TopDirectoryOnly);
                //foreach (string file in files)
                //{
                //    FileInfo this_file_info = new FileInfo(file);
                //    DateTime this_file_last_modified_time = this_file_info.LastWriteTime;
                //    ExplorerItem this_file_tree = new ExplorerItem()
                //    {
                //        Name = Path.GetFileName(file),
                //        Path = file,
                //        Type = ExplorerItem.ExplorerItemType.File,
                //        IsExpanded = false,
                //    };
                //    this_file_folder_folders.Add(this_file_tree);
                //}

            }

            return this_file_folder_folders;
        }

        internal void InitGetAllFiles()
        {
            string folderPath = Path.Combine(savedFolderPath, "LocalSync Transfer");
            this.current_folder_path = folderPath;
            ExplorerItem main_folder_tree = new ExplorerItem()
            {
                Name = "/",
                Path = this.current_folder_path,
                Type = ExplorerItem.ExplorerItemType.Folder,
                IsExpanded = true,
            };
            all_explorerItem.Add(main_folder_tree);

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

                //ExplorerItem this_file_tree = new ExplorerItem()
                //{
                //    Name = Path.GetFileName(file),
                //    Path = file,
                //    Loaded = false,
                //    Type = ExplorerItem.ExplorerItemType.File,
                //    IsExpanded = false,
                //};
                //main_folder_tree.Children.Add(this_file_tree);
            }

            

            // Create Folders and add to ListView 
            // Add Folders to List folder_list
            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders) {
                DirectoryInfo this_folder_info = new DirectoryInfo(subfolder);
                string name = Path.GetFileNameWithoutExtension(subfolder); 
                Folder this_folder = new Folder(name, this_folder_info.LastWriteTime, subfolder);
                folder_list.Add(this_folder);

                ExplorerItem this_folder_tree = new ExplorerItem()
                {
                    Name = name,
                    Path = subfolder,
                    Loaded = false,
                    Type = ExplorerItem.ExplorerItemType.Folder,
                    IsExpanded = HasChildren(subfolder),
                };
                main_folder_tree.Children.Add(this_folder_tree);
            }

            //this.AddFolderToList();
            this.AddFilesToList();
            FileTrees.ItemsSource = all_explorerItem;
            this.CreateSharedFolder(folderPath, "LocalSyncFolder"); 
        }

        public bool HasChildren(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                // 检查是否有子文件
                var hasFiles = Directory.GetFiles(folderPath).Length > 0;

                // 检查是否有子文件夹
                var hasDirectories = Directory.GetDirectories(folderPath).Length > 0;

                // 如果有子文件或子文件夹则返回true
                return hasFiles || hasDirectories;
            }
            else
            {
                return false;
                //throw new DirectoryNotFoundException($"The folder path '{folderPath}' does not exist.");
            }
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

        private void GoToPath(string path)
        {
            UpdateAllFiles_Folders(path);
            GetSubFilesFolderItems(path);
        }

        //internal void AddFolderToList()
        //{
        //    FolderListView.ItemsSource = folder_list;
        //}

        internal void SetupIcon()
        {
            // Grid View Icon
            //FontIcon gridView_Icon = new FontIcon();
            //gridView_Icon.Glyph = "\uF0E2";
            //GridViewBtn.Icon = gridView_Icon;

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
                                var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
                                var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                                this.FileManagerInfoBar.Title = resourceMap.GetValue("DeleteFileFailed_Title", resourceContext).ValueAsString;
                                this.FileManagerInfoBar.Message = resourceMap.GetValue("DeleteFileFailed_Message", resourceContext).ValueAsString;
                                FileManagerInfoBar.IsOpen = true; 
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
                                var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
                                var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                                this.FileManagerInfoBar.Title = resourceMap.GetValue("DeleteFileFailed_Title", resourceContext).ValueAsString;
                                this.FileManagerInfoBar.Message = resourceMap.GetValue("DeleteFileFailed_Message", resourceContext).ValueAsString;
                                FileManagerInfoBar.IsOpen = true;
                            }
                            break;
                        }
                    }
                }

                this.AddFilesToList();
                DeleteFiles.IsEnabled = false;
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

        private void FileTrees_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var selectedItem = args.InvokedItem as ExplorerItem;
            OpenThisFile.IsEnabled = false;

            if (selectedItem.Type is ExplorerItem.ExplorerItemType.Folder)
            {
                string path = selectedItem.Path;
                // If not loaded
                if (!selectedItem.Loaded)
                {
                    if (HasChildren(path))
                    {
                        selectedItem.Children.Clear(); // 先清空当前子项列表
                        var subItems = GetSubFilesFolderItems(path); // 获取子项
                        foreach (var item in subItems)
                        {
                            selectedItem.Children.Add(item);
                        }
                        selectedItem.IsExpanded = true; // 设置为已展开
                        selectedItem.Loaded = true;
                    }
                }
                UpdateAllFiles_Folders(path);
                //FileTrees.ItemsSource = null; // 这一步可以强制刷新TreeView
                //FileTrees.ItemsSource = all_explorerItem;
            }
        }

        private void FileList_ItemClick(object sender, TappedRoutedEventArgs e)
        {
            OpenThisFile.IsEnabled = true;
            DeleteFiles.IsEnabled = true;
        }

        private void Open_file_folder()
        {
            if (FileList.SelectedItems != null && FileList.SelectedItems.Count < 2)
            {
                foreach (Modules.DataType selectedItem in FileList.SelectedItems)
                {
                    foreach (Modules.File eachFile in file_list)
                    {
                        if (eachFile.Name == selectedItem.Name)
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = eachFile.GetFilePath(),
                                    UseShellExecute = true
                                });
                            }
                            catch (Exception ex)
                            {
                                var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
                                var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                                this.FileManagerInfoBar.Title = resourceMap.GetValue("DeleteFileFailed_Title", resourceContext).ValueAsString;
                                this.FileManagerInfoBar.Message = resourceMap.GetValue("DeleteFileFailed_Message", resourceContext).ValueAsString;
                                FileManagerInfoBar.IsOpen = true;
                            }
                            break;
                        }
                    }

                    foreach (Modules.Folder eachFolder in folder_list)
                    {
                        if (eachFolder.Name == selectedItem.Name)
                        {
                            try
                            {
                                GoToPath(eachFolder.GetFolderPath());
                                FindAndFocusExplorerItemByPath(all_explorerItem, eachFolder.GetFolderPath()); 
                            }
                            catch (Exception ex)
                            {
                                var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
                                var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                                this.FileManagerInfoBar.Title = resourceMap.GetValue("DeleteFileFailed_Title", resourceContext).ValueAsString;
                                this.FileManagerInfoBar.Message = resourceMap.GetValue("DeleteFileFailed_Message", resourceContext).ValueAsString;
                                FileManagerInfoBar.IsOpen = true;
                            }
                            break;
                        }
                    }
                }

                
            }
        }

        private void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Open_file_folder();
        }

        private void OpenThisFile_Click(object sender, RoutedEventArgs e)
        {
            Open_file_folder();
        }

        public ExplorerItem FindAndFocusExplorerItemByPath(ObservableCollection<ExplorerItem> items, string targetPath)
        {
            foreach (var item in items)
            {
                // 如果路径匹配
                if (item.Path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    // 更新UI相关代码，将item设为选中
                    // 假设你的TreeView控件名称为FileTrees
                    FileTrees.SelectedItem = item;
                    return item;
                }

                // 递归遍历子项
                if (item.Children != null && item.Children.Count > 0)
                {
                    var foundItem = FindAndFocusExplorerItemByPath(item.Children, targetPath);
                    if (foundItem != null)
                    {
                        // 展开父项以显示找到的子项
                        if (!item.Loaded)
                        {
                            item.Loaded = true;
                            // 通知UI更新
                            item.NotifyPropertyChanged(nameof(ExplorerItem.Children));
                        }

                        // 返回找到的项
                        return foundItem;
                    }
                }
            }
            return null; // 如果未找到
        }


    }


}

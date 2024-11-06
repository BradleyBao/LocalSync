using LocalSync.Helper;
using Microsoft.UI;
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
using System.Collections;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LocalSync
{
    public sealed partial class HomePage : Page
    {
        private string current_folder_path;
        public static string savedFolderPath = (string)App.localSettings.Values["SaveFolderPath"] ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


        private static ObservableCollection<CardModel> _cards = new ObservableCollection<CardModel>();

        private ObservableCollection<Modules.File> recv_file_list = new ObservableCollection<Modules.File>();
        private ObservableCollection<Modules.Folder> recv_folder_list = new ObservableCollection<Modules.Folder>(); 
        private ObservableCollection<LocalSync.Modules.DataType> recv_dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>();

        private ObservableCollection<Modules.File> send_file_list = SenderPage.file_send_list;
        private ObservableCollection<Modules.Folder> send_folder_list = SenderPage.folder_send_list;
        private ObservableCollection<LocalSync.Modules.DataType> send_dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>();

        private ObservableCollection<Modules.DataType> discoveredComputer = new ObservableCollection<Modules.DataType>();

        public HomePage()
        {
            this.InitializeComponent();
            this.InitUI();
        }

        internal void InitUI()
        {
            //TitleTxt.Text = "Home"; 
            this.LoadLocalizedStrings();
            //todo Create Cards 
            RecvInitGetAllFiles();
            InitCards();

        }


        internal void RecvInitGetAllFiles()
        {
            string folderPath = Path.Combine(savedFolderPath, "LocalSync Transfer");
            this.current_folder_path = folderPath;

            // Check if folder already exists
            if (!Directory.Exists(folderPath))
            {
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
                recv_file_list.Add(this_file);
            }



            // Create Folders and add to ListView 
            // Add Folders to List folder_list
            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders)
            {
                DirectoryInfo this_folder_info = new DirectoryInfo(subfolder);
                string name = Path.GetFileNameWithoutExtension(subfolder);
                Folder this_folder = new Folder(name, this_folder_info.LastWriteTime, subfolder);
                recv_folder_list.Add(this_folder);
            }

            //this.AddFolderToList();
            this.RecvAddFilesToList();
        }

        internal ObservableCollection<LocalSync.Modules.DataType> RecvAddFilesToList()
        {
            recv_dataTypeList.Clear();
            recv_dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(
                recv_file_list.Cast<LocalSync.Modules.DataType>().Concat(recv_folder_list.Cast<LocalSync.Modules.DataType>())
            );
            return recv_dataTypeList;
        }

        internal ObservableCollection<LocalSync.Modules.DataType> SendAddFilesToList()
        {
            send_dataTypeList.Clear();
            send_dataTypeList = new ObservableCollection<LocalSync.Modules.DataType>(
                send_file_list.Cast<LocalSync.Modules.DataType>().Concat(send_folder_list.Cast<LocalSync.Modules.DataType>())
            );
            return send_dataTypeList;
        }

        internal void InitCards()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            var recvModel = new CardModel
            {
                Title = resourceMap.GetValue("RecvTitle/Text", resourceContext).ValueAsString,
                Subtitle = resourceMap.GetValue("RecvCardSubtitle", resourceContext).ValueAsString,
                Description = resourceMap.GetValue("RecvCardDescription", resourceContext).ValueAsString,
                iconName = "\uE704",
                navPage = "Receive",
                Items = this.RecvAddFilesToList()
            };
            var senderModel = new CardModel
            {
                Title = resourceMap.GetValue("SenderTitle/Text", resourceContext).ValueAsString,
                Subtitle = resourceMap.GetValue("SendCardSubtitle", resourceContext).ValueAsString,
                Description = resourceMap.GetValue("SendCardDescription", resourceContext).ValueAsString,
                iconName ="\uEC27",
                navPage = "Send",
                Items = this.SendAddFilesToList()
            };
            var computersModel = new CardModel
            {
                Title = resourceMap.GetValue("ComputersTitle/Text", resourceContext).ValueAsString,
                Subtitle = resourceMap.GetValue("ComputerSubtitle", resourceContext).ValueAsString,
                Description = resourceMap.GetValue("ComputerDescription", resourceContext).ValueAsString,
                iconName = "\uF385",
                navPage = "Computers",
            };
            var settingModel = new CardModel
            {
                Title = resourceMap.GetValue("Setting_Title", resourceContext).ValueAsString,
                Subtitle = resourceMap.GetValue("Setting_Subtitle", resourceContext).ValueAsString,
                Description = resourceMap.GetValue("Setting_Description", resourceContext).ValueAsString,
                iconName = "\uE713",
                navPage = "Settings",
            };

            ObservableCollection<CardModel> cardModels = new ObservableCollection<CardModel>()
            {
                recvModel,
                senderModel,
                computersModel,
                settingModel,
            };

            _cards = cardModels;
            cardFunctions.ItemsSource = cardModels;
        }

        private void CardDesignClickEvent(object sender, TappedRoutedEventArgs e)
        {
            var border = sender as Border;

            string navPage = border.Name;
            App.mainWindow.navSwitchTo(navPage, true); 
        }

        private void CardDesign_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = (SolidColorBrush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
            }
        }

        private void CardDesign_PointerExited(object sender, PointerRoutedEventArgs e) 
        {

            if (sender is Border border)
            {
                border.Background = (SolidColorBrush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            }
        }

        private void LoadLocalizedStrings()
        {
            var resourceContext = App.resourceContext; // not using ResourceContext.GetForCurrentView

            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            this.TitleTxt.Text = resourceMap.GetValue("HomeTitle/Text", resourceContext).ValueAsString;
        }

        
    }
}

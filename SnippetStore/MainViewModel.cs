using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AurelienRibon.Ui.SyntaxHighlightBox;
using SnippetStore.BusinessLogic;
using GoogleFile = Google.Apis.Drive.v2.Data.File;

namespace SnippetStore
{
    public class MainViewModel : BaseViewModel
    {
        private readonly SnippetStoreModel model = new SnippetStoreModel();
        private MyTabItem selectedTab;
        private IHighlighter highlighter;
        
        public ObservableCollection<Snippet> Snippets { get; private set; }
        public ObservableCollection<MyTabItem> Tabs { get; private set; }
        public MyTabItem SelectedTab
        {
            get { return selectedTab; }
            set { selectedTab = value; OnPropertyChanged("SelectedTab"); }
        }
        public IHighlighter Highlighter
        {
            get { return highlighter; }
            set { highlighter = value; OnPropertyChanged("Highlighter"); }
        }
           

        public ICommand TreeItemDoubleClickCommand { get; set; }
        public ICommand CloseTabCommand { get; set; }
        public ICommand SelectedChangedCommand { get; set; }
        public ICommand AddSnippetCommand { get; set; }
        public ICommand RemoveSnippetCommand { get; set; }
        public ICommand SaveCurrentTabCommand { get; set; }
        public ICommand CloseAppCommand { get; set; }
        public ICommand SyncSnippetsCommand { get; set; }

        public MainViewModel()
        {
            Snippets = new ObservableCollection<Snippet>(model.GetAllSnippets());
            Tabs = new ObservableCollection<MyTabItem>();

            TreeItemDoubleClickCommand = new RelayCommand<Snippet>(HandleTreeItemDoubleClick);
            CloseTabCommand = new RelayCommand<MyTabItem>(CloseTabCommandHandler);
            SelectedChangedCommand = new RelayCommand<Object>(SelectedChangedCommandHandler);
            AddSnippetCommand = new RelayCommand<Object>(AddSnippetCommandHandler);
            RemoveSnippetCommand = new RelayCommand<Snippet>(RemoveSnippetCommandHandler);
            SaveCurrentTabCommand = new RelayCommand<MyTabItem>(SaveCurrentTabCommandHandler);
            CloseAppCommand = new RelayCommand<CancelEventArgs>(CloseAppCommandHandler);
            SyncSnippetsCommand = new RelayCommand<Object>(SyncSnippetsCommandHandler);

            Highlighter = HighlighterManager.Instance.Highlighters["CSharp"];
        }

        private void SyncSnippetsCommandHandler(object obj)
        {
            var drive = new GoogleDriveStorage();
            drive.CreateInfustructure();

            var allSnippets = Snippets.ToList();
            var driveFiles = drive.GetLookuperFiles();

            var snippetsToUpload = new List<Snippet>();
            foreach (var snippet in allSnippets)
            {
                var existingSnippet = driveFiles.FirstOrDefault(file => file.Title.Equals(snippet.Name, StringComparison.OrdinalIgnoreCase));

                if (existingSnippet == null)
                    snippetsToUpload.Add(snippet);
            }

            var driveFileToDownload = new List<GoogleFile>();
            foreach (var driveFile in driveFiles)
            {
                var existingDriveFile = allSnippets.FirstOrDefault(snippet => snippet.Name.Equals(driveFile.Title, StringComparison.OrdinalIgnoreCase));

                if (existingDriveFile == null)
                    driveFileToDownload.Add(driveFile);
            }
            string debug;

            drive.UploadFiles(snippetsToUpload);
            var snippetsToStore = drive.DownloadFiles(driveFileToDownload);

            foreach (var snippet in snippetsToStore)
            {
                model.Save(snippet);
                Snippets.Add(snippet);
            }
        }

        private void CloseAppCommandHandler(CancelEventArgs eArgs)
        {
            var editedTabs = from tab in Tabs
                             where tab.IsDataChanged
                             select tab;

            if (editedTabs.Count() == 0)
                return;

            foreach (var tab in editedTabs)
            {
                SelectedTab = tab;
                var result = MessageBox.Show(String.Format("Do you want to save changes to {0}", tab.Header), 
                                            "SnippetStore",
                                            MessageBoxButton.YesNoCancel);

                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        eArgs.Cancel = true;
                        return;
                    case MessageBoxResult.No:
                        continue;
                    case MessageBoxResult.Yes:
                        SaveCurrentTabCommand.Execute(SelectedTab);
                        break;
                }
            }
        }

        private void SaveCurrentTabCommandHandler(MyTabItem tab)
        {
            if (tab == null)
                return;

            var snippetToUpdate = GetSnippetByTab(SelectedTab);
            model.Save(snippetToUpdate);

            var drive = new GoogleDriveStorage();
            drive.Update(snippetToUpdate);

            tab.SuccessfulSave();
        }

        private void RemoveSnippetCommandHandler(Snippet snippet)
        {
            if (snippet == null)
                return;

            var mbResult = MessageBox.Show("Remove this item?", "SnippetStore", MessageBoxButton.OKCancel);
            
            if (mbResult == MessageBoxResult.Cancel)
                return;

            model.Remove(snippet);
            Snippets.Remove(snippet);

            // remove from google drive 
            var drive = new GoogleDriveStorage();
            drive.RemoveSnippet(snippet);

            CloseTabCommandHandler(GetTabBySnippet(snippet));
        }

        private void AddSnippetCommandHandler(object obj)
        {
            var addForm = new AddWindow()
            {
                Owner = Application.Current.MainWindow,
                ShowInTaskbar = false,
            };

            if (addForm.ShowDialog().Value)
            {
                var fileName = addForm.DataContext as String;

                if (String.IsNullOrEmpty(fileName))
                {
                    MessageBox.Show("File name not provided", "Invalid Input", MessageBoxButton.OK);
                    return;
                }

                var newSnippet = new Snippet(fileName, null);
                model.Save(newSnippet);

                Snippets.Add(newSnippet);
                HandleTreeItemDoubleClick(newSnippet);
            }
        }

        private void SelectedChangedCommandHandler(object obj)
        {
            foreach (var tab in Tabs)
            {
                tab.CloseButtonVasability = Visibility.Hidden.ToString();
            }

            if (SelectedTab != null)
            {
                var selectedTab = (from tab in Tabs
                                   where tab.Header.Equals(SelectedTab.Header)
                                   select tab).First();

                selectedTab.CloseButtonVasability = Visibility.Visible.ToString();
            }
        }

        private void CloseTabCommandHandler(MyTabItem item)
        {
            if (SelectedTab == null)
                return;

            if (SelectedTab.IsDataChanged)
            {
                var result = MessageBox.Show("Do you want to save?", "Data is changed", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    var sn = GetSnippetByTab(SelectedTab);                    
                    model.Save(sn);

                    SelectedTab.SuccessfulSave();
                }
            }

            Tabs.Remove(SelectedTab);
        }

        private void HandleTreeItemDoubleClick(Snippet snippet)
        {
            var tab = new MyTabItem()
            {
                Header = snippet.Name,
                VisibleHeader = snippet.Name,
                Content = snippet.Content,
                CloseButtonVasability = Visibility.Visible.ToString(),
            };

            var existedTab = (from duplicateTab in Tabs
                              where duplicateTab.Header.Equals(snippet.Name)
                              select duplicateTab).FirstOrDefault();

            if (existedTab != null)
            {
                SelectedTab = existedTab;
            }
            else
            {
                Tabs.Add(tab);
                SelectedTab = tab;
            }
        }

        private Snippet GetSnippetByTab(MyTabItem tab)
        {
            var snippet = (from sn in Snippets
                           where sn.Name.Equals(tab.Header)
                           select sn).FirstOrDefault();

            snippet.Content = tab.Content;
            return snippet;
        }

        private MyTabItem GetTabBySnippet(Snippet snippet)
        {
            var selectedTab = (from tab in Tabs
                               where tab.Header.Equals(SelectedTab.Header)
                               select tab).FirstOrDefault();

            return selectedTab;
        }
    }
}

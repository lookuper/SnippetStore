﻿using System;
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
        private String syncButtonTooltip = "No updates avaliable";
        private bool isSyncActive;
        private IHighlighter highlighter;
        private GoogleDriveStorage googleDrive = new GoogleDriveStorage
            (
                "929945743179-l1ut4m2r3untdolncrgu6tf6vlsccjcl.apps.googleusercontent.com",
                "W7iNoZwX1Wl4WS4HBP5B3akU"
            );
        
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
        public string SyncButtonTooltip
        {
            get { return syncButtonTooltip; }
            set { syncButtonTooltip = value; OnPropertyChanged("SyncButtonTooltip"); }
        }
        public bool IsSyncActive
        {
            get { return isSyncActive; }
            set { isSyncActive = value; OnPropertyChanged("IsSyncActive"); }
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

            SyntaxUtil.Async(
                () => googleDrive.GetUpdateFilesFromGoogleDrive(),
                result =>
                {
                    if (result.Count > 0)
                    {
                        //IsSyncActive = true;
                        SyncButtonTooltip = String.Format("{0} update avaliable", result.Count);
                    }
                });
        }

        private void SyncSnippetsCommandHandler(object obj)
        {
            SyntaxUtil.Async(
                () => GetLatestFromGDrive(),
                result => SyncButtonTooltip = result);
        }

        private String GetLatestFromGDrive()
        {
            int filesUpdated = 0;
            googleDrive.CreateInfustructure();

            var allSnippets = Snippets.ToList();
            var driveFiles = googleDrive.GetLookuperFiles();

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

            googleDrive.UploadFiles(snippetsToUpload);
            var snippetsToStore = googleDrive.DownloadFiles(driveFileToDownload);
            var dispatcher = Application.Current.Dispatcher;

            foreach (var snippet in snippetsToStore)
            {
                model.Save(snippet);
                dispatcher.Invoke(new Action(() => Snippets.Add(snippet)));
                //Snippets.Add(snippet); // for STA
            }  

            var updatesFromGoogleDrive = googleDrive.GetUpdateFilesFromGoogleDrive();

            foreach (var gFileUpdate in updatesFromGoogleDrive)
            {
                var snippetToUpdate = Snippets.FirstOrDefault(snippet => snippet.Name.Equals(gFileUpdate.Title, StringComparison.OrdinalIgnoreCase));

                if (snippetToUpdate != null)
                    googleDrive.Update(snippetToUpdate);
            }

            if (snippetsToUpload.Count == 0 &&
                driveFileToDownload.Count == 0 &&
                updatesFromGoogleDrive.Count == 0)
                return SyncButtonTooltip;
            else
            return String.Format("Uploaded: {0}, Downloaded: {1}, Updated: {2}", 
                                    snippetsToUpload.Count, 
                                    driveFileToDownload.Count, 
                                    updatesFromGoogleDrive.Count);
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

            googleDrive.Update(snippetToUpdate);

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

            googleDrive.RemoveSnippet(snippet);
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AurelienRibon.Ui.SyntaxHighlightBox;
using SnippetStore.BusinessLogic;

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

        public MainViewModel()
        {
            Snippets = new ObservableCollection<Snippet>(model.GetAllSnippets());
            Tabs = new ObservableCollection<MyTabItem>();

            TreeItemDoubleClickCommand = new RelayCommand<Snippet>(HandleTreeItemDoubleClick);
            CloseTabCommand = new RelayCommand<MyTabItem>(CloseTabCommandHandler);
            SelectedChangedCommand = new RelayCommand<Object>(SelectedChangedCommandHandler);
            AddSnippetCommand = new RelayCommand<Object>(AddSnippetCommandHandler);
            RemoveSnippetCommand = new RelayCommand<Snippet>(RemoveSnippetCommandHandler);

            Highlighter = HighlighterManager.Instance.Highlighters["CSharp"];
        }

        private void RemoveSnippetCommandHandler(Snippet snippet)
        {
            if (snippet == null)
                return;

            model.Remove(snippet);
            Snippets.Remove(snippet);
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
                // addForm.DataContext
            }
            //var newSnippet = new Snippet("New Snippet *", String.Empty);

            //var newItem = new MyTabItem()
            //{
            //    Header = "New Snippet *",
            //    Content = String.Empty,
            //};

            //Snippets.Add(newSnippet);
            
            //Tabs.Add(newItem);
            //SelectedTab = newItem;
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
            Tabs.Remove(SelectedTab);
        }

        private void HandleTreeItemDoubleClick(Snippet snippet)
        {
            var tab = new MyTabItem()
            {
                Header = snippet.Name,
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

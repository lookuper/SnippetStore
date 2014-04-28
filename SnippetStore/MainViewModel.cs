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


        public MainViewModel()
        {
            Snippets = new ObservableCollection<Snippet>(model.GetAllSnippets());
            Tabs = new ObservableCollection<MyTabItem>();

            TreeItemDoubleClickCommand = new RelayCommand<Snippet>(HandleTreeItemDoubleClick);
            CloseTabCommand = new RelayCommand<MyTabItem>(CloseTabCommandHandler);
            SelectedChangedCommand = new RelayCommand<Object>(SelectedChangedCommandHandler);

            Highlighter = HighlighterManager.Instance.Highlighters["CSharp"];
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
    }
}

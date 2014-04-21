using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using SnippetStore.BusinessLogic;

namespace SnippetStore
{
    public class MainViewModel : BaseViewModel
    {
        private readonly SnippetStoreModel model = new SnippetStoreModel();
                
        public ObservableCollection<Snippet> Snippets { get; private set; }

        public ICommand TreeItemDoubleClickCommand { get; set; }

        public MainViewModel()
        {
            Snippets = new ObservableCollection<Snippet>(model.GetAllSnippets());

            TreeItemDoubleClickCommand = new RelayCommand<Snippet>(HandleTreeItemDoubleClick);
        }

        private void HandleTreeItemDoubleClick(Snippet obj)
        {
            throw new NotImplementedException();
        }
    }
}

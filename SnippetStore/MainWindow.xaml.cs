using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SnippetStore.BusinessLogic;

namespace SnippetStore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void ToolBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var toolbar = sender as ToolBar;
            var overflowGrid = toolbar.Template.FindName("OverflowGrid", toolbar) as FrameworkElement;

            if (overflowGrid != null)
            {
                overflowGrid.Visibility = toolbar.HasOverflowItems ? Visibility.Visible : Visibility.Collapsed;
            }

            var mainPanelBorder = toolbar.Template.FindName("MainPanelBorder", toolbar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                var defaultMargin = new Thickness(0, 0, 11, 0);
                mainPanelBorder.Margin = toolbar.HasOverflowItems ? defaultMargin : new Thickness(0);
            }
        }

        private void ListBoxItem_DoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var mv = this.DataContext as MainViewModel;
            mv.TreeItemDoubleClickCommand.Execute((sender as ListBoxItem).Content as Snippet);
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Control control = sender as Control;
            //ICommand command = (ICommand)control.GetValue(CommandProperty);
            //object commandParameter = control.GetValue(CommandParameterProperty);

            //if (sender is TreeViewItem)
            //{
            //    if (!((TreeViewItem)sender).IsSelected)
            //        return;
            //}

            //if (command.CanExecute(commandParameter))
            //{
            //    command.Execute(commandParameter);
            //}
        }
    }
}

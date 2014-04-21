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
    }
}

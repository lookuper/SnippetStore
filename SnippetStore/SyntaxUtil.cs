using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AurelienRibon.Ui.SyntaxHighlightBox;

namespace SnippetStore
{
    public static class SyntaxUtil
    {
        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached("BindableSource",
                                                typeof(IHighlighter),
                                                typeof(SyntaxUtil),
                                                new UIPropertyMetadata(null, BindableSourcePropertyChanged));

        public static String GetBindableSource(DependencyObject obj)
        {
            return obj.GetValue(BindableSourceProperty) as String;
        }

        public static void SetBindableSource(DependencyObject obj, string value)
        {
            obj.SetValue(BindableSourceProperty, value);
        }

        private static void BindableSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as SyntaxHighlightBox;
            var highlighter = e.NewValue as IHighlighter;

            if (box == null)
                return;

            box.CurrentHighlighter = highlighter;
        }

        public static void Async<T>(Func<T> g, Action action)
        {
            Task.Factory.StartNew<T>(g)
                .ContinueWith(result => result)
                .ContinueWith(_ => action.Invoke());
        }
    }

}

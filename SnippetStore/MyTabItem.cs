using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnippetStore
{
    public class MyTabItem : BaseViewModel
    {
        public String Header { get; set; }

        private String content;
        public String Content 
        {
            get { return content; }
            set { content = value; 
                OnPropertyChanged("Content"); }
        }

        private String closeButtonVisability;
        public String CloseButtonVasability
        {
            get { return closeButtonVisability; }
            set { closeButtonVisability = value; OnPropertyChanged("CloseButtonVasability"); }
        }
    }
}

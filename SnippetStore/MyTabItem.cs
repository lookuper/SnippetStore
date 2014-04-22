using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnippetStore
{
    public class MyTabItem : BaseViewModel
    {
        public String Header { get; set; }
        public String Content { get; set; }

        private String closeButtonVisability;
        public String CloseButtonVasability
        {
            get { return closeButtonVisability; }
            set { closeButtonVisability = value; OnPropertyChanged("CloseButtonVasability"); }
        }
    }
}

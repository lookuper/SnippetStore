using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnippetStore
{
    public class MyTabItem : BaseViewModel
    {
        private String header;
        public String Header
        {
            get { return header; }
            set { header = value; OnPropertyChanged("Header"); }
        }

        private String visibleHeader;
        public String VisibleHeader
        {
            get { return visibleHeader; }
            set { visibleHeader = value; OnPropertyChanged("VisibleHeader"); }
        }

        private String content;
        public String Content 
        {
            get { return content; }
            set
            {
                var oldContent = content;

                content = value;
                OnPropertyChanged("Content");

                if (!IsDataChanged && oldContent != null && !String.Equals(oldContent, content))
                {
                    ChangeHeader();
                    IsDataChanged = true;
                }
            }
        }

        private bool dataChanged;
        public bool IsDataChanged
        {
            get { return dataChanged; }
            set { dataChanged = value; OnPropertyChanged("DataChanged"); }
        }

        private void ChangeHeader()
        {
            if (VisibleHeader[VisibleHeader.Length - 1].Equals('*'))
                return;
            else
                VisibleHeader = String.Format("{0} *", VisibleHeader);
        }

        private String closeButtonVisability;
        public String CloseButtonVasability
        {
            get { return closeButtonVisability; }
            set { closeButtonVisability = value; OnPropertyChanged("CloseButtonVasability"); }
        }

        public void SuccessfulSave()
        {
            IsDataChanged = false;
            VisibleHeader = Header;
        }
    }
}

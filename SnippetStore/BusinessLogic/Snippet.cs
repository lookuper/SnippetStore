using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SnippetStore.BusinessLogic
{
    public class Snippet
    {
        public String Name { get; private set; }
        public String PathToFile { get; set; }

        private String content;
        public String Content
        {
            get { return content ?? (content = GetContent()); }
            set { content = value; }
        }

        public Snippet(string name, string path)
        {
            Name = name;
            PathToFile = path;

            //Content = File.ReadAllText(PathToFile);
        }

        private String GetContent()
        {
            return File.ReadAllText(PathToFile);
        }
    }
}

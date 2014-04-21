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
        public String PathToFile { get; private set; }

        public Snippet(string name, string path)
        {
            Name = name;
            PathToFile = path;
        }
    }
}

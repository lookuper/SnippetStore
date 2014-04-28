using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SnippetStore.BusinessLogic; // shoul not be here, need to use enity ds and them map to DTO object

namespace SnippetStore.DataAccess
{
    class FolderRepository : IRepository<Snippet>
    {
        public String PathToSnippets { get; private set; }

        public FolderRepository()
        {
            PathToSnippets = Path.Combine(Directory.GetCurrentDirectory(), SnippetStore.Properties.Resources.SnippetFolderName);
        }

        public IEnumerable<Snippet> GetData()
        {
            if (!Directory.Exists(PathToSnippets))
                throw new DirectoryNotFoundException(PathToSnippets);

            var list = new List<Snippet>();

            foreach (var filePath in Directory.GetFiles(PathToSnippets))
            {
                var snippet = new Snippet(Path.GetFileName(filePath), filePath);
                list.Add(snippet);
            }

            return list;
        }

        public void Save(Snippet snippet)
        {
            var pathWithName = Path.Combine(PathToSnippets, snippet.Name);

            using (File.Create(pathWithName))
            {
                snippet.PathToFile = pathWithName;
            }
        }

        public void Update(Snippet snippet)
        {
            var pathWithName = Path.Combine(PathToSnippets, snippet.Name);
            File.WriteAllText(pathWithName, snippet.Content);
        }

        public void Remove(Snippet snippet)
        {
            File.Delete(snippet.PathToFile);
        }
    }
}

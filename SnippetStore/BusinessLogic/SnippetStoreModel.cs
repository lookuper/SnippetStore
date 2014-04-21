using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SnippetStore.DataAccess;

namespace SnippetStore.BusinessLogic
{
    public class SnippetStoreModel
    {
        private readonly IRepository<Snippet> repository = new FolderRepository();

        public IEnumerable<Snippet> GetAllSnippets()
        {
            return repository.GetData();
        }
    }
}

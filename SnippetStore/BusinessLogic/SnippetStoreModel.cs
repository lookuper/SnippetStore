﻿using System;
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

        public void Save(Snippet snippet)
        {
            if (String.IsNullOrEmpty(snippet.PathToFile)) // new file
            {
                repository.Save(snippet);
            }
            else // need to update file
            {
                repository.Update(snippet);
            }
        }

        internal void Remove(Snippet snippet)
        {
            repository.Remove(snippet);
        }
    }
}

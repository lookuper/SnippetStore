using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnippetStore.BusinessLogic
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetData();
        void Save(T item);
        void Update(T item);
        void Remove(T item);
    }
}

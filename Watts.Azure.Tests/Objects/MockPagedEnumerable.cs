namespace Watts.Azure.Tests.Objects
{
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.Azure.Batch;

    public class MockPagedEnumerable<T> : IPagedEnumerable<T>
    {
        private readonly List<T> items = new List<T>();

        public void Add(T item)
        {
            this.items.Add(item);
        }

        public IPagedEnumerator<T> GetPagedEnumerator()
        {
            return null;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }
    }
}
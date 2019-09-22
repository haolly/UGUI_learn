using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UI.Collections
{
    internal class IndexedSet<T> : IList<T>
    {
        readonly List<T> m_List = new List<T>();
        Dictionary<T, int> m_Dictionary = new Dictionary<T, int>();
        
        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            m_List.Add(item);
            m_Dictionary.Add(item, m_List.Count - 1);
        }

        public bool AddUnique(T item)
        {
            if (m_Dictionary.ContainsKey(item))
                return false;
            Add(item);
            return true;
        }

        public void Clear()
        {
            m_List.Clear();
            m_Dictionary.Clear();
        }

        public bool Contains(T item)
        {
            return m_Dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_List.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            int index = -1;
            if (!m_Dictionary.TryGetValue(item, out index))
                return false;
            RemoveAt(index);
            return true;
        }

        public int Count
        {
            get { return m_List.Count; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public int IndexOf(T item)
        {
            int index = -1;
            m_Dictionary.TryGetValue(item, out index);
            return index;
        }

        public void Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            T item = m_List[index];
            m_Dictionary.Remove(item);
            
            if(index == m_List.Count - 1)
                m_List.RemoveAt(index);
            else
            {
                // replace indexItem with lastItem, then remove last index of list, so the order is not guaranteed
                int replaceItemIndex = m_List.Count - 1;
                T replaceItem = m_List[replaceItemIndex];
                
                m_List[index] = replaceItem;
                m_Dictionary[replaceItem] = index;
                
                m_List.RemoveAt(replaceItemIndex);
            }
        }

        public T this[int index]
        {
            get { return m_List[index];}
            set
            {
                T item = m_List[index];
                m_Dictionary.Remove(item);
                m_List[index] = value;
                m_Dictionary.Add(value, index);
            }
        }

        public void RemoveAll(Predicate<T> match)
        {
            int i = 0;
            while (i < m_List.Count)
            {
                T item = m_List[i];
                if (match(item))
                    Remove(item);
                else
                {
                    ++i;
                }
            }
        }

        public void Sort(Comparison<T> sortLayoutFunction)
        {
            m_List.Sort(sortLayoutFunction);
            for (int i = 0; i < m_List.Count; i++)
            {
                T item = m_List[i];
                m_Dictionary[item] = i;
            }
        }
    }
}
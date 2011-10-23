//TODO: FIX
/*using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;

namespace System.Collections.Concurrent
{
    public class ConcurrentList<T> : IEnumerable<T>
    {
        private readonly HashSet<T> items = new HashSet<T>();
        private readonly object padlock = new object();

        public bool Contains(T item)
        {
            lock (padlock)
            {
                return items.Contains(item);
            }
        }

        public bool Add(T item)
        {
            lock (padlock)
            {
                return items.Add(item);
            }
        }

        public bool Remove(T item)
        {
            lock (padlock)
            {
                if (items.Remove(item))
                {
                    Monitor.PulseAll(padlock);
                    return true;
                }
                else return false;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }    
    }
} */

using System;
using System.Collections;
using System.Collections.Generic;

namespace NameGenerator
{
    public class Counter<T> : ICollection<KeyValuePair<T, int>> where T : class
    {

        Dictionary<T, int> innerDictionary;
        public int Total { get; private set; }

        int ICollection<KeyValuePair<T, int>>.Count => ((ICollection<KeyValuePair<T, int>>)innerDictionary).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<T, int>>)innerDictionary).IsReadOnly;

        public Counter()
        {
            innerDictionary = new Dictionary<T, int>();
            Total = 0;
        }

        public void Add(T entry, int quantity = 1)
        {
            if (innerDictionary.TryAdd(entry, quantity))
            {
                // All done - entry wasn't present, and we added 1 for it
            }
            else
            {
                // Entry already existed - add one to the existing value
                innerDictionary[entry] += quantity;
            }
            Total += quantity;
        }

        public int Count(T entry)
        {
            if (innerDictionary.TryGetValue(entry, out int countFromDictionary))
            {
                return countFromDictionary;
            }
            else
            {
                return 0;
            }
        }

        public IEnumerator<KeyValuePair<T, int>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<T, int>>)innerDictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)innerDictionary).GetEnumerator();
        }

        public T? GetRandom()
        {
            if (Total == 0)
            {
                return null;
            }
            int randomTargetIndex = new Random().Next(Total);
            int currentIndex = 0;
            foreach (var (entry, count) in innerDictionary)
            {
                currentIndex += count;
                if (currentIndex > randomTargetIndex)
                {
                    return entry;
                }
            }
            // Should be impossible to reach
            // We added all counts, but the random index (<Total) is still greater!
            throw new InvalidOperationException("The author of this method screwed up");
        }

        public void Add(KeyValuePair<T, int> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<T, int>>)innerDictionary).Clear();
        }

        public bool Contains(KeyValuePair<T, int> item)
        {
            return ((ICollection<KeyValuePair<T, int>>)innerDictionary).Contains(item);
        }

        public void CopyTo(KeyValuePair<T, int>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<T, int>>)innerDictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<T, int> item)
        {
            return ((ICollection<KeyValuePair<T, int>>)innerDictionary).Remove(item);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;

namespace NameGenerator
{
    public class Counter<T> : IEnumerable<KeyValuePair<T, int>> where T : class
    {

        Dictionary<T, int> innerDictionary;
        public int Total { get; private set; }

        public Counter()
        {
            innerDictionary = new Dictionary<T, int>();
            Total = 0;
        }

        public void Add(T entry)
        {
            if (innerDictionary.TryAdd(entry, 1))
            {
                // All done - entry wasn't present, and we added 1 for it
            }
            else
            {
                // Entry already existed - add one to the existing value
                innerDictionary[entry] += 1;
            }
            Total += 1;
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
    }
}

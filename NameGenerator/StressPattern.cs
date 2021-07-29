using System;
using System.Collections.Generic;

namespace NameGenerator
{
    public class StressPattern
    {
        public static StressPattern? FromWord(Word word)
        {
            int? stressedIndex = null;
            int vowelIndex = 0;
            foreach (Run run in word.Runs)
            {
                if (run.Category() == RunCategory.Vowel)
                {
                    if (run.HasPrimaryStress())
                    {
                        if (stressedIndex.HasValue)
                        {
                            // We just encountered a *second* primary stress, which isn't allowed
                            return null;
                        }
                        else
                        {
                            stressedIndex = vowelIndex;
                        }
                    }
                    vowelIndex++;
                }
            }
            if (stressedIndex.HasValue)
            {
                return new StressPattern(stressedIndex.Value, vowelIndex);
            }
            else
            {
                // Word has no stressed index, which we signal with -1
                return new StressPattern(-1, vowelIndex);
            }
        }
        public StressPattern(int stressedIndex, int count)
        {
            if (count <= stressedIndex) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (stressedIndex < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(stressedIndex));
            }
            StressedIndex = stressedIndex;
            Count = count;
        }

        public int StressedIndex { get; }
        public int Count { get; }
        public string Symbol()
        {
            List<char> chars = new();
            for (int i = 0; i<Count; i++)
            {
                if (i == StressedIndex)
                {
                    chars.Add('A');
                }
                else
                {
                    chars.Add('a');
                }
            }
            return new string(chars.ToArray());
        }

        public override bool Equals(object? obj)
        {
            return obj is StressPattern pattern &&
                   StressedIndex == pattern.StressedIndex &&
                   Count == pattern.Count;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StressedIndex, Count);
        }
    }
}

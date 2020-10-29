using System;
using System.Collections.Generic;

namespace NameGenerator
{
    public class WordGenerator
    {
        public Counter<StressPattern> StressPatterns { get; set; } = new Counter<StressPattern>();

        public Counter<Run> PreStressOnsets { get; set; } = new Counter<Run>();
        public Counter<Run> UnstressedOnsets { get; set; } = new Counter<Run>();

        public ContinuationMap<Run> PreStressOnsetToStressedVowel { get; set; } = new ContinuationMap<Run>();
        public ContinuationMap<Run> UnstressedOnsetToUnstressedVowel { get; set; } = new ContinuationMap<Run>();

        public ContinuationMap<Run> UnstressedVowelToPreStressIntervocal { get; set; } = new ContinuationMap<Run>();
        public ContinuationMap<Run> UnstressedVowelToUnstressedIntervocal { get; set; } = new ContinuationMap<Run>();
        public ContinuationMap<Run> StressedVowelToPostStressIntervocal { get; set; } = new ContinuationMap<Run>();

        public ContinuationMap<Run> PreStressIntervocalToStressedVowel { get; set; } = new ContinuationMap<Run>();
        public ContinuationMap<Run> PostStressIntervocalToUnstressedVowel { get; set; } = new ContinuationMap<Run>();
        public ContinuationMap<Run> UnstressedIntervocalToUnstressedVowel { get; set; } = new ContinuationMap<Run>();

        public ContinuationMap<Run> StressedVowelToPostStressCoda { get; set; } = new ContinuationMap<Run>();
        public ContinuationMap<Run> UnstressedVowelToUnstressedCoda { get; set; } = new ContinuationMap<Run>();

        public bool LearnWord(Word word)
        {
            StressPattern? stressPattern = StressPattern.FromWord(word);
            if (stressPattern == null)
            {
                // Ignore words which have multiple primary stresses,
                // Or no primary stress at all
                return false;
            }
            StressPatterns.Add(stressPattern);

            if (stressPattern.StressedIndex == 0)
            {
                PreStressOnsets.Add(word.Runs[0]);
                PreStressOnsetToStressedVowel.Add(word.Runs[0], word.Runs[1]);
            }
            else
            {
                UnstressedOnsets.Add(word.Runs[0]);
                UnstressedOnsetToUnstressedVowel.Add(word.Runs[0], word.Runs[1]);
            }

            for (int i = 2;
                i < word.Runs.Count - 2;
                i += 2) // Increment by two - we are only looking at intervocal consonant clusters
            {
                Run intervocal = word.Runs[i];
                Run previousVowel = word.Runs[i - 1];
                Run nextVowel = word.Runs[i + 1];
                if (previousVowel.HasPrimaryStress())
                {
                    StressedVowelToPostStressIntervocal.Add(previousVowel, intervocal);
                    PostStressIntervocalToUnstressedVowel.Add(intervocal, nextVowel);
                }
                else if (nextVowel.HasPrimaryStress())
                {
                    UnstressedVowelToPreStressIntervocal.Add(previousVowel, intervocal);
                    PreStressIntervocalToStressedVowel.Add(intervocal, nextVowel);
                }
                else
                {
                    UnstressedVowelToUnstressedIntervocal.Add(previousVowel, intervocal);
                    UnstressedIntervocalToUnstressedVowel.Add(intervocal, nextVowel);
                }
            }

            int lastIndex = word.Runs.Count - 1;
            Run lastVowel = word.Runs[lastIndex - 1];
            Run coda = word.Runs[lastIndex];
            if (lastVowel.HasPrimaryStress())
            {
                StressedVowelToPostStressCoda.Add(lastVowel, coda);
            }
            else
            {
                UnstressedVowelToUnstressedCoda.Add(lastVowel, coda);
            }
            return true;
        }

        public class GenerationFailedException : Exception { }

        private Exception Fail()
        {
            return new GenerationFailedException();
        }

        public string? GenerateName()
        {
            List<string> parts = new List<string>();
            StressPattern? stressPattern = StressPatterns.GetRandom() ?? throw Fail();

            Run onset;
            Run firstVowel;
            if (stressPattern.StressedIndex == 0)
            {
                onset = PreStressOnsets.GetRandom() ?? throw Fail();
                firstVowel = PreStressOnsetToStressedVowel.GenerateContinuation(onset) ?? throw Fail();
            }
            else
            {
                onset = UnstressedOnsets.GetRandom() ?? throw Fail();
                firstVowel = UnstressedOnsetToUnstressedVowel.GenerateContinuation(onset) ?? throw Fail();
            }
            parts.Add(onset.Symbol());
            parts.Add(firstVowel.Symbol());

            Run previousVowel = firstVowel;

            for (int i = 1; i<stressPattern.Count; i++)
            {
                Run intervocal;
                Run vowel;
                if (i == stressPattern.StressedIndex)
                {
                    intervocal = UnstressedVowelToPreStressIntervocal
                        .GenerateContinuation(previousVowel) 
                        ?? throw Fail();
                    vowel = PreStressIntervocalToStressedVowel
                        .GenerateContinuation(intervocal)
                        ?? throw Fail();
                }
                else if (i - 1 == stressPattern.StressedIndex)
                {
                    intervocal = StressedVowelToPostStressIntervocal
                        .GenerateContinuation(previousVowel)
                        ?? throw Fail();
                    vowel = PostStressIntervocalToUnstressedVowel
                        .GenerateContinuation(intervocal)
                        ?? throw Fail();
                }
                else
                {
                    intervocal = UnstressedVowelToUnstressedIntervocal
                        .GenerateContinuation(previousVowel)
                        ?? throw Fail();
                    vowel = UnstressedIntervocalToUnstressedVowel
                        .GenerateContinuation(intervocal)
                        ?? throw Fail();
                }
                parts.Add(intervocal.Symbol());
                parts.Add(vowel.Symbol());
                previousVowel = vowel;
            }

            Run coda;
            if (stressPattern.StressedIndex == stressPattern.Count - 1)
            {
                coda = StressedVowelToPostStressCoda
                    .GenerateContinuation(previousVowel)
                    ?? throw Fail();
            }
            else
            {
                coda = UnstressedVowelToUnstressedCoda
                    .GenerateContinuation(previousVowel)
                    ?? throw Fail();
            }
            parts.Add(coda.Symbol());
            return string.Join('|', parts);
        }      
    }
}

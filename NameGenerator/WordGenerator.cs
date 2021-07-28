using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NameGenerator
{
    public class WordGenerator
    {
        public Counter<StressPattern> StressPatterns { get; set; } = new Counter<StressPattern>();

        // Vowels are divided into two categories
        //     Stressed   - Denoted by a 1 in ARPAbet notation
        //     Unstressed - Denoted by any other number in ARPAbet notation

        // Words contain exactly one stressed vowel.

        // Intervocals are sequences of consonants between vowels. They are divided into three
        // categories:
        //     Rising  - Occurs just before a stressed vowel (and after an unstressed one).
        //     Falling - Occurs just after a stressed vowel (and after a stressed one).
        //     Flat    - Occurs betwen two unstressed vowel.

        /// <summary>
        /// Onsets (word-initial consonant clusters) which precede stressed syllables
        /// e.g. PR in PR-AE1-KT-IH0-S ("practice")
        /// </summary>
        public Counter<Run> StressedOnset { get; set; } = new Counter<Run>();

        /// <summary>
        /// Onsets (word-initial consonant clusters) which precede unstressed syllables
        /// e.g. STR in STR-AA0-MB-OW1-L-IY0 ("stromboli")
        /// </summary>
        public Counter<Run> UnstressedOnset { get; set; } = new Counter<Run>();

        /// <summary>
        /// Transitions from onsets to stressed vowels
        /// e.g. PR-AE1 in [PR-AE1]-KT-IH0-S ("practice")
        /// </summary>
        public ContinuationMap<Run> OnsetToStressed { get; set; } = new();

        /// <summary>
        /// Transitions from onsets to unstressed vowels
        /// e.g. STR-AA0 in STR-AA0-MB-OW1-L-IY0 ("stromboli")
        /// </summary>
        public ContinuationMap<Run> OnsetToUnstressed { get; set; } = new();

        /// <summary>
        /// Transitions from unstressed vowels to intervocal sequences that precede stressed vowels
        /// e.g. AA0-MB in STR-AA0-MB-OW1-L-IY0 ("stromboli")
        /// </summary>
        public ContinuationMap<Run> UnstressedToRising { get; set; } = new();
        /// <summary>
        /// Transitions from unstressed vowels to intervocal sequences that do not precede stressed vowels.
        /// e.g. IH0-K in T-AE1-KT-IH0-K-AH0-L ("tactical")
        /// </summary>
        public ContinuationMap<Run> UnstressedToFlat { get; set; } = new();
        /// <summary>
        /// Transitions from stressed vowels to intervocal sequences
        /// e.g. OW1-L in STR-AA0-MB-OW1-L-IY0 ("stromboli")
        /// </summary>
        public ContinuationMap<Run> StressedToFalling { get; set; } = new();
        /// <summary>
        /// Transitions from intervocal sequences to stressed vowels.
        /// e.g. MB-OW1 in STR-AA0-MB-OW1-L-IY0 ("stromboli")
        /// </summary>
        public ContinuationMap<Run> RisingToStressed { get; set; } = new();
        /// <summary>
        /// Transitions from intervocals that follow stressed vowels to unstressed vowels.
        /// e.g. KT-IH0 in T-AE1-KT-IH0-K-AH0-L ("tactical")
        /// </summary>
        public ContinuationMap<Run> FallingToUnstressed { get; set; } = new();
        /// <summary>
        /// Transitions from intervocals that do not follow stressed vowels to unstressed vowels.
        /// e.g. K-AH0 in T-AE1-KT-IH0-K-AH0-L ("tactical")
        /// </summary>
        public ContinuationMap<Run> FlatToUnstressed { get; set; } = new();
        /// <summary>
        /// Transitions from stressed vowels to codas (word-final consonant clusters).
        /// e.g. OW1-K in P-OW1-K ("poke")
        /// </summary>
        public ContinuationMap<Run> StressedToCoda { get; set; } = new();
        /// <summary>
        /// Transitions from unstressed vowels to codas (word-final consonant clusters).
        /// AH0-L in T-AE1-KT-IH0-K-AH0-L ("tactical")
        /// </summary>
        public ContinuationMap<Run> UnstressedToCoda { get; set; } = new();

        /// <summary>
        /// Serialize the trained generator to JSON
        /// </summary>
        /// <returns>Resulting JSON string</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Deserialize the given <paramref name="jsonString"/> into a <see cref="WordGenerator"/>.
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static WordGenerator ImportFromJson(string jsonString)
        {
            return JsonSerializer.Deserialize<WordGenerator>(jsonString)
                ?? throw new Exception("Failed to decode WordGenerator from JSON.");
        }

        /// <summary>
        /// Analyze the word, and update the generator's counters and continuation maps
        /// to reflect the derived statistics. Specifically, new data will be added to:
        /// <list type="bullet">
        /// <item><description><see cref="StressedOnset"/></description></item>
        /// <item><description><see cref="UnstressedOnset"/></description></item>
        /// <item><description><see cref="OnsetToStressed"/></description></item>
        /// <item><description><see cref="OnsetToUnstressed"/></description></item>
        /// <item><description><see cref="StressedToFalling"/></description></item>
        /// <item><description><see cref="UnstressedToRising"/></description></item>
        /// <item><description><see cref="UnstressedToFlat"/></description></item>
        /// <item><description><see cref="RisingToStressed"/></description></item>
        /// <item><description><see cref="FallingToUnstressed"/></description></item>
        /// <item><description><see cref="FlatToUnstressed"/></description></item>
        /// </list>
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool LearnWord(Word word)
        {
            StressPattern? stressPattern = StressPattern.FromWord(word);
            if (stressPattern == null)
            {
                // Ignore words which have multiple primary stresses,
                // Or no primary stress at all
                return false;
            }
            StressPatterns.Add(stressPattern, word.Frequency);

            if (stressPattern.StressedIndex == 0)
            {
                StressedOnset.Add(word.Runs[0], word.Frequency);
                OnsetToStressed.Add(word.Runs[0], word.Runs[1], word.Frequency);
            }
            else
            {
                UnstressedOnset.Add(word.Runs[0], word.Frequency);
                OnsetToUnstressed.Add(word.Runs[0], word.Runs[1], word.Frequency);
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
                    StressedToFalling.Add(previousVowel, intervocal, word.Frequency);
                    FallingToUnstressed.Add(intervocal, nextVowel, word.Frequency);
                }
                else if (nextVowel.HasPrimaryStress())
                {
                    UnstressedToRising.Add(previousVowel, intervocal, word.Frequency);
                    RisingToStressed.Add(intervocal, nextVowel, word.Frequency);
                }
                else
                {
                    UnstressedToFlat.Add(previousVowel, intervocal, word.Frequency);
                    FlatToUnstressed.Add(intervocal, nextVowel, word.Frequency);
                }
            }

            int lastIndex = word.Runs.Count - 1;
            Run lastVowel = word.Runs[lastIndex - 1];
            Run coda = word.Runs[lastIndex];
            if (lastVowel.HasPrimaryStress())
            {
                StressedToCoda.Add(lastVowel, coda, word.Frequency);
            }
            else
            {
                UnstressedToCoda.Add(lastVowel, coda, word.Frequency);
            }
            return true;
        }

        private static Exception Fail()
        {
            // rdavidson todo: Stop it
            return new GenerationFailedException<Run>("Generation failed.", null!);
        }

        /// <summary>
        /// Generate a new random word based on the generator's 
        /// <see cref="LearnWord(Word)"/> method.
        /// </summary>
        /// <returns></returns>
        public Word GenerateWord()
        {
            List<Run> parts = new();
            StressPattern? stressPattern = StressPatterns.GetRandom() ?? throw Fail();

            Run onset;
            Run firstVowel;
            if (stressPattern.StressedIndex == 0)
            {
                onset = StressedOnset.GetRandom() ?? throw Fail();
                firstVowel = OnsetToStressed.GenerateContinuation(onset) ?? throw Fail();
            }
            else
            {
                onset = UnstressedOnset.GetRandom() ?? throw Fail();
                firstVowel = OnsetToUnstressed.GenerateContinuation(onset) ?? throw Fail();
            }
            parts.Add(onset);
            parts.Add(firstVowel);

            Run previousVowel = firstVowel;

            for (int i = 1; i<stressPattern.Count; i++)
            {
                Run intervocal;
                Run vowel;
                if (i == stressPattern.StressedIndex)
                {
                    intervocal = UnstressedToRising
                        .GenerateContinuation(previousVowel) 
                        ?? throw Fail();
                    vowel = RisingToStressed
                        .GenerateContinuation(intervocal)
                        ?? throw Fail();
                }
                else if (i - 1 == stressPattern.StressedIndex)
                {
                    intervocal = StressedToFalling
                        .GenerateContinuation(previousVowel)
                        ?? throw Fail();
                    vowel = FallingToUnstressed
                        .GenerateContinuation(intervocal)
                        ?? throw Fail();
                }
                else
                {
                    intervocal = UnstressedToFlat
                        .GenerateContinuation(previousVowel)
                        ?? throw Fail();
                    vowel = FlatToUnstressed
                        .GenerateContinuation(intervocal)
                        ?? throw Fail();
                }
                parts.Add(intervocal);
                parts.Add(vowel);
                previousVowel = vowel;
            }

            Run coda;
            if (stressPattern.StressedIndex == stressPattern.Count - 1)
            {
                coda = StressedToCoda
                    .GenerateContinuation(previousVowel)
                    ?? throw Fail();
            }
            else
            {
                coda = UnstressedToCoda
                    .GenerateContinuation(previousVowel)
                    ?? throw Fail();
            }
            parts.Add(coda);
            string text = string.Join('|', parts.Select(x => x.Symbol()));
            return new Word(text, parts, 1);
        }

        private Word TryGenerationUntilSuccessful(int maxTries)
        {
            Word name;
            int tries = 0;
            while (true)
            {
                try
                {
                    name = GenerateWord();
                    break;
                }
                catch (GenerationFailedException<Run>)
                {
                    tries++;
                    if (tries >= maxTries)
                    {
                        // After ten failures in a row, rethrow the exception
                        throw;
                    }
                }
            }

            return name;
        }

        public List<string> CreateArpabetNames(int wordCount, Dictionary<string, string> spellings)
        {
            List<string> arpabetStrings = new();
            string spelling = "";
            for (int j = 0; j < wordCount; j++)
            {
                Word word = TryGenerationUntilSuccessful(maxTries: 10);
                string spelledName = word.CreateSpelling(spellings);
                string arpabetWord = word.ToArpabet();
                arpabetStrings.Add(arpabetWord);
                //string ipaName = "placeholder";// translator.TranslateArpabetToIpaXml(arpabetName);

                if (spelling == "")
                {
                    spelling = spelledName.First().ToString().ToUpperInvariant() + spelledName[1..];
                }
                else
                {
                    spelling += " ";
                    spelling += spelledName;
                }
                //string spelledNameCapitalized = spelledName.First().ToString().ToUpperInvariant() + spelledName[1..];
            }
            if (wordCount == 1)
            {
                Console.WriteLine($"{spelling} - {arpabetStrings[0]}");
            }
            else
            {
                Console.WriteLine(spelling);
            }

            return arpabetStrings;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NameGenerator
{
    public class Word
    {
        public Word(string text, List<Run> runs, int frequency)
        {
            Text = text;
            Runs = runs;
            Frequency = frequency;
        }

        public IEnumerable<Phone> EnumeratePhones()
        {
            foreach (var run in Runs)
            {
                foreach (var phone in run.Phones)
                {
                    yield return phone;
                }
            }
        }

        // syllable breaks are represented by nulls (I'm drunk)
        public IEnumerable<Phone?> EnumeratePhonesAndBreaks()
        {
            Phone? syllableBreak = null;
            bool finalConsonant = (Runs[^1].Category() == RunCategory.Consonant);
            int forbiddenIndex = finalConsonant ? Runs.Count - 2 : Runs.Count - 1;
            for (int i_run = 0; i_run < forbiddenIndex; i_run++)
            {
                var currentRun = Runs[i_run];
                var nextRun = Runs[i_run + 1];
                if (currentRun.Category() == RunCategory.Vowel)
                {
                    foreach (Phone phone in currentRun.Phones)
                        yield return phone;
                    if (nextRun.Phones.Count == 1)
                    {
                        // If the next consonant run is just a single consonant, leave it alone as the onset
                        yield return syllableBreak;
                    }
                    else
                    {
                        // Otherwise absorb the first consonant of the following cluster into the coda
                        // of our own syllable.
                        yield return nextRun.Phones[0];
                        yield return syllableBreak;
                        foreach (Phone phone in nextRun.Phones.Skip(1))
                            yield return phone;
                        i_run += 1;
                    }
                }
                else
                {
                    foreach (Phone phone in currentRun.Phones)
                        yield return phone;
                }
            }

            // Finally, yield up all the phones we didn't cover earlier
            for (int i_run = forbiddenIndex; i_run < Runs.Count; i_run++)
            {
                foreach (Phone phone in Runs[i_run].Phones)
                    yield return phone;
            }
            //List<Phone> phones = EnumeratePhones().ToList();
            //int len = phones.Count;
            //bool vowelAt(int j) => j < len && phones[j].IsVowel();
            //bool consonantAt(int j) => j < len && !phones[j].IsVowel();
            //bool endsInConsonant() => !phones[^1].IsVowel();
            //for (int i = 0; i < len - 1; i++)
            //{
            //    yield return phones[i];
            //    // If there is a consonant cluster upcoming, move the first consonant in front of the
            //    // syllable break to create some form of coda (and hopefully make the cluster sound less complex)
            //    if (vowelAt(i) && consonantAt(i + 1) && consonantAt(i + 2) && )
            //    {
            //        yield return phones[i + 1];
            //        yield return syllableBreak;
            //        i += 1;
            //    }
            //    // Otherwise, if there is a vowel following the consonant phone, place the 
            //    else if (vowelAt(i) && consonantAt(i + 1) && vowelAt(i + 2))
            //    {
            //        yield return syllableBreak;
            //    }
            //}
        }

        public string ToArpabet()
        {
            List<string> output = new();
            foreach (Phone? phone in this.EnumeratePhonesAndBreaks())
            {
                if (phone is null)
                {
                    output.Add("-");
                }
                else
                {
                    string entry = phone.Code.ToLower();
                    // represent schwas by ax0, not ah0
                    if (entry == "ah0")
                    {
                        output.Add("ax0");
                    }
                    else
                    {
                        output.Add($"{entry}");
                    }
                }
            }
            return string.Join(' ', output);
        }

        public string Text { get; }
        public IReadOnlyList<Run> Runs { get; }
        public int Frequency { get; }

        public string SymbolizedRuns()
        {
            IEnumerable<string> runSymbols = Runs.Select(run => run.Symbol());
            return string.Join('|', runSymbols);
        }

        public string CreateSpelling(
            Dictionary<string, string> lookupTable,
            string separator = "",
            string primaryStressMarker = "",
            string secondaryStressMarker = "")
        {
            List<string> outputPieces = new();
            foreach (Run run in Runs)
            {
                foreach (Phone phone in run.Phones)
                {
                    string phoneSymbol = phone.Code;
                    if (phoneSymbol == "")
                    {
                        continue;
                    }
                    // Add IPA stress marks to account for ARPAbet stress annotations
                    if (phoneSymbol.EndsWith("1"))
                    {
                        outputPieces.Add(primaryStressMarker);
                    }
                    else if (phoneSymbol.EndsWith("2"))
                    {
                        outputPieces.Add(secondaryStressMarker);
                    }
                    // Then trim the stress annotations away
                    string trimmedSymbol = phoneSymbol.TrimEnd('0', '1', '2');

                    // Try the untrimmed symbol first, in case there is a special entry
                    // for a syllable with a specific stress.
                    if (lookupTable.TryGetValue(phoneSymbol, out string? translatedSymbol))
                    {
                        outputPieces.Add(translatedSymbol);
                    }
                    // otherwise use the trimmed version
                    else if (lookupTable.TryGetValue(trimmedSymbol, out translatedSymbol))
                    {
                        outputPieces.Add(translatedSymbol);
                    }
                    else
                    {
                        throw new Exception($"Unrecognized symbol {trimmedSymbol}, translation failed.");
                    }
                }

            }
            return string.Join(separator, outputPieces);
        }

        private static int ReadFrequency(string dictionaryEntry)
        {
            if (!dictionaryEntry.StartsWith('['))
                return 1;

            var parts = dictionaryEntry.Split(']');
            if (parts.Length < 2)
                return 1;

            var numeral = parts[0][1..];
            if (int.TryParse(numeral, out int result))
                return result;
            return 1;
        }
        
        public static Word? FromDictionaryLine(string line)
        {
            if (line.StartsWith(";;;"))
            {
                // Indicates a comment in cmudict syntax
                return null;
            }
            var parts = Regex.Split(line, @"\s+");
            if (parts.Length < 2)
            {
                return null;
            }

            int frequency = ReadFrequency(parts[0]);

            List<Phone> phones = new();
            for (int i = 1; i < parts.Length; i++)
            {
                phones.Add(new Phone(parts[i]));
            }

            List<Run> runs = new();
            int phoneIndex = 0;
            while (phoneIndex < phones.Count)
            {
                Run newRun = Run.ReadFromPhoneList(phones, ref phoneIndex);
                runs.Add(newRun);
            }
            if (runs[0].Category() != RunCategory.Consonant)
            {
                // Enforce the constraint that the first entry is *always* a consonant run,
                // (possibly empty, as in this case)
                runs.Insert(0, new Run(new List<Phone>()));
            }
            if (runs[^1].Category() != RunCategory.Consonant)
            {
                // Likewise for the *last* entry
                runs.Add(new Run(new List<Phone>()));
            }

            return new Word(parts[0], runs, frequency);
        }

        public override bool Equals(object? obj)
        {
            return obj is Word word &&
                   Text == word.Text &&
                   word.SymbolizedRuns() == SymbolizedRuns();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text, SymbolizedRuns());
        }
    }
}

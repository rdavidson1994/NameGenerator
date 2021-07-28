using System;
using System.Collections.Generic;
using System.Linq;
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

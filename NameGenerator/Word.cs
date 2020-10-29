using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NameGenerator
{
    public class Word
    {
        public Word(string text, List<Run> runs)
        {
            Text = text;
            Runs = runs;
        }

        public string Text { get; }
        public IReadOnlyList<Run> Runs { get; }
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
            List<string> outputPieces = new List<string>();
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
                    if (lookupTable.TryGetValue(trimmedSymbol, out string? translatedSymbol))
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
            List<Phone> phones = new List<Phone>();
            for (int i = 1; i < parts.Length; i++)
            {
                phones.Add(new Phone(parts[i]));
            }

            List<Run> runs = new List<Run>();
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
            if (runs[runs.Count - 1].Category() != RunCategory.Consonant)
            {
                // Likewise for the *last* entry
                runs.Add(new Run(new List<Phone>()));
            }

            return new Word(parts[0], runs);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace NameGenerator
{
    public class Program
    {
        static readonly string cmuDictFilename = @"cmudict-0.7b.txt";
        static void SayName(string name)
        {
            Process process = Process.Start(@"NameSayer.exe", name);
            process.WaitForExit();
        }

        static void Main(string[] args)
        {
            int count;
            if (args.Length == 0)
            {
                count = 100;
            }
            else
            {
                count = Int32.Parse(args[0]);
            }
            List<Word> words = File.ReadLines(cmuDictFilename)
                .Select(line => Word.FromDictionaryLine(line))
                .WhereNotNull()
                .ToList();

            WordGenerator wordGenerator = new WordGenerator();

            foreach (Word word in words)
            {
                wordGenerator.LearnWord(word);
            }

            Console.WriteLine($"Done! Read {words.Count} words from dictionary.");


            ArpabetTranslator translator;
            using (var fs = new FileStream(@"arpabet-to-ipa.json", FileMode.Open, FileAccess.Read))
            {
                translator = ArpabetTranslator.FromStream(fs);
            }

            Dictionary<string, string> spellings = JsonUtils.DictionaryFromJsonFile("arpabet-to-spelling.json");
            //= ArpabetTranslator.FromStream()

            Console.WriteLine("Generating names:");
            for (int i = 0; i < count; i++)
            {
                Word name;
                int tries = 0;
                while (true)
                {
                    try
                    {
                        name = wordGenerator.GenerateName();
                        break;
                    }
                    catch (GenerationFailedException<Run>)
                    {
                        tries++;
                        if (tries >= 10)
                        {
                            // After ten failures in a row, rethrow the exception
                            throw;
                        }
                    }
                }
                string arpabetName = name.SymbolizedRuns();
                string ipaName = translator.TranslateArpabetToIpaXml(name.SymbolizedRuns());
                string spelledName = name.CreateSpelling(spellings);
                string spelledNameCapitalized = spelledName.First().ToString().ToUpperInvariant() + spelledName.Substring(1);

                Console.WriteLine($"{spelledNameCapitalized}: {arpabetName}");
                SayName(ipaName);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using CommandLine;
using System.Text.Json;

namespace NameGenerator
{
    public class Program
    {
        static void SayName(string name)
        {
            Process process = Process.Start(@"NameSayer.exe", name);
            process.WaitForExit();
        }

        static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<CommandLineOptions>(args)
                .WithParsed(ExecuteOptions);
        }

        static void ExecuteOptions(CommandLineOptions options)
        {
            WordGenerator wordGenerator;
            if (options.ModelInput != null && File.Exists(options.ModelInput))
            {
                Console.WriteLine("Importing WordGenerator from file...");
                string jsonString = File.ReadAllText(options.ModelInput);
                wordGenerator = WordGenerator.ImportFromJson(jsonString);
            }
            else if (options.TrainingInput != null)
            {
                List<Word> words = File.ReadLines(options.TrainingInput)
                    .Select(line => Word.FromDictionaryLine(line))
                    .WhereNotNull()
                    .ToList();

                wordGenerator = new WordGenerator();
                foreach (Word word in words)
                {
                    wordGenerator.LearnWord(word);
                }

                Console.WriteLine($"Done! Read {words.Count} words from dictionary.");
                if (options.ModelOutput != null)
                {
                    File.WriteAllText(options.ModelOutput, wordGenerator.ToJson());
                }
            }
            else
            {
                throw new Exception("Pre-trained model not found or not specified, and no training input given.");
            }

            ArpabetTranslator translator;
            using (var fs = new FileStream(@"arpabet-to-ipa.json", FileMode.Open, FileAccess.Read))
            {
                translator = ArpabetTranslator.FromStream(fs);
            }
            if (options.SpellingInput == null)
            {
                throw new Exception("spelling-input argument must be provided.");
            }
            Dictionary<string, string> spellings = JsonUtils.DictionaryFromJsonFile(options.SpellingInput);

            Console.WriteLine("Generating names:");
            for (int i = 0; i < options.Quantity; i++)
            {
                Word name = TryGenerationUntilSuccessful(wordGenerator, maxTries: 10);
                string arpabetName = name.SymbolizedRuns();
                string ipaName = translator.TranslateArpabetToIpaXml(arpabetName);
                string spelledName = name.CreateSpelling(spellings);
                string spelledNameCapitalized = spelledName.First().ToString().ToUpperInvariant() + spelledName.Substring(1);

                Console.WriteLine($"{spelledNameCapitalized}: {arpabetName}");
                //Console.WriteLine(ipaName);
                if (options.Speak)
                {
                    SayName(ipaName);
                }
            }
        }

        private static Word TryGenerationUntilSuccessful(WordGenerator wordGenerator, int maxTries)
        {
            Word name;
            int tries = 0;
            while (true)
            {
                try
                {
                    name = wordGenerator.GenerateWord();
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
    }
}

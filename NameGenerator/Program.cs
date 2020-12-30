using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using CommandLine;
using System.Text.Json;

namespace NameGenerator
{
    public class CommandLineOptions
    {
        [Option("model-input",
            HelpText="Input .json file for pre-trained model."
            // ,Default ="model.json"
        )]
        public string? ModelInput { get; set; }

        [Option("model-output",
            HelpText="Output .json file for trained model.",
            Default ="model.json")]
        public string? ModelOutput { get; set; }

        [Option("training-input",
            HelpText="Input .txt file for training corpus.",
            Default = @"cmu-names.txt")]
        public string? TrainingInput { get; set; }

        [Option("spelling-input",
            HelpText = "Input .json file for ARPAbet to glyph translation",
            Default = "arpabet-to-spelling.json")]
        public string? SpellingInput { get; set; }

        [Option("name-output",
            HelpText = "Output .txt file for generated names.")]
        public string? NameOutput { get; set; }

        [Option("speak",
            HelpText = "Speak generated names aloud via default audio device.",
            Default = false
        )]
        public bool Speak { get; set; }

        [Option("quantity",
            HelpText = "Number of names to generate.",
            Default = 1
        )]
        public int Quantity { get; set; }
        
    }
    public class Program
    {
        static void ExportGenerator(WordGenerator generator, string path)
        {
            string jsonString = JsonSerializer.Serialize(generator);
            File.WriteAllText(path, jsonString);
        }

        static WordGenerator ImportGenerator(string path)
        {
            string jsonString = File.ReadAllText(path);
            return JsonSerializer.Deserialize<WordGenerator>(jsonString);
        }
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
                wordGenerator = ImportGenerator(options.ModelInput);
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
                    ExportGenerator(wordGenerator, options.ModelOutput);
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
                //Console.WriteLine(ipaName);
                if (options.Speak)
                {
                    SayName(ipaName);
                }
            }
        }
    }
}

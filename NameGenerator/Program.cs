using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using CommandLine;
using System.Text.Json;
using System.Text;

namespace NameGenerator
{
    public class Program
    {
        static void SayName(string name)
        {
            Process process = Process.Start(@"NameSayer.exe", name);
            process.WaitForExit();
        }

        static void SayNameFlite(bool flatStress, params string[] arpabetNames)
        {
            string utterance = SanitizeArpabet(arpabetNames);

            // Render all syllables equal stress for languages like Japanese
            // (pitch accent not implemented yet)
            if (flatStress)
            {
                utterance = utterance.Replace('1', '0');
            }


            // Surround the utterence by pauses, so the output doesn't sound abrupt.
            utterance = "pau " + utterance + " pau";//+ " - pau";
            string voice = "awb";// "slp";
            double durationStretch = flatStress ? 1.0 : 1.5;
            Process process = Process.Start(@"flite.exe",
                $"-p \"{utterance}\" " +
                $"-voice .\\voices\\cmu_us_{voice}.flitevox " +
                $"-set duration_stretch={durationStretch}");
            process.WaitForExit();
        }

        private static string SanitizeArpabet(params string[] arpabetWords)
        {
            StringBuilder output = new();
            bool firstPass = true;
            foreach (string arpabetWord in arpabetWords)
            {
                if (firstPass)
                {
                    firstPass = false;
                }
                else
                {
                    output.Append(" - ");
                }
                string utterance = arpabetWord
                .Trim('|') // Discard any pipes that mark "empty" coda/onset
                .Replace('|', ' ') // Replace other delimiters with " "
                .Replace('-', ' ')
                .ToLowerInvariant() // Convert to lowercase
                .Replace('2', '0'); // Reduce secondary stress to non-stress.

                // Represent nonfinal schwas with "AX" instead of "AH".
                utterance = Regex.Replace(utterance, "ah0(?!$)", "ax0");

                // Follow all vowels except the last with a syllable break (hyphen)
                bool skippedFirstMatch = false;
                utterance = Regex.Replace(utterance, @"(\d)", (match) =>
                {
                    if (skippedFirstMatch)
                    {
                        return match.Groups[0].Value + " - ";
                    }
                    else
                    {
                        skippedFirstMatch = true;
                        return match.Groups[0].Value;
                    }
                }, RegexOptions.RightToLeft);

                output.Append(utterance);
            }
            return output.ToString();
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
                List<string> arpabetStrings = new();
                string spelling = "";
                for (int j = 0; j < options.WordCount; j++)
                {
                    Word word = TryGenerationUntilSuccessful(wordGenerator, maxTries: 10);
                    string arpabetWord = word.SymbolizedRuns();
                    arpabetStrings.Add(arpabetWord);
                    //string ipaName = "placeholder";// translator.TranslateArpabetToIpaXml(arpabetName);
                    string spelledName = word.CreateSpelling(spellings);
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
                if (options.WordCount == 1)
                {
                    Console.WriteLine($"{spelling} - {arpabetStrings[0]}");
                }
                else
                {
                    Console.WriteLine(spelling);
                }
                SayNameFlite(options.FlatStress, arpabetStrings.ToArray());

                //Console.WriteLine($"{spelledNameCapitalized} - {arpabetName}");
                //if (options.Speak)
                //{
                //    switch (options.SpeechEngine)
                //    {
                //        case "flite":
                //            SayNameFlite(options.FlatStress, arpabetName);
                //            break;
                //        default: // including "windows"
                //            SayName(ipaName);
                //            break;
                //    }
                //}
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

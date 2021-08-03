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

        static void SayUtteranceFlite(string utterance, string voice, double durationStretch)
        {
            Process process = Process.Start(@"flite.exe",
                $"-p \"{utterance}\" " +
                $"-voice {voice} " +
                //$"-voice .\\voices\\cmu_us_{voice}.flitevox " +
                $"-set duration_stretch={durationStretch}");
            process.WaitForExit();
        }

        //static void SayNameFlite(bool flatStress, params string[] arpabetNames)
        //{
        //    string utterance = SanitizeArpabet(arpabetNames);

        //    // Render all syllables equal stress for languages like Japanese
        //    // (pitch accent not implemented yet)
        //    if (flatStress)
        //    {
        //        utterance = utterance.Replace('1', '0');
        //    }


        //    // Surround the utterence by pauses, so the output doesn't sound abrupt.
        //    utterance = "pau " + utterance + " pau";//+ " - pau";
        //    string voice = "awb";// "slp";
        //    double durationStretch = flatStress ? 1.0 : 1.5;
        //    Process process = Process.Start(@"flite.exe",
        //        $"-p \"{utterance}\" " +
        //        $"-voice slt " +
        //        //$"-voice .\\voices\\cmu_us_{voice}.flitevox " +
        //        $"-set duration_stretch={durationStretch}");
        //    process.WaitForExit();
        //}

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
                // utterance = Regex.Replace(utterance, "ah0(?!$)", "ax0");

                //List<string> piecesWithSyllables = new();
                //string[] pieces = utterance.Split(' ');
                //List<string> insertSyllableBreakIndexes = new();
                //for (int i = 0; i < pieces.Length - 1; i++)
                //{
                //    if (char.IsDigit(pieces[i][^1]))
                //    {
                //        if (char.IsDigit(pieces[i+1][^1]))
                //        {
                //            continue;
                //        }
                //    }
                //}

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
                Console.WriteLine(utterance);
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
                List<string> arpabetStrings = wordGenerator.CreateArpabetNames(options.WordCount, spellings);
                var joined = "pau "+string.Join(" -word- ", arpabetStrings)+" pau";
                Console.WriteLine(joined);
                //joined = joined.Replace("ah0", "ax0");
                double durationStretch = options.FlatStress ? 1.0 : 1.5;

                string voiceAwb = ".\\voices\\cmu_us_awb.flitevox";


                string voice = voiceAwb;
                SayUtteranceFlite(joined, voice, durationStretch);
                //SayWordsFlite(options.FlatStress,)
            }
        }



       
    }
}

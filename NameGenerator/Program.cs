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
            //= ArpabetTranslator.FromStream()

            Console.WriteLine("Generating names:");
            for (int i = 0; i < 100; i++)
            {
                string? name = wordGenerator.GenerateName();
                Console.WriteLine(name);
                if (name == null)
                {
                    throw new Exception("I messed up");
                }
                string ipaName = translator.TranslateArpabetToIpaXml(name);
                SayName(ipaName);
            }

        }
    }
}

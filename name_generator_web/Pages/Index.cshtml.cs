using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NameGenerator;

namespace name_generator_web.Pages
{
    public enum TranscriptionKind
    {
        IPA,
        Arpabet
    }
    public record NameData(string Spelling, string Transcription, Guid Id);
    public class IndexModel : PageModel
    {
        [BindProperty]
        public int Quantity { get; set; }

        [BindProperty]
        public TranscriptionKind Transcription { get; set; }

        [BindProperty]
        public FliteVoice Voice { get; set; }

        [BindProperty]
        public List<NameData> GeneratedNames { get; set; } = new();

        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private const string WORD_GENERATOR = nameof(WORD_GENERATOR);
        private const string JokeGuid = "33e6f70a-480e-415a-a37b-4cf0c1915656";

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment env, IMemoryCache cache)
        {
            _logger = logger;
            _env = env;
            _cache = cache;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            
            await Task.Run(() =>
            {
                
                //string binPath = _env.
                var runDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var spellings = JsonUtils.DictionaryFromJsonFile($"{runDir}/arpabet-to-spelling.json");
                var ipaLookup = JsonUtils.DictionaryFromJsonFile($"{runDir}/arpabet-to-ipa-pretty.json");
                

                WordGenerator wordGenerator = _cache.GetOrCreate(WORD_GENERATOR, entry => {
                    var json = System.IO.File.ReadAllText(@"model.json");
                    return WordGenerator.ImportFromJson(json);
                });

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                //foreach (Word word in words)
                //{
                //    wordGenerator.LearnWord(word);
                //}
                if (Quantity < 0 || Quantity > 99)
                {
                    GeneratedNames.Add(new NameData("No.", "", Guid.Parse(JokeGuid)));
                    Quantity = 0;
                }

                for (int i = 0; i < Quantity; i++)
                {
                    var word = wordGenerator.TryGenerationUntilSuccessful(10);
                    var spelling = word.CreateSpelling(spellings);
                    spelling = textInfo.ToTitleCase(spelling);
                    var ipa = word.CreateSpelling(ipaLookup);
                    var arpabet = word.ToArpabet();

                    string transcriptionText;
                    if (Transcription == TranscriptionKind.IPA)
                    {
                        transcriptionText = ipa;
                    }
                    else
                    {
                        transcriptionText = arpabet;
                    }
                    Guid guid = Guid.NewGuid();
                    // TODO - use a real database you dork
                    Directory.CreateDirectory("transcriptions");
                    System.IO.File.WriteAllText($"transcriptions/{guid}.txt", arpabet);
                    GeneratedNames.Add(new NameData(spelling, transcriptionText, guid));
                }

                string[] files = Directory.GetFiles("transcriptions");

                foreach (string file in files)
                {
                    // Delete transcription files older than 1 hour, except the funny one.
                    FileInfo fi = new FileInfo(file);
                    if (
                        fi.LastAccessTime < DateTime.Now.AddHours(-1)
                        && !fi.Name.Contains(JokeGuid)
                    )
                    {
                        fi.Delete();
                    }
                }
            });
            return Page();
        }
    }
}

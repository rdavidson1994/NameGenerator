using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace name_generator_web
{
    [Route("api/audio")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        public static List<string> VoiceFileNames { get; } = new()
        {
            "cmu_us_aew.flitevox",
            "cmu_us_ahw.flitevox",
            "cmu_us_aup.flitevox",
            "cmu_us_awb.flitevox",
            "cmu_us_axb.flitevox",
            "cmu_us_bdl.flitevox",
            "cmu_us_clb.flitevox",
            "cmu_us_eey.flitevox",
            "cmu_us_fem.flitevox",
            "cmu_us_gka.flitevox",
            "cmu_us_jmk.flitevox",
            "cmu_us_ksp.flitevox",
            "cmu_us_ljm.flitevox",
            "cmu_us_lnh.flitevox",
            "cmu_us_rms.flitevox",
            "cmu_us_rxr.flitevox",
            "cmu_us_slp.flitevox",
            "cmu_us_slt.flitevox"
        };


        [HttpGet("{voiceIndex}/{guid}")]
        public async Task<ActionResult> Download(int voiceIndex, Guid guid)
        {
            if (voiceIndex < 0 || voiceIndex >= VoiceFileNames.Count)
            {
                throw new ArgumentException($"Selected voice {voiceIndex} is out of bounds.");
            }
            string voiceFile = VoiceFileNames[voiceIndex];
            Directory.CreateDirectory("audios");
            string audioPath = $"audios/{voiceFile}-{guid}.mp3";
            string arpabetPath = $"transcriptions/{guid}.txt";
            if (!System.IO.File.Exists(audioPath))
            {
                if (!System.IO.File.Exists(arpabetPath)) {
                    throw new ArgumentException($"The audio file for {guid} is expired or does not exist.", nameof(guid));
                }
                string utterance = await System.IO.File.ReadAllTextAsync(arpabetPath);
                await Task.Run(() =>
                {
                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    string binDir = Path.GetDirectoryName(executingAssembly.Location);
                    string flitePath = Path.Join(binDir, "flite.exe");
                    string voicePath = Path.Join(binDir, voiceFile);
                    Process process = Process.Start(flitePath,
                        $"-p \"{utterance}\" " +
                        $"-voice {voicePath} " +
                        $"-o {audioPath}");
                    process.WaitForExit();
                });
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(audioPath);
            try
            {
                System.IO.File.Delete(audioPath);
            }
            catch (IOException)
            {
                
            }
            return File(bytes, "audio/mpeg", $"{guid}");
        }
    }
}

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
    public enum FliteVoice
    {
        AWB,
        SLT
    }

    public static class FliteVoiceExtensions
    {
        public static string CommandLineArg(this FliteVoice voice)
        {
            return voice switch
            {
                FliteVoice.SLT => "slt",
                FliteVoice.AWB => "awb",
                _ => null
            };
        }
    }


    [Route("api/audio")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        [HttpGet("{voice}/{guid}")]
        public async Task<ActionResult> Download(FliteVoice voice, Guid guid)
        {
            string voiceArg = voice.CommandLineArg();

            Directory.CreateDirectory("audios");
            string audioPath = $"audios/{voiceArg}-{guid}.mp3";
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
                    Process process = Process.Start(flitePath,
                        $"-p \"{utterance}\" " +
                        $"-voice {voiceArg} " +
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Threading;

namespace NameSayer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize a new instance of the SpeechSynthesizer.  
            SpeechSynthesizer synth = new SpeechSynthesizer();

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();
            foreach (string nameToSay in args)
            {
                string str =
                    $@"<?xml version='1.0'?>
                    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis'
                            xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                            xsi:schemaLocation='http://www.w3.org/2001/10/synthesis
                                        http://www.w3.org/TR/speech-synthesis11/synthesis.xsd'
                            xml:lang='en-US'>
                        <phoneme alphabet='ipa' ph='{nameToSay}'>placeholder</phoneme>
                    </speak>";
                synth.SpeakSsml(str);
            }
        }
    }
}

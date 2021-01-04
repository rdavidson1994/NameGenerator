using CommandLine;

namespace NameGenerator
{
    public class CommandLineOptions
    {
        [Option("model-input",
            HelpText="Input .json file for pre-trained model."
            //,Default ="model.json"
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
            Default = true
        )]
        public bool Speak { get; set; }

        [Option("quantity",
            HelpText = "Number of names to generate.",
            Default = 10
        )]
        public int Quantity { get; set; }
        
    }
}

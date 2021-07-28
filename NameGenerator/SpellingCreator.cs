using System;
using System.Collections.Generic;

namespace NameGenerator
{
    public record Phoneme(List<List<Phone>> Variants, string Representation);

    /// <summary>
    /// Consider
    /// </summary>
    public class Phonemizer
    {
        public Phonemizer(List<Tuple<List<Phone>, List<Phone>>> mappings)
        {
            Mappings = mappings;
        }

        public List<Tuple<List<Phone>, List<Phone>>> Mappings {get;}
    }
    public class SpellingCreator
    {

    }
}

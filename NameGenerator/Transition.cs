using System;

namespace NameGenerator
{
    public class Transition
    {
        public Transition(Run consonant, Run vowel, bool vowelIsStressed, bool vowelFirst)
        {
            Consonant = consonant;
            Vowel = vowel;
            VowelIsStressed = vowelIsStressed;
            VowelFirst = vowelFirst;
        }

        public Run Consonant { get; }
        public Run Vowel { get; }
        public bool VowelIsStressed { get; }
        public bool VowelFirst { get; }

        public override bool Equals(object? obj)
        {
            return obj is Transition transition &&
                   Vowel.Symbol() == transition.Vowel.Symbol() &&
                   Consonant.Symbol() == transition.Consonant.Symbol() &&
                   VowelIsStressed == transition.VowelIsStressed;
        }

        public string Symbol()
        {
            if (VowelFirst)
            {
                return Vowel.Symbol() + "|" + Consonant.Symbol();
            }
            else
            {
                return Consonant.Symbol() + "|" + Vowel.Symbol();
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Consonant.Symbol(), Vowel.Symbol(), VowelIsStressed, VowelFirst);
        }
    }
}

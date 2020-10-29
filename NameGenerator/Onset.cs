using System;
using System.Collections.Generic;

namespace NameGenerator
{
    public class Onset
    {
        public Onset(bool precedesStress, Run? run)
        {
            PrecedesStress = precedesStress;
            Run = run;
        }

        public static Onset ReadFromWord(Word word, out int index)
        {
            Run firstRun = word.Runs[0];
            if (firstRun.Category() == RunCategory.Vowel)
            {
                // Onset is *absent* (word begins with a vowel)
                // Set index to zero (no scanning necessary to reach first vowel)
                // The absent onset precedes stress if this vowel is stressed
                bool precedesStress = firstRun.HasPrimaryStress();
                // The onset is indicated as "absent" because its Run is null
                index = 0;
                return new Onset(precedesStress, null);
            }
            else
            {
                // Onset is present (word begins with a consonant run)
                // Set index to 1 (must scan past one run to reach first vowel)
                index = 1;
                Run followingVowelRun = word.Runs[1];
                if (followingVowelRun.Category() != RunCategory.Vowel)
                {
                    throw new Exception("Word does not consist of alternating consonant and vowel runs.");
                }
                // Onset precedes stress if the following vowel is stressed
                bool precedesStress = followingVowelRun.HasPrimaryStress();
                // Record the first (consonant) run as the run for this onset
                return new Onset(precedesStress, firstRun);
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is Onset onset)
            {
                if (PrecedesStress != onset.PrecedesStress)
                {
                    // Returnt false if stress doesn't match
                    return false;
                }
                
                if (Run == null)
                {
                    // If this is an "absent" onset (Run==null),
                    // return true if-and-only-if the other onset is also "absent"
                    return onset.Run == null;
                }
                // Otherwise, return true if our runs are equal.
                return Run.Equals(onset.Run);  
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PrecedesStress, Run?.GetHashCode() ?? 0);
        }

        public bool PrecedesStress { get; }
        // Can be null if the onset is *absent* - e.g. the word "about"
        public Run? Run { get; }
    }
}

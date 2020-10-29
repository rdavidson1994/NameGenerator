using System;

namespace NameGenerator
{
    public class Intervocal
    {
        public Intervocal(Run run, bool precedesStress, bool followsStress)
        {
            Run = run;
            PrecedesStress = precedesStress;
            FollowsStress = followsStress;
        }

        public Run Run {get;}
        public bool PrecedesStress { get; }
        public bool FollowsStress { get; }

        public static Intervocal? ReadFromWord(Word word, ref int index)
        {
            // if we can't read the following vowal, is is a *coda*, not an intervocal
            if (index + 1 >= word.Runs.Count)
            {
                return null;
            }
            Run previousVowel = word.Runs[index - 1];
            Run nextVowel = word.Runs[index + 1];
            Run run = word.Runs[index];
            bool precedesStress = nextVowel.HasPrimaryStress();
            bool followsStress = previousVowel.HasPrimaryStress();
            // we were able to read an intervocal from here, so increment the index
            index += 1;
            return new Intervocal(run, precedesStress, followsStress);
        }

        public override bool Equals(object? obj)
        {
            return obj is Intervocal intervocal &&
                   Run.Symbol() == intervocal.Run.Symbol() &&
                   PrecedesStress == intervocal.PrecedesStress &&
                   FollowsStress == intervocal.FollowsStress;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Run, PrecedesStress, FollowsStress);
        }

    }
}

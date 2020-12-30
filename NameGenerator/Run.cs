using System;
using System.Collections.Generic;
using System.Linq;

namespace NameGenerator
{
    public interface IStringSymbol
    {
        string Symbol();
    }
    public class Run : IStringSymbol 
    {
        public Run(List<Phone> phones)
        {
            Phones = phones;
        }

        public string Symbol()
        {
            if (Phones.Count == 0)
            {
                return "";
            }
            return string.Join('-', Phones.Select(x=>x.Code));
        }

        public RunCategory Category()
        {
            if (Phones.Count == 0)
            {
                return RunCategory.Consonant;
            }
            return Phones.First().Category();
        }

        public bool HasPrimaryStress()
        {
            return Phones.Any(x => x.HasPrimaryStress());
        }

        public List<Phone> Phones { get; }

        /// <summary>
        /// Reads a run from the phone list, starting at "index". Index is incremented once for each phone read.
        /// </summary>
        /// <param name="phones">List of phones to read from</param>
        /// <param name="index">Index to start at</param>
        /// <returns></returns>
        public static Run ReadFromPhoneList(IList<Phone> phones, ref int index)
        {
            List<Phone> matchingPhones = new List<Phone>();
            if (index < 0 || index >= phones.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            RunCategory initialCategory = phones[index].Category();
            
            while (true)
            {
                Phone nextPhone = phones[index];
                if (nextPhone.Category() != initialCategory)
                {
                    break;
                }
                matchingPhones.Add(nextPhone);
                index++;
                if (index == phones.Count)
                {
                    break;
                }
            }
            return new Run(matchingPhones);
        }

        public override bool Equals(object? obj)
        {
            return obj is Run run &&
                   run.Symbol() == this.Symbol();

        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Symbol());
        }
    }
}

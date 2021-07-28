using System;

namespace NameGenerator
{
    public class Phone
    {
        public Phone(string code)
        {
            Code = code;
        }

        public string Code { get; }

        public bool HasPrimaryStress()
        {
            char lastChar = Code[Code.Length - 1];
            return lastChar == '1';
        }

        public RunCategory Category()
        {
            char lastChar = Code[Code.Length - 1];
            if (char.IsDigit(lastChar))
            {
                return RunCategory.Vowel;
            }
            else
            {
                return RunCategory.Consonant;
            }
        }

        public bool IsVowel() => Category() == RunCategory.Vowel;

        public override bool Equals(object? obj)
        {
            return obj is Phone phone &&
                   Code == phone.Code;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Code);
        }
    }
}

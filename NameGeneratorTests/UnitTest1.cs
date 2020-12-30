using NUnit.Framework;
using NameGenerator;
using System.Collections.Generic;
using System.Linq;

namespace NameGeneratorTests
{
    public class Tests
    {
        [TestCase("M", RunCategory.Consonant)]
        [TestCase("KH", RunCategory.Consonant)]
        [TestCase("UH1", RunCategory.Vowel)]
        [TestCase("ER0", RunCategory.Vowel)]
        public void TestCategory(string code, RunCategory expectedCategory)
        {
            Phone phone = new Phone(code);
            Assert.AreEqual(expectedCategory, phone.Category());
        }

        [TestCase(
            new object[] { "HH", "AY1" }, 
            0, 
            new object[] { "HH" },
            1)]
        [TestCase(
            new object[] { "HH", "AY1" },
            1,
            new object[] { "AY1" },
            2)]
        [TestCase(
            new object[] { "S", "T", "R", "AY1", "V" },
            0,
            new object[] { "S", "T", "R" },
            3)]
        [TestCase(
            new object[] { "S", "T", "R", "AY1", "V" },
            3,
            new object[] { "AY1" },
            4)]
        [TestCase(
            new object[] { "S", "T", "R", "AY1", "V" },
            4,
            new object[] { "V" },
            5)]
        public void TestReadPhoneList(object [] input, int index, object[] expectedFirstRun, int expectedFinalIndex)
        {
            IEnumerable<string> expectedFirstRunAsStrings = expectedFirstRun.Select(x => (string)x);
            List<Phone> phoneList = input.Select(x => new Phone((string)x)).ToList();
            Run run = Run.ReadFromPhoneList(phoneList, ref index);
            IEnumerable<string> outputCodes = run.Phones.Select(x => x.Code);
            CollectionAssert.AreEqual(expectedFirstRunAsStrings, outputCodes);
            Assert.AreEqual(expectedFinalIndex, index);
        }

        [Test]
        public void TestFromDictionaryLine_CommentsReturnNull()
        {
            Word word = Word.FromDictionaryLine(";;; Some random stuff here");
            Assert.IsNull(word);
        }


        [TestCase("HELLO  HH AH0 L OW1", "HELLO", 5)]
        [TestCase("JOLIET  JH OW1 L IY0 EH2 T", "JOLIET", 5)]
        public void TestReadDictionaryLine(string line, string wordText, int runCount)
        {
            Word word = Word.FromDictionaryLine(line);
            Assert.AreEqual(wordText, word.Text);
            Assert.AreEqual(word.Runs.Count(), runCount);
        }

        [TestCase("HELLO  HH AH0 L OW1", "aA")]
        [TestCase("JURISDICTIONS  JH UH2 R IH0 S D IH1 K SH AH0 N Z", "aaAa")]
        [TestCase("ACADEMIA  AE2 K AH0 D IY1 M IY0 AH0", "aaAa")]
        public void Test_StressPatternFromWord(string input, string expected)
        {
            Word word = Word.FromDictionaryLine(input);
            StressPattern stress = StressPattern.FromWord(word);
            Assert.AreEqual(expected, stress.Symbol());
        }

        [TestCase("CMU  S IY1 EH1 M Y UW1")] // multiple primary stress
        [TestCase("THE  DH AH0")] // No stress
        public void Test_StressPatternFromWord_BadInputsReturnNull(string input)
        {
            Word word = Word.FromDictionaryLine(input);
            StressPattern stress = StressPattern.FromWord(word);
            Assert.IsNull(stress);
        }

        [Test]
        public void TestRunsUseValueEquality()
        {
            Run run1 = new Run(new List<Phone> { new Phone("HH") });
            Run run2 = new Run(new List<Phone> { new Phone("HH") });
            Assert.True(run1.Equals(run2));
        }

        [Test]
        public void TestOnsetsUseValueEquality()
        {
            Onset onset1 = Onset.ReadFromWord(Word.FromDictionaryLine("HELLO  HH AH0 L OW1"), out int _);
            Onset onset2 = Onset.ReadFromWord(Word.FromDictionaryLine("HURRAY  HH AH0 R EY1"), out int _);
            Assert.True(onset1.Equals(onset2));
        }

        [Test]
        public void TestCounterFirstAddSucceeds()
        {
            Counter<string> counter = new Counter<string>();
            counter.Add("Hello");
            Assert.AreEqual(counter.Count("Hello"), 1);
        }

        [Test]
        public void TestCounterSecondAddSucceeds()
        {
            Counter<string> counter = new Counter<string>();
            counter.Add("Hello");
            counter.Add("Hello");
            Assert.AreEqual(counter.Count("Hello"), 2);
        }

        public void TestCounterEnumerableIsStacked()
        {
            Counter<string> counter = new Counter<string>();
            counter.Add("Hello");
            counter.Add("Hello");
            counter.Add("World");
            counter.Add("World");
            counter.Add("World");
            List<KeyValuePair<string, int>> asList = counter.ToList();
            var hellos = asList.Where(kvp => kvp.Key == "Hello");
            var worlds = asList.Where(kvp => kvp.Key == "World");
            Assert.AreEqual(2, asList.Count());
            Assert.AreEqual(1, hellos.Count());
            Assert.AreEqual(1, worlds.Count());
            Assert.AreEqual(2, hellos.First().Value);
            Assert.AreEqual(3, worlds.First().Value);
        }

        [Test]
        public void TestOnsetCounterUsesValueEquality()
        {
            Onset onset1 = Onset.ReadFromWord(Word.FromDictionaryLine("HELLO  HH AH0 L OW1"), out int _);
            Onset onset2 = Onset.ReadFromWord(Word.FromDictionaryLine("HURRAY  HH AH0 R EY1"), out int _);
            Counter<Onset> onsetCounter = new Counter<Onset>();
            onsetCounter.Add(onset1);
            onsetCounter.Add(onset2);
            Assert.AreEqual(1, onsetCounter.Count());
            Assert.AreEqual(2, onsetCounter.Total);
        }

        [Test]
        public void TestOnsetCounterUsesValueEquality_UnequalCase()
        {
            Onset onset1 = Onset.ReadFromWord(Word.FromDictionaryLine("HELLO  HH AH0 L OW1"), out int _);
            Onset onset2 = Onset.ReadFromWord(Word.FromDictionaryLine("SPROCKET  S P R AA1 K AH0 T"), out int _);
            Counter<Onset> onsetCounter = new Counter<Onset>();
            onsetCounter.Add(onset1);
            onsetCounter.Add(onset2);
            Assert.AreEqual(2, onsetCounter.Count());
            Assert.AreEqual(2, onsetCounter.Total);
        }

        [TestCase("HELLO  HH AH0 L OW1", "HH|AH0|L|OW1|")]
        public void Test_WordGenerator_TaughtOneWord_GeneratesThatWord(string dictionaryLine, string expectedOutput)
        {
            WordGenerator wordGenerator = new WordGenerator();
            Word hello = Word.FromDictionaryLine(dictionaryLine);
            wordGenerator.LearnWord(hello);
            string generatedOutput = wordGenerator.GenerateWord().SymbolizedRuns();
            Assert.AreEqual(expectedOutput, generatedOutput);
        }

        [TestCase("HELLO  HH AH0 L OW1",
            0,1,
            0,1,
            0,1,0,
            0,0,1,
            0,1)]
        public void TestWordGeneratorLearnWordResultsInExpectedCounts(
            string dictionaryLine,

            int expectedPreStressOnsets,
            int expectedUnstressedOnsets,

            int expectedPreStressOnsetToStressedVowel,
            int expectedUnstressedOnsetToUnstressedVowel,

            int expectedStressedVowelToPostStressIntervocal,
            int expectedUnstressedVowelToPreStressIntervocal,
            int expectedUnstressedVowelToUnstressedIntervocal,

            int expectedPostStressIntervocalToUnstressedVowel,
            int expectedUnstressedIntervocalToUnstressedVowel,
            int expectedPreStressIntervocalToStressedVowel,

            int expectedUnstressedVowelToUnstressedCoda,
            int expectedStressedVowelToPostStressCoda)
        {
            WordGenerator wordGenerator = new WordGenerator();
            Word hello = Word.FromDictionaryLine(dictionaryLine);
            wordGenerator.LearnWord(hello);

            Assert.AreEqual(
                expectedPreStressOnsets,
                wordGenerator.StressedOnset.Count());

            Assert.AreEqual(
                expectedUnstressedOnsets,
                wordGenerator.UnstressedOnset.Count());

            Assert.AreEqual(
                expectedPreStressOnsetToStressedVowel,
                wordGenerator.OnsetToStressed.Count());

            Assert.AreEqual(
                expectedUnstressedOnsetToUnstressedVowel,
                wordGenerator.OnsetToUnstressed.Count());

            Assert.AreEqual(
                expectedStressedVowelToPostStressIntervocal,
                wordGenerator.StressedToFalling.Count());

            Assert.AreEqual(
                expectedUnstressedVowelToPreStressIntervocal,
                wordGenerator.UnstressedToRising.Count());

            Assert.AreEqual(
                expectedUnstressedVowelToUnstressedIntervocal,
                wordGenerator.UnstressedToFlat.Count());

            Assert.AreEqual(
                expectedPostStressIntervocalToUnstressedVowel,
                wordGenerator.FallingToUnstressed.Count());

            Assert.AreEqual(
                expectedUnstressedIntervocalToUnstressedVowel,
                wordGenerator.FlatToUnstressed.Count());

            Assert.AreEqual(
                expectedPreStressIntervocalToStressedVowel, 
                wordGenerator.RisingToStressed.Count());

            Assert.AreEqual(
                expectedUnstressedVowelToUnstressedCoda, 
                wordGenerator.UnstressedToCoda.Count());

            Assert.AreEqual(
                expectedStressedVowelToPostStressCoda,
                wordGenerator.StressedToCoda.Count());
        }
    }
}
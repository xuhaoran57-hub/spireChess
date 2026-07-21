using System;
using System.Linq;
using NUnit.Framework;
using SpireChess.Run;

namespace SpireChess.Tests.EditMode
{
    public sealed class RecordedRandomTests
    {
        [Test]
        public void RecordedRandom_PreservesSystemRandomGoldenOutput()
        {
            var random = new RecordedRandom(12345);

            Assert.That(random.Next(), Is.EqualTo(143337951));
            Assert.That(random.Next(1000), Is.EqualTo(70));
            Assert.That(random.Next(-50, 50), Is.EqualTo(27));
            Assert.That(
                BitConverter.DoubleToInt64Bits(random.NextDouble()),
                Is.EqualTo(4602779152785913473L));
            var bytes = new byte[8];
            random.NextBytes(bytes);
            Assert.That(Convert.ToBase64String(bytes), Is.EqualTo("rfJHPBRX/Ws="));
        }

        [Test]
        public void Restore_ReplaysHistoryAndContinuesAtSamePosition()
        {
            var original = new RecordedRandom(7719);
            for (var index = 0; index < 25; index++)
            {
                original.Next(index + 1, index + 100);
            }

            var restored = RecordedRandom.Restore(7719, original.Entries);

            Assert.That(restored.Entries.Count, Is.EqualTo(25));
            Assert.That(restored.Next(), Is.EqualTo(original.Next()));
            Assert.That(restored.NextDouble(), Is.EqualTo(original.NextDouble()));
        }

        [Test]
        public void Restore_RejectsTamperedResult()
        {
            var original = new RecordedRandom(991);
            original.Next(100);
            var entries = original.Entries.Select(value => value.Clone()).ToArray();
            entries[0].IntResult++;

            Assert.That(
                () => RecordedRandom.Restore(991, entries),
                Throws.TypeOf<RandomReplayException>());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace SpireChess.Run
{
    public enum RecordedRandomOperation
    {
        Next,
        NextMax,
        NextRange,
        NextDouble,
        NextBytes
    }

    public sealed class RecordedRandomEntry
    {
        public RecordedRandomOperation Operation { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int IntResult { get; set; }
        public double DoubleResult { get; set; }
        public byte[] BytesResult { get; set; }

        public RecordedRandomEntry Clone()
        {
            return new RecordedRandomEntry
            {
                Operation = Operation,
                Minimum = Minimum,
                Maximum = Maximum,
                IntResult = IntResult,
                DoubleResult = DoubleResult,
                BytesResult = BytesResult == null ? null : BytesResult.ToArray()
            };
        }
    }

    public sealed class RandomReplayException : InvalidOperationException
    {
        public RandomReplayException(string message)
            : base(message)
        {
        }
    }

    public sealed class RecordedRandom : Random
    {
        public const int MaximumRecordedCalls = 100000;

        private readonly Random inner;
        private readonly List<RecordedRandomEntry> entries =
            new List<RecordedRandomEntry>();

        public RecordedRandom(int seed)
        {
            Seed = seed;
            inner = new Random(seed);
        }

        public int Seed { get; }
        public IReadOnlyList<RecordedRandomEntry> Entries => entries;

        public override int Next()
        {
            var result = inner.Next();
            Record(new RecordedRandomEntry
            {
                Operation = RecordedRandomOperation.Next,
                IntResult = result
            });
            return result;
        }

        public override int Next(int maxValue)
        {
            var result = inner.Next(maxValue);
            Record(new RecordedRandomEntry
            {
                Operation = RecordedRandomOperation.NextMax,
                Maximum = maxValue,
                IntResult = result
            });
            return result;
        }

        public override int Next(int minValue, int maxValue)
        {
            var result = inner.Next(minValue, maxValue);
            Record(new RecordedRandomEntry
            {
                Operation = RecordedRandomOperation.NextRange,
                Minimum = minValue,
                Maximum = maxValue,
                IntResult = result
            });
            return result;
        }

        public override double NextDouble()
        {
            var result = inner.NextDouble();
            Record(new RecordedRandomEntry
            {
                Operation = RecordedRandomOperation.NextDouble,
                DoubleResult = result
            });
            return result;
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            inner.NextBytes(buffer);
            Record(new RecordedRandomEntry
            {
                Operation = RecordedRandomOperation.NextBytes,
                Maximum = buffer.Length,
                BytesResult = buffer.ToArray()
            });
        }

        public static RecordedRandom Restore(
            int seed,
            IEnumerable<RecordedRandomEntry> recordedEntries)
        {
            var restored = new RecordedRandom(seed);
            foreach (var expected in recordedEntries ??
                     Enumerable.Empty<RecordedRandomEntry>())
            {
                restored.Replay(expected);
            }

            return restored;
        }

        private void Replay(RecordedRandomEntry expected)
        {
            if (expected == null)
            {
                throw new RandomReplayException("Random replay entry is missing.");
            }

            RecordedRandomEntry actual;
            switch (expected.Operation)
            {
                case RecordedRandomOperation.Next:
                    actual = new RecordedRandomEntry
                    {
                        Operation = expected.Operation,
                        IntResult = inner.Next()
                    };
                    break;
                case RecordedRandomOperation.NextMax:
                    actual = new RecordedRandomEntry
                    {
                        Operation = expected.Operation,
                        Maximum = expected.Maximum,
                        IntResult = inner.Next(expected.Maximum)
                    };
                    break;
                case RecordedRandomOperation.NextRange:
                    actual = new RecordedRandomEntry
                    {
                        Operation = expected.Operation,
                        Minimum = expected.Minimum,
                        Maximum = expected.Maximum,
                        IntResult = inner.Next(expected.Minimum, expected.Maximum)
                    };
                    break;
                case RecordedRandomOperation.NextDouble:
                    actual = new RecordedRandomEntry
                    {
                        Operation = expected.Operation,
                        DoubleResult = inner.NextDouble()
                    };
                    break;
                case RecordedRandomOperation.NextBytes:
                    var bytes = new byte[expected.Maximum];
                    inner.NextBytes(bytes);
                    actual = new RecordedRandomEntry
                    {
                        Operation = expected.Operation,
                        Maximum = expected.Maximum,
                        BytesResult = bytes
                    };
                    break;
                default:
                    throw new RandomReplayException(
                        $"Unsupported random operation {expected.Operation}.");
            }

            if (!Matches(expected, actual))
            {
                throw new RandomReplayException(
                    $"Random replay mismatch at call {entries.Count} ({expected.Operation}).");
            }

            Record(expected.Clone());
        }

        private void Record(RecordedRandomEntry entry)
        {
            if (entries.Count >= MaximumRecordedCalls)
            {
                throw new InvalidOperationException(
                    $"Random stream exceeded {MaximumRecordedCalls} recorded calls.");
            }

            entries.Add(entry);
        }

        private static bool Matches(
            RecordedRandomEntry expected,
            RecordedRandomEntry actual)
        {
            return expected.Operation == actual.Operation &&
                   expected.Minimum == actual.Minimum &&
                   expected.Maximum == actual.Maximum &&
                   expected.IntResult == actual.IntResult &&
                   BitConverter.DoubleToInt64Bits(expected.DoubleResult) ==
                   BitConverter.DoubleToInt64Bits(actual.DoubleResult) &&
                   ((expected.BytesResult == null && actual.BytesResult == null) ||
                    (expected.BytesResult != null && actual.BytesResult != null &&
                     expected.BytesResult.SequenceEqual(actual.BytesResult)));
        }
    }
}

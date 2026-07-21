using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SpireChess.Battle;
using SpireChess.Config;

namespace SpireChess.Simulation
{
    public sealed class BattleSample
    {
        internal BattleSample(int seed, BattleSimulationResult result, Exception exception)
        {
            Seed = seed;
            ExceptionType = exception?.GetType().FullName;
            if (result == null)
            {
                return;
            }

            Winner = result.Winner;
            OutcomeReason = result.OutcomeReason;
            Diagnostics = result.Diagnostics;
            DeterminismHash = BattleDeterminismHasher.Compute(result);
            PlayerSurvivors = CountSurvivors(result.FinalState.Player);
            EnemySurvivors = CountSurvivors(result.FinalState.Enemy);
            PlayerSurvivorInstanceIds = SurvivorInstanceIds(result.FinalState.Player);
            EnemySurvivorInstanceIds = SurvivorInstanceIds(result.FinalState.Enemy);
            PermanentDeltasByInstanceJson = JsonConvert.SerializeObject(
                result.PermanentDeltas.Select(value => new
                {
                    value.SourceInstanceId,
                    value.Attack,
                    value.Health,
                    keywords = value.Keywords.OrderBy(keyword => keyword).ToArray()
                }));
        }

        public int Seed { get; }
        public BattleSide? Winner { get; }
        public BattleOutcomeReason? OutcomeReason { get; }
        public BattleDiagnostics Diagnostics { get; }
        public string DeterminismHash { get; }
        public string ExceptionType { get; }
        public string PermanentDeltasByInstanceJson { get; }
        public int PlayerSurvivors { get; }
        public int EnemySurvivors { get; }
        public IReadOnlyList<string> PlayerSurvivorInstanceIds { get; }
            = Array.Empty<string>();
        public IReadOnlyList<string> EnemySurvivorInstanceIds { get; }
            = Array.Empty<string>();
        public bool Succeeded => string.IsNullOrWhiteSpace(ExceptionType);

        private static int CountSurvivors(IEnumerable<BattleMinionRuntime> row)
        {
            return row.Count(value => value != null && value.IsAlive);
        }

        private static IReadOnlyList<string> SurvivorInstanceIds(
            IReadOnlyList<BattleMinionRuntime> row)
        {
            return row.Select((value, index) => new { value, index })
                .Where(entry => entry.value != null && entry.value.IsAlive)
                .Select(entry => string.IsNullOrWhiteSpace(entry.value.SourceInstanceId)
                    ? $"{entry.value.Id}@{entry.index}"
                    : entry.value.SourceInstanceId)
                .ToList().AsReadOnly();
        }
    }

    public sealed class BattleBatchResult
    {
        private readonly List<BattleSample> samples = new List<BattleSample>();

        public int Battles { get; internal set; }
        public int PlayerWins { get; internal set; }
        public int EnemyWins { get; internal set; }
        public int Draws { get; internal set; }
        public int Exceptions { get; internal set; }
        public double PlayerWinRate => Battles <= 0 ? 0d : (double)PlayerWins / Battles;
        public double PlayerScoreRate => Battles <= 0
            ? 0d
            : (PlayerWins + Draws * 0.5d) / Battles;
        public IReadOnlyList<BattleSample> Samples => samples;

        internal void Add(BattleSample sample)
        {
            samples.Add(sample);
        }
    }

    public sealed class BattleBatchRunner
    {
        private readonly Func<string, MinionConfig> resolveMinion;

        public BattleBatchRunner(Func<string, MinionConfig> resolveMinion)
        {
            this.resolveMinion = resolveMinion;
        }

        public BattleBatchResult Run(
            BattleBoardState fixture,
            int firstSeed,
            int battleCount)
        {
            if (battleCount < 1) throw new ArgumentOutOfRangeException(nameof(battleCount));
            return Run(fixture, Enumerable.Range(firstSeed, battleCount));
        }

        public BattleBatchResult Run(
            BattleBoardState fixture,
            IEnumerable<int> seeds)
        {
            if (fixture == null) throw new ArgumentNullException(nameof(fixture));
            if (seeds == null) throw new ArgumentNullException(nameof(seeds));

            var materializedSeeds = seeds.ToList();
            if (materializedSeeds.Count == 0)
            {
                throw new ArgumentException("At least one seed is required.", nameof(seeds));
            }
            if (materializedSeeds.Distinct().Count() != materializedSeeds.Count)
            {
                throw new ArgumentException("Battle seeds must be unique.", nameof(seeds));
            }

            var result = new BattleBatchResult { Battles = materializedSeeds.Count };
            foreach (var seed in materializedSeeds)
            {
                BattleSimulationResult battle = null;
                Exception exception = null;
                try
                {
                    var simulator = new BattleSimulator(new Random(seed), resolveMinion);
                    battle = simulator.Simulate(fixture);
                }
                catch (Exception caught)
                {
                    exception = caught;
                }

                var sample = new BattleSample(seed, battle, exception);
                result.Add(sample);
                if (!sample.Succeeded)
                {
                    result.Exceptions++;
                }
                else if (sample.Winner == BattleSide.Player)
                {
                    result.PlayerWins++;
                }
                else if (sample.Winner == BattleSide.Enemy)
                {
                    result.EnemyWins++;
                }
                else
                {
                    result.Draws++;
                }
            }

            return result;
        }
    }

    public static class BattleBatchComparer
    {
        public static int CountDeterminismFailures(
            BattleBatchResult first,
            BattleBatchResult second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var secondBySeed = second.Samples.ToDictionary(value => value.Seed);
            var failures = 0;
            foreach (var sample in first.Samples)
            {
                if (!secondBySeed.TryGetValue(sample.Seed, out var other) ||
                    sample.DeterminismHash != other.DeterminismHash ||
                    sample.ExceptionType != other.ExceptionType)
                {
                    failures++;
                }
            }

            return failures + second.Samples.Count(value =>
                first.Samples.All(firstSample => firstSample.Seed != value.Seed));
        }
    }

    public static class BattleDeterminismHasher
    {
        public static string Compute(BattleSimulationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            var builder = new StringBuilder();
            builder.Append(result.Winner?.ToString() ?? "Draw").Append('|')
                .Append(result.OutcomeReason).Append('|');
            AppendRow(builder, result.FinalState.Player, "P");
            AppendRow(builder, result.FinalState.Enemy, "E");
            builder.Append("F:")
                .Append(result.FinalState.PlayerFlourishStacks).Append(':')
                .Append(result.FinalState.EnemyFlourishStacks).Append('|');
            var rules = result.FinalState.RuleModifiers;
            builder.Append("M:")
                .Append(rules.PlayerExtraDeathrattleTriggers).Append(':')
                .Append(rules.PlayerFirstNonTokenDeathSummonCount).Append(':')
                .Append(rules.PlayerFirstNonTokenDeathTokenId ?? string.Empty).Append(':')
                .Append(rules.PlayerFirstNonTokenDeathTokenAttack).Append(':')
                .Append(rules.PlayerFirstNonTokenDeathTokenHealth).Append(':')
                .Append(rules.PlayerBattleStartShieldTargets).Append(':')
                .Append(rules.PlayerDistinctRaceStatBonus).Append('|');
            foreach (var delta in result.PermanentDeltas
                         .OrderBy(value => value.SourceInstanceId))
            {
                builder.Append("D:").Append(delta.SourceInstanceId).Append(':')
                    .Append(delta.Attack).Append(':').Append(delta.Health).Append(':')
                    .Append(string.Join(",", delta.Keywords.OrderBy(value => value)))
                    .Append('|');
            }
            foreach (var reward in result.PostCombatRewardRequests
                         .OrderBy(value => value.Side)
                         .ThenBy(value => value.Race)
                         .ThenBy(value => value.Count))
            {
                builder.Append("W:").Append(reward.Side).Append(':')
                    .Append(reward.Race).Append(':').Append(reward.Count).Append('|');
            }

            AppendDiagnostics(builder, result.Diagnostics.Player, "P");
            AppendDiagnostics(builder, result.Diagnostics.Enemy, "E");
            builder.Append("R:").Append(result.Diagnostics.RoundCount).Append(':')
                .Append(result.Diagnostics.ProcessedEffectCount).Append(':')
                .Append(result.Diagnostics.HitEffectLimit ? 1 : 0);

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void AppendRow(
            StringBuilder builder,
            IReadOnlyList<BattleMinionRuntime> row,
            string prefix)
        {
            for (var i = 0; i < row.Count; i++)
            {
                var minion = row[i];
                builder.Append(prefix).Append(i).Append(':');
                if (minion == null)
                {
                    builder.Append("null|");
                    continue;
                }

                builder.Append(minion.Id).Append(':')
                    .Append(minion.SourceInstanceId ?? string.Empty).Append(':')
                    .Append(minion.IsGolden ? 1 : 0).Append(':')
                    .Append(minion.CurrentAttack).Append(':')
                    .Append(minion.CurrentHealth).Append(':')
                    .Append(minion.PermanentAttackBonus).Append(':')
                    .Append(minion.PermanentHealthBonus).Append(':')
                    .Append(minion.HasShield ? 1 : 0).Append(':')
                    .Append(string.Join(",", minion.Keywords.OrderBy(value => value)))
                    .Append('|');
            }
        }

        private static void AppendDiagnostics(
            StringBuilder builder,
            BattleSideDiagnostics side,
            string prefix)
        {
            builder.Append(prefix).Append("X:")
                .Append(side.OpeningRawDamage).Append(':')
                .Append(side.OpeningEffectiveDamage).Append(':')
                .Append(side.RoundOneRawDamage).Append(':')
                .Append(side.RoundOneEffectiveDamage).Append(':')
                .Append(side.NormalAttacks).Append(':')
                .Append(side.ImmediateAttacks).Append(':')
                .Append(side.CleaveHits).Append(':')
                .Append(side.SummonAttempts).Append(':')
                .Append(side.SummonSuccesses).Append(':')
                .Append(side.SummonFailures).Append(':')
                .Append(side.TokenDeaths).Append(':')
                .Append(side.NonTokenDeaths).Append(':')
                .Append(side.ShieldsGranted).Append(':')
                .Append(side.ShieldsLost).Append(':')
                .Append(side.ShieldDamageBlocks).Append(':')
                .Append(side.FurnaceTransfers).Append(':')
                .Append(side.ShieldBenefitTriggers).Append(':')
                .Append(side.NonTokenDeathBenefitTriggers).Append(':')
                .Append(side.TemporaryAttackGained).Append(':')
                .Append(side.TemporaryHealthGained).Append(':')
                .Append(side.PermanentAttackDelta).Append(':')
                .Append(side.PermanentHealthDelta).Append(':')
                .Append(side.FlourishGained).Append('|');
        }
    }

    public static class BuildFixture
    {
        public static BattleBoardState Create(
            IEnumerable<BattleMinionRuntime> player,
            IEnumerable<BattleMinionRuntime> enemy)
        {
            var state = new BattleBoardState();
            Fill(state.Player, player);
            Fill(state.Enemy, enemy);
            return state;
        }

        private static void Fill(
            IList<BattleMinionRuntime> row,
            IEnumerable<BattleMinionRuntime> values)
        {
            var index = 0;
            foreach (var value in values ?? Array.Empty<BattleMinionRuntime>())
            {
                if (index >= row.Count) break;
                row[index++] = value;
            }
        }
    }
}

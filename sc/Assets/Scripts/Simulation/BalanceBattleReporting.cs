using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using SpireChess.Battle;
using SpireChess.Utils;

namespace SpireChess.Simulation
{
    public sealed class BalanceBatchMetadata
    {
        public string BalanceSchemaVersion { get; set; } = "0.2.0";
        public string CandidateId { get; set; }
        public string ContentVersion { get; set; }
        public string ConfigHash { get; set; }
        public string GitCommit { get; set; }
        public string UnityVersion { get; set; }
        public string TuningRound { get; set; }
        public string BatchId { get; set; }
        public string FixtureVersion { get; set; } = "0.2.0";
        public string CoreClassifierVersion { get; set; } = "0.2.2";
        public string SeedSet { get; set; }
        public string PlayerFixtureId { get; set; }
        public string EnemyFixtureId { get; set; }
        public string DevelopmentLevel { get; set; }
        public string Orientation { get; set; }
    }

    public sealed class BalanceMirrorSummary
    {
        public string BuildA { get; private set; }
        public string BuildB { get; private set; }
        public string DevelopmentLevel { get; private set; }
        public int Battles { get; private set; }
        public int AWins { get; private set; }
        public int BWins { get; private set; }
        public int Draws { get; private set; }
        public int RoundLimits { get; private set; }
        public int MutualEliminations { get; private set; }
        public int EffectLimitHits { get; private set; }
        public int Exceptions { get; private set; }
        public double AScoreRate => Battles <= 0 ? 0d : (AWins + Draws * 0.5d) / Battles;
        public double ADecisiveWinWilsonLow { get; private set; }
        public double ADecisiveWinWilsonHigh { get; private set; }
        public double RoundLimitRate => Battles <= 0 ? 0d : (double)RoundLimits / Battles;
        public double MutualEliminationRate => Battles <= 0
            ? 0d
            : (double)MutualEliminations / Battles;
        public double MeanRounds { get; private set; }
        public double MedianRounds { get; private set; }
        public double P90Rounds { get; private set; }
        public double MeanRoundOneRawDamageA { get; private set; }
        public double MeanRoundOneRawDamageB { get; private set; }
        public double MeanSurvivorsA { get; private set; }
        public double MeanSurvivorsB { get; private set; }
        public double MeanSummonFailuresA { get; private set; }
        public double MeanSummonFailuresB { get; private set; }
        public double MeanImmediateAttacksA { get; private set; }
        public double MeanImmediateAttacksB { get; private set; }
        public double MeanShieldBlocksA { get; private set; }
        public double MeanShieldBlocksB { get; private set; }
        public double MeanFurnaceTransfersA { get; private set; }
        public double MeanFurnaceTransfersB { get; private set; }
        public double MeanPermanentDeltaA { get; private set; }
        public double MeanPermanentDeltaB { get; private set; }
        public double MeanFlourishGainedA { get; private set; }
        public double MeanFlourishGainedB { get; private set; }

        public static BalanceMirrorSummary Create(
            string buildA,
            string buildB,
            string developmentLevel,
            BattleBatchResult aAsPlayer,
            BattleBatchResult bAsPlayer)
        {
            if (aAsPlayer == null) throw new ArgumentNullException(nameof(aAsPlayer));
            if (bAsPlayer == null) throw new ArgumentNullException(nameof(bAsPlayer));

            var successful = aAsPlayer.Samples.Concat(bAsPlayer.Samples)
                .Where(value => value.Succeeded).ToList();
            var aDiagnostics = aAsPlayer.Samples.Where(value => value.Succeeded)
                .Select(value => value.Diagnostics.Player)
                .Concat(bAsPlayer.Samples.Where(value => value.Succeeded)
                    .Select(value => value.Diagnostics.Enemy)).ToList();
            var bDiagnostics = aAsPlayer.Samples.Where(value => value.Succeeded)
                .Select(value => value.Diagnostics.Enemy)
                .Concat(bAsPlayer.Samples.Where(value => value.Succeeded)
                    .Select(value => value.Diagnostics.Player)).ToList();
            var roundCounts = successful.Select(value =>
                value.Diagnostics?.RoundCount ?? 0).OrderBy(value => value).ToList();
            var aSurvivors = aAsPlayer.Samples.Where(value => value.Succeeded)
                .Select(value => value.PlayerSurvivors)
                .Concat(bAsPlayer.Samples.Where(value => value.Succeeded)
                    .Select(value => value.EnemySurvivors));
            var bSurvivors = aAsPlayer.Samples.Where(value => value.Succeeded)
                .Select(value => value.EnemySurvivors)
                .Concat(bAsPlayer.Samples.Where(value => value.Succeeded)
                    .Select(value => value.PlayerSurvivors));
            var result = new BalanceMirrorSummary
            {
                BuildA = buildA,
                BuildB = buildB,
                DevelopmentLevel = developmentLevel,
                Battles = aAsPlayer.Battles + bAsPlayer.Battles,
                AWins = aAsPlayer.PlayerWins + bAsPlayer.EnemyWins,
                BWins = aAsPlayer.EnemyWins + bAsPlayer.PlayerWins,
                Draws = aAsPlayer.Draws + bAsPlayer.Draws,
                RoundLimits = successful.Count(value =>
                    value.OutcomeReason == BattleOutcomeReason.RoundLimit),
                MutualEliminations = successful.Count(value =>
                    value.OutcomeReason == BattleOutcomeReason.MutualElimination),
                EffectLimitHits = successful.Count(value =>
                    value.Diagnostics?.HitEffectLimit == true),
                Exceptions = aAsPlayer.Exceptions + bAsPlayer.Exceptions,
                MeanRounds = Average(roundCounts),
                MedianRounds = Percentile(roundCounts, 0.5d),
                P90Rounds = Percentile(roundCounts, 0.9d),
                MeanRoundOneRawDamageA = Average(aDiagnostics.Select(value => value.RoundOneRawDamage)),
                MeanRoundOneRawDamageB = Average(bDiagnostics.Select(value => value.RoundOneRawDamage)),
                MeanSurvivorsA = Average(aSurvivors),
                MeanSurvivorsB = Average(bSurvivors),
                MeanSummonFailuresA = Average(aDiagnostics.Select(value => value.SummonFailures)),
                MeanSummonFailuresB = Average(bDiagnostics.Select(value => value.SummonFailures)),
                MeanImmediateAttacksA = Average(aDiagnostics.Select(value => value.ImmediateAttacks)),
                MeanImmediateAttacksB = Average(bDiagnostics.Select(value => value.ImmediateAttacks)),
                MeanShieldBlocksA = Average(aDiagnostics.Select(value => value.ShieldDamageBlocks)),
                MeanShieldBlocksB = Average(bDiagnostics.Select(value => value.ShieldDamageBlocks)),
                MeanFurnaceTransfersA = Average(aDiagnostics.Select(value => value.FurnaceTransfers)),
                MeanFurnaceTransfersB = Average(bDiagnostics.Select(value => value.FurnaceTransfers)),
                MeanPermanentDeltaA = Average(aDiagnostics.Select(value =>
                    value.PermanentAttackDelta + value.PermanentHealthDelta)),
                MeanPermanentDeltaB = Average(bDiagnostics.Select(value =>
                    value.PermanentAttackDelta + value.PermanentHealthDelta)),
                MeanFlourishGainedA = Average(aDiagnostics.Select(value => value.FlourishGained)),
                MeanFlourishGainedB = Average(bDiagnostics.Select(value => value.FlourishGained))
            };
            var wilson = Wilson(result.AWins, result.AWins + result.BWins);
            result.ADecisiveWinWilsonLow = wilson.Item1;
            result.ADecisiveWinWilsonHigh = wilson.Item2;
            return result;
        }

        private static double Average(IEnumerable<int> values)
        {
            var materialized = values.ToList();
            return materialized.Count == 0 ? 0d : materialized.Average();
        }

        private static double Percentile(IReadOnlyList<int> sorted, double percentile)
        {
            if (sorted.Count == 0) return 0d;
            var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))];
        }

        private static Tuple<double, double> Wilson(int successes, int total)
        {
            if (total <= 0) return Tuple.Create(0d, 0d);
            const double z = 1.959963984540054d;
            var p = (double)successes / total;
            var denominator = 1d + z * z / total;
            var center = (p + z * z / (2d * total)) / denominator;
            var margin = z * Math.Sqrt(
                p * (1d - p) / total + z * z / (4d * total * total)) / denominator;
            return Tuple.Create(Math.Max(0d, center - margin), Math.Min(1d, center + margin));
        }
    }

    public static class BalanceBattleCsv
    {
        public static string SerializeSamples(
            BalanceBatchMetadata metadata,
            BattleBatchResult result)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var builder = new StringBuilder();
            builder.AppendLine("balanceSchemaVersion,candidateId,contentVersion,configHash,gitCommit,unityVersion,tuningRound,batchId,fixtureVersion,coreClassifierVersion,seedSet,seed,playerFixtureId,enemyFixtureId,developmentLevel,orientation,winner,outcomeReason,roundCount,playerSurvivors,enemySurvivors,playerSurvivorInstanceIds,enemySurvivorInstanceIds,playerOpeningRawDamage,enemyOpeningRawDamage,playerOpeningEffectiveDamage,enemyOpeningEffectiveDamage,playerRound1RawDamage,enemyRound1RawDamage,playerRound1EffectiveDamage,enemyRound1EffectiveDamage,playerNormalAttacks,enemyNormalAttacks,playerImmediateAttacks,enemyImmediateAttacks,playerCleaveHits,enemyCleaveHits,playerSummonAttempts,enemySummonAttempts,playerSummonSuccesses,enemySummonSuccesses,playerSummonFailures,enemySummonFailures,playerTokenDeaths,enemyTokenDeaths,playerNonTokenDeaths,enemyNonTokenDeaths,playerShieldsGranted,enemyShieldsGranted,playerShieldsLost,enemyShieldsLost,playerShieldDamageBlocks,enemyShieldDamageBlocks,playerFurnaceTransfers,enemyFurnaceTransfers,playerShieldBenefitTriggers,enemyShieldBenefitTriggers,playerNonTokenDeathBenefitTriggers,enemyNonTokenDeathBenefitTriggers,playerTemporaryAttackGained,enemyTemporaryAttackGained,playerTemporaryHealthGained,enemyTemporaryHealthGained,playerPermanentAttackDelta,enemyPermanentAttackDelta,playerPermanentHealthDelta,enemyPermanentHealthDelta,playerFlourishGained,enemyFlourishGained,permanentDeltasByInstanceJson,processedEffectCount,hitEffectLimit,exceptionType,determinismHash");
            foreach (var sample in result.Samples)
            {
                var diagnostics = sample.Diagnostics ?? new BattleDiagnostics();
                var player = diagnostics.Player;
                var enemy = diagnostics.Enemy;
                AppendRow(builder, new object[]
                {
                    metadata.BalanceSchemaVersion, metadata.CandidateId,
                    metadata.ContentVersion, metadata.ConfigHash, metadata.GitCommit,
                    metadata.UnityVersion, metadata.TuningRound, metadata.BatchId,
                    metadata.FixtureVersion, metadata.CoreClassifierVersion,
                    metadata.SeedSet, sample.Seed, metadata.PlayerFixtureId,
                    metadata.EnemyFixtureId, metadata.DevelopmentLevel,
                    metadata.Orientation, sample.Winner?.ToString() ?? "Draw",
                    sample.OutcomeReason?.ToString(), diagnostics.RoundCount,
                    sample.PlayerSurvivors, sample.EnemySurvivors,
                    string.Join(";", sample.PlayerSurvivorInstanceIds),
                    string.Join(";", sample.EnemySurvivorInstanceIds),
                    player.OpeningRawDamage, enemy.OpeningRawDamage,
                    player.OpeningEffectiveDamage, enemy.OpeningEffectiveDamage,
                    player.RoundOneRawDamage, enemy.RoundOneRawDamage,
                    player.RoundOneEffectiveDamage, enemy.RoundOneEffectiveDamage,
                    player.NormalAttacks, enemy.NormalAttacks,
                    player.ImmediateAttacks, enemy.ImmediateAttacks,
                    player.CleaveHits, enemy.CleaveHits,
                    player.SummonAttempts, enemy.SummonAttempts,
                    player.SummonSuccesses, enemy.SummonSuccesses,
                    player.SummonFailures, enemy.SummonFailures,
                    player.TokenDeaths, enemy.TokenDeaths,
                    player.NonTokenDeaths, enemy.NonTokenDeaths,
                    player.ShieldsGranted, enemy.ShieldsGranted,
                    player.ShieldsLost, enemy.ShieldsLost,
                    player.ShieldDamageBlocks, enemy.ShieldDamageBlocks,
                    player.FurnaceTransfers, enemy.FurnaceTransfers,
                    player.ShieldBenefitTriggers, enemy.ShieldBenefitTriggers,
                    player.NonTokenDeathBenefitTriggers,
                    enemy.NonTokenDeathBenefitTriggers,
                    player.TemporaryAttackGained, enemy.TemporaryAttackGained,
                    player.TemporaryHealthGained, enemy.TemporaryHealthGained,
                    player.PermanentAttackDelta, enemy.PermanentAttackDelta,
                    player.PermanentHealthDelta, enemy.PermanentHealthDelta,
                    player.FlourishGained, enemy.FlourishGained,
                    sample.PermanentDeltasByInstanceJson,
                    diagnostics.ProcessedEffectCount, diagnostics.HitEffectLimit,
                    sample.ExceptionType, sample.DeterminismHash
                });
            }

            return builder.ToString();
        }

        public static string SerializeMirrorSummaries(
            IEnumerable<BalanceMirrorSummary> summaries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("buildA,buildB,developmentLevel,battles,aWins,bWins,draws,aScoreRate,aDecisiveWinWilsonLow,aDecisiveWinWilsonHigh,roundLimitRate,mutualEliminationRate,medianRounds,p90Rounds,meanRound1RawDamageA,meanRound1RawDamageB,meanSurvivorsA,meanSurvivorsB,meanSummonFailuresA,meanSummonFailuresB,meanImmediateAttacksA,meanImmediateAttacksB,meanShieldBlocksA,meanShieldBlocksB,meanFurnaceTransfersA,meanFurnaceTransfersB,meanPermanentDeltaA,meanPermanentDeltaB,meanFlourishGainedA,meanFlourishGainedB,effectLimitHits,exceptions");
            foreach (var row in summaries ?? Enumerable.Empty<BalanceMirrorSummary>())
            {
                AppendRow(builder, new object[]
                {
                    row.BuildA, row.BuildB, row.DevelopmentLevel, row.Battles,
                    row.AWins, row.BWins, row.Draws, Format(row.AScoreRate),
                    Format(row.ADecisiveWinWilsonLow), Format(row.ADecisiveWinWilsonHigh),
                    Format(row.RoundLimitRate), Format(row.MutualEliminationRate),
                    Format(row.MedianRounds), Format(row.P90Rounds),
                    Format(row.MeanRoundOneRawDamageA), Format(row.MeanRoundOneRawDamageB),
                    Format(row.MeanSurvivorsA), Format(row.MeanSurvivorsB),
                    Format(row.MeanSummonFailuresA), Format(row.MeanSummonFailuresB),
                    Format(row.MeanImmediateAttacksA), Format(row.MeanImmediateAttacksB),
                    Format(row.MeanShieldBlocksA), Format(row.MeanShieldBlocksB),
                    Format(row.MeanFurnaceTransfersA), Format(row.MeanFurnaceTransfersB),
                    Format(row.MeanPermanentDeltaA), Format(row.MeanPermanentDeltaB),
                    Format(row.MeanFlourishGainedA), Format(row.MeanFlourishGainedB),
                    row.EffectLimitHits, row.Exceptions
                });
            }
            return builder.ToString();
        }

        private static string Format(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static void AppendRow(StringBuilder builder, IEnumerable<object> values)
        {
            builder.AppendLine(string.Join(",", values.Select(Escape)));
        }

        private static string Escape(object value)
        {
            if (value == null) return string.Empty;
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? text
                : $"\"{text.Replace("\"", "\"\"")}\"";
        }
    }

    public static class BalanceConfigHasher
    {
        public static string Compute(params string[] jsonDocuments)
        {
            return CanonicalJson.ComputeSha256(jsonDocuments);
        }
    }
}

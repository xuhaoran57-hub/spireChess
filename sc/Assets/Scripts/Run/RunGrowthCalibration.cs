using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SpireChess.Run
{
    public sealed class BalanceGrowthCalibrationRow
    {
        public string BalanceSchemaVersion { get; set; } = "0.3.0";
        public string TuningRound { get; set; }
        public string CandidateId { get; set; }
        public string CoreClassifierVersion { get; set; } = CoreBuildClassifier.Version;
        public string BuildId { get; set; }
        public int IntendedRuns { get; set; }
        public int IntendedRunsReachingTurnTen { get; set; }
        public int IntendedRunsFormedByTurnTen { get; set; }
        public int ObservedBuildSamples { get; set; }
        public double TurnTenReachRate { get; set; }
        public double FormationRate { get; set; }
        public int NormalAttackP50 { get; set; }
        public int NormalHealthP50 { get; set; }
        public int HighAttackP90 { get; set; }
        public int HighHealthP90 { get; set; }
        public int? FirstCoreTurnP50 { get; set; }
        public int? SecondCoreTurnP50 { get; set; }
        public int RefreshesPaidP50 { get; set; }
        public int RefreshesFreeP50 { get; set; }
        public int MinionsBoughtP50 { get; set; }
        public int SpellsUsedP50 { get; set; }
        public int TavernUpgradesP50 { get; set; }
        public int TriplesFormedP50 { get; set; }
        public int NormalRepresentativeSeed { get; set; }
        public string NormalRepresentativeBoardJson { get; set; }
        public int HighRepresentativeSeed { get; set; }
        public string HighRepresentativeBoardJson { get; set; }
        public string CalibrationStatus { get; set; }
    }

    public sealed class RunGrowthCalibrationAggregator
    {
        public const int MinimumReadySamples = 10;

        public IReadOnlyList<BalanceGrowthCalibrationRow> Aggregate(
            IEnumerable<BalanceRunSummary> summaries,
            string candidateId,
            string tuningRound)
        {
            var materialized = (summaries ?? Enumerable.Empty<BalanceRunSummary>())
                .ToList();
            var rows = new List<BalanceGrowthCalibrationRow>();
            foreach (var buildId in CoreBuildClassifier.BuildIds)
            {
                var intended = materialized.Where(value =>
                    value.IntendedBuildId == buildId).ToList();
                var observed = materialized.Where(value =>
                    value.TurnTenReached && value.TurnTenBuildId == buildId).ToList();
                var formedAsIntended = intended.Count(value =>
                    value.TurnTenReached && value.TurnTenBuildId == buildId);
                var normalAttack = Percentile(observed.Select(value =>
                    value.TurnTenPermanentAttack), 0.5d);
                var normalHealth = Percentile(observed.Select(value =>
                    value.TurnTenPermanentHealth), 0.5d);
                var highAttack = Percentile(observed.Select(value =>
                    value.TurnTenPermanentAttack), 0.9d);
                var highHealth = Percentile(observed.Select(value =>
                    value.TurnTenPermanentHealth), 0.9d);
                var normalRepresentative = SelectRepresentative(
                    observed,
                    normalAttack,
                    normalHealth);
                var highRepresentative = SelectRepresentative(
                    observed,
                    highAttack,
                    highHealth);

                rows.Add(new BalanceGrowthCalibrationRow
                {
                    CandidateId = candidateId,
                    TuningRound = tuningRound,
                    BuildId = buildId,
                    IntendedRuns = intended.Count,
                    IntendedRunsReachingTurnTen = intended.Count(value => value.TurnTenReached),
                    IntendedRunsFormedByTurnTen = formedAsIntended,
                    ObservedBuildSamples = observed.Count,
                    TurnTenReachRate = intended.Count == 0
                        ? 0d
                        : (double)intended.Count(value => value.TurnTenReached) / intended.Count,
                    FormationRate = intended.Count == 0
                        ? 0d
                        : (double)formedAsIntended / intended.Count,
                    NormalAttackP50 = normalAttack,
                    NormalHealthP50 = normalHealth,
                    HighAttackP90 = highAttack,
                    HighHealthP90 = highHealth,
                    FirstCoreTurnP50 = NullablePercentile(
                        observed.Select(value => value.FirstCoreTurn), 0.5d),
                    SecondCoreTurnP50 = NullablePercentile(
                        observed.Select(value => value.SecondCoreTurn), 0.5d),
                    RefreshesPaidP50 = Percentile(
                        observed.Select(value => value.TurnTenRefreshesPaid), 0.5d),
                    RefreshesFreeP50 = Percentile(
                        observed.Select(value => value.TurnTenRefreshesFree), 0.5d),
                    MinionsBoughtP50 = Percentile(
                        observed.Select(value => value.TurnTenMinionsBought), 0.5d),
                    SpellsUsedP50 = Percentile(
                        observed.Select(value => value.TurnTenSpellsUsed), 0.5d),
                    TavernUpgradesP50 = Percentile(
                        observed.Select(value => value.TurnTenTavernUpgrades), 0.5d),
                    TriplesFormedP50 = Percentile(
                        observed.Select(value => value.TurnTenTriplesFormed), 0.5d),
                    NormalRepresentativeSeed = normalRepresentative?.Seed ?? 0,
                    NormalRepresentativeBoardJson = normalRepresentative?.TurnTenBoardJson,
                    HighRepresentativeSeed = highRepresentative?.Seed ?? 0,
                    HighRepresentativeBoardJson = highRepresentative?.TurnTenBoardJson,
                    CalibrationStatus = ResolveStatus(observed.Count)
                });
            }

            return rows.AsReadOnly();
        }

        private static int Percentile(IEnumerable<int> values, double percentile)
        {
            var ordered = (values ?? Enumerable.Empty<int>()).OrderBy(value => value).ToList();
            if (ordered.Count == 0)
            {
                return 0;
            }

            var index = Math.Max(0, Math.Min(
                ordered.Count - 1,
                (int)Math.Ceiling(percentile * ordered.Count) - 1));
            return ordered[index];
        }

        private static int? NullablePercentile(
            IEnumerable<int?> values,
            double percentile)
        {
            var materialized = (values ?? Enumerable.Empty<int?>())
                .Where(value => value.HasValue)
                .Select(value => value.Value)
                .ToList();
            return materialized.Count == 0
                ? (int?)null
                : Percentile(materialized, percentile);
        }

        private static BalanceRunSummary SelectRepresentative(
            IEnumerable<BalanceRunSummary> samples,
            int targetAttack,
            int targetHealth)
        {
            return (samples ?? Enumerable.Empty<BalanceRunSummary>())
                .OrderBy(value =>
                    Math.Abs(value.TurnTenPermanentAttack - targetAttack) +
                    Math.Abs(value.TurnTenPermanentHealth - targetHealth))
                .ThenBy(value => value.Seed)
                .FirstOrDefault();
        }

        private static string ResolveStatus(int sampleCount)
        {
            if (sampleCount == 0) return "NoFormedSamples";
            if (sampleCount < 3) return "Insufficient";
            if (sampleCount < MinimumReadySamples) return "Provisional";
            return "Ready";
        }
    }

    public static class BalanceGrowthCalibrationCsv
    {
        public static string Serialize(IEnumerable<BalanceGrowthCalibrationRow> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                "balanceSchemaVersion,tuningRound,candidateId,coreClassifierVersion," +
                "buildId,intendedRuns,intendedRunsReachingTurnTen," +
                "intendedRunsFormedByTurnTen,observedBuildSamples,turnTenReachRate," +
                "formationRate,normalAttackP50,normalHealthP50,highAttackP90," +
                "highHealthP90,firstCoreTurnP50,secondCoreTurnP50,refreshesPaidP50," +
                "refreshesFreeP50,minionsBoughtP50,spellsUsedP50,tavernUpgradesP50," +
                "triplesFormedP50,normalRepresentativeSeed,normalRepresentativeBoardJson," +
                "highRepresentativeSeed,highRepresentativeBoardJson,calibrationStatus");
            foreach (var row in rows ?? Enumerable.Empty<BalanceGrowthCalibrationRow>())
            {
                builder.AppendLine(string.Join(",", new object[]
                {
                    row.BalanceSchemaVersion, row.TuningRound, row.CandidateId,
                    row.CoreClassifierVersion, row.BuildId, row.IntendedRuns,
                    row.IntendedRunsReachingTurnTen, row.IntendedRunsFormedByTurnTen,
                    row.ObservedBuildSamples,
                    row.TurnTenReachRate.ToString("0.######", CultureInfo.InvariantCulture),
                    row.FormationRate.ToString("0.######", CultureInfo.InvariantCulture),
                    row.NormalAttackP50, row.NormalHealthP50, row.HighAttackP90,
                    row.HighHealthP90, row.FirstCoreTurnP50, row.SecondCoreTurnP50,
                    row.RefreshesPaidP50, row.RefreshesFreeP50, row.MinionsBoughtP50,
                    row.SpellsUsedP50, row.TavernUpgradesP50, row.TriplesFormedP50,
                    row.NormalRepresentativeSeed, row.NormalRepresentativeBoardJson,
                    row.HighRepresentativeSeed, row.HighRepresentativeBoardJson,
                    row.CalibrationStatus
                }.Select(Escape)));
            }
            return builder.ToString();
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
}

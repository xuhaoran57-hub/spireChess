using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Simulation;
using SpireChess.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SpireChess.Editor
{
    public static class RunGrowthCalibrationCommand
    {
        public static void RunFromCommandLine()
        {
            try
            {
                Run(Environment.GetCommandLineArgs());
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }
                throw;
            }
        }

        private static void Run(IReadOnlyList<string> arguments)
        {
            var input = ReadRequiredArgument(arguments, "-runTelemetryInput");
            var candidateId = ReadArgument(arguments, "-balanceCandidate") ??
                              "fixture-growth-calibration";
            var tuningRound = ReadArgument(arguments, "-balanceTuningRound") ?? "R3";
            var decisionInput = ReadArgument(arguments, "-runDecisionInput");
            var output = ResolveOutputDirectory(
                ReadArgument(arguments, "-balanceOutput"),
                candidateId);
            var telemetryFiles = Directory.GetFiles(
                    ResolveRepositoryPath(input),
                    "*.ndjson",
                    SearchOption.AllDirectories)
                .OrderBy(value => value)
                .ToList();
            if (telemetryFiles.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No NDJSON telemetry files found under {input}.");
            }

            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();
            var decisions = ReadDecisionSummaries(string.IsNullOrWhiteSpace(decisionInput)
                ? null
                : ResolveRepositoryPath(decisionInput));
            var aggregator = new RunTelemetryAggregator(id =>
                configs.MinionsById.TryGetValue(id, out var minion)
                    ? minion.Tier
                    : configs.SpellsById.TryGetValue(id, out var spell) ? spell.Tier : 0);
            var documents = telemetryFiles.ToDictionary(
                path => path,
                File.ReadAllText);
            var summaries = new List<BalanceRunSummary>();
            foreach (var pair in documents)
            {
                var preliminary = aggregator.AggregateRun(
                    pair.Value,
                    candidateId,
                    tuningRound,
                    pair.Key);
                decisions.TryGetValue(preliminary.Seed, out var decision);
                summaries.Add(aggregator.AggregateRun(pair.Value, new BalanceRunMetadata
                {
                    CandidateId = candidateId,
                    TuningRound = tuningRound,
                    ConfigHash = ReadArgument(arguments, "-balanceConfigHash"),
                    GitCommit = ReadArgument(arguments, "-balanceGitCommit"),
                    UnityVersion = Application.unityVersion,
                    Tester = decision?.Tester,
                    IntendedBuildId = decision?.IntendedBuildId ??
                                      IntendedBuildForSeed(preliminary.Seed),
                    FailureReason = decision?.FailureReason,
                    BoringMoment = decision?.BoringMoment,
                    UnfairMoment = decision?.UnfairMoment,
                    DecisionSummaryPath = decision?.Path,
                    RawTelemetryPath = pair.Key
                }));
            }

            var duplicateSeeds = summaries.GroupBy(value => value.Seed)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(value => value)
                .ToArray();
            if (duplicateSeeds.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate run seeds are not allowed: {string.Join(",", duplicateSeeds)}.");
            }

            var calibration = new RunGrowthCalibrationAggregator().Aggregate(
                summaries,
                candidateId,
                tuningRound);
            var funnel = aggregator.AggregateCardFunnel(
                documents.Values,
                candidateId,
                tuningRound);
            Directory.CreateDirectory(output);
            File.WriteAllText(
                Path.Combine(output, "balance_run_summary.csv"),
                BalanceTelemetryCsv.SerializeRunSummaries(summaries),
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(output, "balance_card_funnel.csv"),
                BalanceTelemetryCsv.SerializeCardFunnel(funnel),
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(output, "balance_fixture_calibration.csv"),
                BalanceGrowthCalibrationCsv.Serialize(calibration),
                new UTF8Encoding(false));

            var expectedSeeds = new HashSet<int>(BalanceSeedSets.S2Runs);
            expectedSeeds.ExceptWith(summaries.Select(value => value.Seed));
            var metadata = new
            {
                balanceSchemaVersion = "0.3.0",
                candidateId,
                tuningRound,
                unityVersion = Application.unityVersion,
                coreClassifierVersion = CoreBuildClassifier.Version,
                runCount = summaries.Count,
                expectedRunCount = BalanceSeedSets.S2Runs.Count,
                missingSeeds = expectedSeeds.OrderBy(value => value).ToArray(),
                calibrationReady = calibration.All(value =>
                    value.CalibrationStatus == "Ready"),
                minimumReadySamplesPerBuild =
                    RunGrowthCalibrationAggregator.MinimumReadySamples,
                generatedAtUtc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(
                Path.Combine(output, "calibration_metadata.json"),
                JsonConvert.SerializeObject(metadata, Formatting.Indented),
                new UTF8Encoding(false));

            Debug.Log(
                $"Growth calibration complete: runs={summaries.Count}, " +
                $"missingSeeds={expectedSeeds.Count}, output={output}.");
        }

        private static Dictionary<int, DecisionSummary> ReadDecisionSummaries(string directory)
        {
            var results = new Dictionary<int, DecisionSummary>();
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return results;
            }

            foreach (var path in Directory.GetFiles(
                         Path.GetFullPath(directory),
                         "*.json",
                         SearchOption.AllDirectories))
            {
                var decision = JsonConvert.DeserializeObject<DecisionSummary>(
                    File.ReadAllText(path));
                if (decision == null || decision.Seed == 0)
                {
                    continue;
                }
                if (results.ContainsKey(decision.Seed))
                {
                    throw new InvalidOperationException(
                        $"Duplicate decision summary seed {decision.Seed}.");
                }
                decision.Path = path;
                results.Add(decision.Seed, decision);
            }
            return results;
        }

        private static string IntendedBuildForSeed(int seed)
        {
            if (seed >= 2000 && seed <= 2002) return "B01_SHIELD";
            if (seed >= 2003 && seed <= 2005) return "B02_BREAK";
            if (seed >= 2006 && seed <= 2008) return "B03_SUMMON";
            if (seed >= 2009 && seed <= 2011) return "B04_DEATH";
            if (seed >= 2012 && seed <= 2014) return "B05_SPELL";
            if (seed >= 2015 && seed <= 2017) return "B06_REFRESH";
            return null;
        }

        private static string ResolveOutputDirectory(string output, string candidateId)
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                return ResolveRepositoryPath(output);
            }
            return Path.Combine(
                RepositoryRoot(),
                "balance-results",
                "phase-6-v0.2",
                candidateId,
                "S2_RUNS");
        }

        private static string ResolveRepositoryPath(string path)
        {
            return Path.GetFullPath(Path.IsPathRooted(path)
                ? path
                : Path.Combine(RepositoryRoot(), path));
        }

        private static string RepositoryRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException(
                                  "Unity project root is unavailable.");
            return Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
        }

        private static string ReadRequiredArgument(
            IReadOnlyList<string> arguments,
            string name)
        {
            return ReadArgument(arguments, name) ??
                   throw new ArgumentException($"{name} is required.");
        }

        private static string ReadArgument(
            IReadOnlyList<string> arguments,
            string name)
        {
            for (var index = 0; index < arguments.Count - 1; index++)
            {
                if (string.Equals(
                        arguments[index],
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }
            return null;
        }

        private sealed class DecisionSummary
        {
            [JsonProperty("seed")]
            public int Seed { get; set; }

            [JsonProperty("tester")]
            public string Tester { get; set; }

            [JsonProperty("intendedBuildId")]
            public string IntendedBuildId { get; set; }

            [JsonProperty("failureReason")]
            public string FailureReason { get; set; }

            [JsonProperty("boringMoment")]
            public string BoringMoment { get; set; }

            [JsonProperty("unfairMoment")]
            public string UnfairMoment { get; set; }

            [JsonIgnore]
            public string Path { get; set; }
        }
    }
}

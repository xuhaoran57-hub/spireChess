using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.Utils;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class BalanceFixtureGrowthCommand
    {
        private const int DefaultSampleCount = 1000;
        private const int GrowthTurnCount = 5;
        private const int RefreshRouteBaseGold = 10;
        private const string DefaultCandidate = "R9-lineup-and-growth-candidate";
        private static readonly string[] GrowthSpellIds =
        {
            "minor_tempering",
            "precise_training",
            "thickhide_potion"
        };

        [MenuItem("Spire Chess/Balance/Generate Five Turn Fixture Growth")]
        public static void RunFromMenu()
        {
            Run(new GrowthOptions
            {
                CandidateId = DefaultCandidate,
                SampleCount = DefaultSampleCount
            });
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run(GrowthOptions.FromCommandLine(Environment.GetCommandLineArgs()));
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
                }
                throw;
            }
        }

        private static void Run(GrowthOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();

            var fixturePath = Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "Balance",
                "balance-fixtures.v0.3.json");
            var fixture = JObject.Parse(File.ReadAllText(fixturePath));
            var builds = (JArray)fixture["builds"] ??
                         throw new InvalidOperationException("Fixture builds are missing.");
            var samples = new List<GrowthSample>();
            foreach (var build in builds.OfType<JObject>())
            {
                var buildId = (string)build["buildId"];
                foreach (var level in new[] { "N", "H" })
                {
                    for (var sampleIndex = 0; sampleIndex < options.SampleCount; sampleIndex++)
                    {
                        var seed = GrowthSeed(buildId, level, sampleIndex);
                        samples.Add(Simulate(
                            configs,
                            build,
                            level,
                            sampleIndex,
                            seed));
                    }
                }
            }

            var calibrations = CreateCalibrations(samples, options.SampleCount);
            var candidateFixture = CreateCandidateFixture(
                fixture,
                calibrations,
                options.CandidateId,
                options.SampleCount);
            var outputDirectory = options.ResolveOutputDirectory();
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, "growth_samples.csv"),
                SerializeSamples(samples),
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(outputDirectory, "balance-fixture-calibration.v0.3.csv"),
                SerializeCalibrations(calibrations, options.SampleCount),
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(outputDirectory, "balance-fixtures.v0.3.json"),
                candidateFixture.ToString(Formatting.Indented) + Environment.NewLine,
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(outputDirectory, "growth_metadata.json"),
                JsonConvert.SerializeObject(new
                {
                    candidateId = options.CandidateId,
                    sampleCountPerBuildAndLevel = options.SampleCount,
                    growthTurnCount = GrowthTurnCount,
                    growthSpellIdsForNonRefreshBuilds = GrowthSpellIds,
                    pressureBattleTurns = new[] { 1, 3, 5 },
                    b06Route = new
                    {
                        goldPolicy = "10 base gold each round plus effect-generated bonus gold",
                        refreshPolicy = "Refresh while a paid or free refresh is affordable",
                        spellPolicy = "Use only temporary spells granted by refresh effects",
                        fixedGrowthSpells = Array.Empty<string>()
                    },
                    generatedAtUtc = DateTime.UtcNow.ToString("O"),
                    unityVersion = Application.unityVersion
                }, Formatting.Indented),
                new UTF8Encoding(false));
            Debug.Log(
                $"Fixture growth complete: samples={samples.Count}, output={outputDirectory}.");
        }

        private static GrowthSample Simulate(
            ConfigService configs,
            JObject build,
            string level,
            int sampleIndex,
            int seed)
        {
            var random = new System.Random(seed);
            var shop = new ShopSession(configs.Minions, configs.Spells, random);
            var isRefreshBuild = (string)build["buildId"] == "B06_REFRESH";
            var startingGoldByTurn = new List<int>();
            var refreshesByTurn = new List<int>();
            var temporarySpellsByTurn = new List<int>();
            Require(shop.StartRound(1), "start growth turn 1");
            var slots = ((JArray)build["slots"]).OfType<JObject>()
                .OrderBy(value => (int)value["slot"])
                .ToList();
            foreach (var slot in slots)
            {
                var slotIndex = (int)slot["slot"];
                var minionId = (string)slot["minionId"];
                var minion = configs.MinionsById[minionId];
                var isGolden = level == "H"
                    ? (bool?)slot["highIsGolden"] ?? true
                    : (bool?)slot["normalIsGolden"] ?? false;
                var card = ShopCardInstance.CreateMinion(
                    $"growth-{level}-{slotIndex}",
                    minion,
                    isGolden);
                if (!shop.Collection.TryAddToBench(card, out var benchIndex))
                {
                    throw new InvalidOperationException("Unable to add growth lineup card.");
                }
                Require(shop.PlayMinion(benchIndex, slotIndex), $"play {minionId}");
            }

            for (var turn = 1; turn <= GrowthTurnCount; turn++)
            {
                if (turn > 1)
                {
                    Require(shop.StartRound(turn), $"start growth turn {turn}");
                }
                var startingGold = 0;
                var refreshes = 0;
                var temporarySpells = 0;
                if (isRefreshBuild)
                {
                    SetRefreshRouteGold(shop, turn);
                    startingGold = shop.Gold;
                    do
                    {
                        refreshes += RefreshWhileAffordable(shop);
                        temporarySpells += UseTemporarySpells(shop, random);
                    }
                    while (shop.FreeRefreshes > 0 ||
                           shop.Gold >= ShopEconomyRules.RefreshCost);
                }
                else
                {
                    SetCalibrationGold(shop, 100);
                    startingGold = shop.Gold;
                    foreach (var spellId in GrowthSpellIds)
                    {
                        UseGrantedSpell(
                            shop,
                            configs.SpellsById[spellId],
                            random.Next(BattleBoardState.SlotCount),
                            $"growth-{level}-{sampleIndex}-{turn}-{spellId}");
                    }
                }
                startingGoldByTurn.Add(startingGold);
                refreshesByTurn.Add(refreshes);
                temporarySpellsByTurn.Add(temporarySpells);

                ResolveGrowthBattle(
                    shop,
                    configs,
                    random,
                    level,
                    turn == 1 || turn == 3 || turn == 5);
                Require(shop.EndRound(), $"end growth turn {turn}");
            }

            var cards = shop.Collection.Battle
                .Select((card, slot) => new GrowthSlotSample(card, slot))
                .ToList();
            return new GrowthSample
            {
                BuildId = (string)build["buildId"],
                Level = level,
                SampleIndex = sampleIndex,
                Seed = seed,
                Attack = cards.Sum(value => value.ExpectedAttack),
                Health = cards.Sum(value => value.ExpectedHealth),
                FlourishStacks = shop.FlourishStacks,
                StartingGoldByTurn = startingGoldByTurn,
                RefreshesByTurn = refreshesByTurn,
                TemporarySpellsByTurn = temporarySpellsByTurn,
                Slots = cards
            };
        }

        private static void ResolveGrowthBattle(
            ShopSession shop,
            ConfigService configs,
            System.Random random,
            string level,
            bool pressure)
        {
            var state = shop.CreateBattleSnapshot();
            var dummy = configs.MinionsById["wandering_swordsman"];
            var attack = pressure ? (level == "H" ? 16 : 8) : 0;
            var health = level == "H" ? 1000 : 500;
            for (var slot = 0; slot < BattleBoardState.SlotCount; slot++)
            {
                state.Enemy[slot] = new BattleMinionRuntime(
                    dummy,
                    false,
                    attack,
                    health);
            }
            var result = new BattleSimulator(random, id =>
                configs.MinionsById.TryGetValue(id, out var minion) ? minion : null)
                .Simulate(state);
            if (result.Diagnostics.HitEffectLimit)
            {
                throw new InvalidOperationException("Growth battle hit the effect limit.");
            }
            shop.ApplyPostCombatSurvivorBuffs(result);
            foreach (var delta in result.PermanentDeltas)
            {
                shop.ModifyOwnedBattleMinion(
                    delta.SourceInstanceId,
                    delta.Attack,
                    delta.Health);
                foreach (var keyword in delta.Keywords)
                {
                    shop.ModifyOwnedBattleMinion(
                        delta.SourceInstanceId,
                        0,
                        0,
                        keyword);
                }
            }
            shop.ApplyFlourish(result.Diagnostics.Player.FlourishGained);
        }

        private static int RefreshWhileAffordable(ShopSession shop)
        {
            var refreshes = 0;
            while (shop.FreeRefreshes > 0 || shop.Gold >= ShopEconomyRules.RefreshCost)
            {
                Require(shop.Refresh(), $"refresh {refreshes + 1}");
                refreshes++;
                if (refreshes >= 256)
                {
                    throw new InvalidOperationException(
                        "Growth refresh route exceeded its safety limit.");
                }
            }
            return refreshes;
        }

        private static void SetRefreshRouteGold(ShopSession shop, int turn)
        {
            var effectBonus = Math.Max(
                0,
                shop.Gold - ShopEconomyRules.GetRoundBudget(turn));
            SetCalibrationGold(shop, RefreshRouteBaseGold + effectBonus);
        }

        private static int UseTemporarySpells(ShopSession shop, System.Random random)
        {
            var used = 0;
            while (true)
            {
                var entry = shop.Collection.Bench
                    .Select((card, index) => new { card, index })
                    .FirstOrDefault(value => value.card != null &&
                        value.card.CardType == ShopCardType.Spell &&
                        value.card.ExpiresAtShopEnd);
                if (entry == null)
                {
                    return used;
                }
                Require(
                    shop.UseSpell(
                        entry.index,
                        random.Next(BattleBoardState.SlotCount)),
                    $"use temporary spell {entry.card.ConfigId}");
                used++;
            }
        }

        private static void UseGrantedSpell(
            ShopSession shop,
            SpellConfig spell,
            int targetSlot,
            string instanceId)
        {
            var card = ShopCardInstance.CreateSpell(instanceId, spell);
            if (!shop.Collection.TryAddToBench(card, out var benchIndex))
            {
                throw new InvalidOperationException($"Unable to add spell {spell.Id}.");
            }
            Require(shop.UseSpell(benchIndex, targetSlot), $"use {spell.Id}");
        }

        private static void SetCalibrationGold(ShopSession shop, int value)
        {
            var property = typeof(ShopSession).GetProperty(
                nameof(ShopSession.Gold),
                BindingFlags.Instance | BindingFlags.Public);
            var setter = property?.GetSetMethod(true) ??
                         throw new InvalidOperationException("ShopSession.Gold setter is unavailable.");
            setter.Invoke(shop, new object[] { value });
        }

        private static void Require(ShopOperationResult result, string operation)
        {
            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Unable to {operation}: {result.Error}.");
            }
        }

        private static IReadOnlyList<GrowthCalibration> CreateCalibrations(
            IReadOnlyList<GrowthSample> samples,
            int sampleCount)
        {
            var results = new List<GrowthCalibration>();
            foreach (var buildId in samples.Select(value => value.BuildId).Distinct())
            {
                var normal = SelectRepresentative(samples.Where(value =>
                    value.BuildId == buildId && value.Level == "N"));
                var high = SelectRepresentative(samples.Where(value =>
                    value.BuildId == buildId && value.Level == "H"));
                results.Add(new GrowthCalibration
                {
                    BuildId = buildId,
                    SampleCount = sampleCount,
                    Normal = normal,
                    High = high
                });
            }
            return results;
        }

        private static GrowthSample SelectRepresentative(IEnumerable<GrowthSample> values)
        {
            var ordered = values.OrderBy(value => value.Attack)
                .ThenBy(value => value.Health)
                .ThenBy(value => value.SampleIndex)
                .ToList();
            if (ordered.Count == 0)
            {
                throw new InvalidOperationException("Growth samples are missing.");
            }
            var attack = Percentile(ordered.Select(value => value.Attack), 0.5d);
            var health = Percentile(ordered.Select(value => value.Health), 0.5d);
            var flourish = Percentile(
                ordered.Select(value => value.FlourishStacks),
                0.5d);
            return ordered
                .OrderBy(value => Math.Abs(value.Attack - attack) +
                                  Math.Abs(value.Health - health) +
                                  Math.Abs(value.FlourishStacks - flourish))
                .ThenBy(value => value.SampleIndex)
                .First();
        }

        private static int Percentile(IEnumerable<int> values, double percentile)
        {
            var ordered = values.OrderBy(value => value).ToList();
            var index = Math.Max(0, Math.Min(
                ordered.Count - 1,
                (int)Math.Ceiling(percentile * ordered.Count) - 1));
            return ordered[index];
        }

        private static JObject CreateCandidateFixture(
            JObject source,
            IReadOnlyList<GrowthCalibration> calibrations,
            string candidateId,
            int sampleCount)
        {
            var candidate = (JObject)source.DeepClone();
            candidate["calibration"]["candidateId"] = candidateId;
            candidate["calibration"]["sourceCsv"] = "balance-fixture-calibration.v0.3.csv";
            candidate["calibration"]["normalPercentile"] = "P50";
            candidate["calibration"]["highPercentile"] = "P50";
            candidate["calibration"]["minimumSamplesPerBuild"] = sampleCount;
            var builds = ((JArray)candidate["builds"]).OfType<JObject>()
                .ToDictionary(value => (string)value["buildId"]);
            foreach (var calibration in calibrations)
            {
                var build = builds[calibration.BuildId];
                build["calibration"] = new JObject
                {
                    ["observedSamples"] = sampleCount,
                    ["formationRate"] = 1.0d,
                    ["normalRepresentativeSeed"] = calibration.Normal.Seed,
                    ["highRepresentativeSeed"] = calibration.High.Seed,
                    ["status"] = "Ready"
                };
                build["normalFlourishStacks"] = calibration.Normal.FlourishStacks;
                build["highFlourishStacks"] = calibration.High.FlourishStacks;
                var slots = ((JArray)build["slots"]).OfType<JObject>()
                    .ToDictionary(value => (int)value["slot"]);
                foreach (var normalSlot in calibration.Normal.Slots)
                {
                    var highSlot = calibration.High.Slots.Single(value =>
                        value.Slot == normalSlot.Slot);
                    var slot = slots[normalSlot.Slot];
                    slot["permanentAttackBonus"] = normalSlot.PermanentAttackBonus;
                    slot["permanentHealthBonus"] = normalSlot.PermanentHealthBonus;
                    slot["normalIsGolden"] = normalSlot.IsGolden;
                    slot["highIsGolden"] = highSlot.IsGolden;
                    slot["highPermanentAttackBonus"] = highSlot.PermanentAttackBonus;
                    slot["highPermanentHealthBonus"] = highSlot.PermanentHealthBonus;
                    slot["expectedNormalAttack"] = normalSlot.ExpectedAttack;
                    slot["expectedNormalHealth"] = normalSlot.ExpectedHealth;
                    slot["expectedHighAttack"] = highSlot.ExpectedAttack;
                    slot["expectedHighHealth"] = highSlot.ExpectedHealth;
                }
            }
            return candidate;
        }

        private static string SerializeSamples(IEnumerable<GrowthSample> samples)
        {
            var builder = new StringBuilder();
            builder.AppendLine("buildId,level,sampleIndex,seed,attack,health,flourishStacks,startingGoldByTurn,totalRefreshes,refreshesByTurn,totalTemporarySpellsUsed,temporarySpellsByTurn,board");
            foreach (var sample in samples)
            {
                AppendCsvRow(builder, new object[]
                {
                    sample.BuildId,
                    sample.Level,
                    sample.SampleIndex,
                    sample.Seed,
                    sample.Attack,
                    sample.Health,
                    sample.FlourishStacks,
                    sample.StartingGoldByTurnText,
                    sample.TotalRefreshes,
                    sample.RefreshesByTurnText,
                    sample.TotalTemporarySpellsUsed,
                    sample.TemporarySpellsByTurnText,
                    sample.BoardText
                });
            }
            return builder.ToString();
        }

        private static string SerializeCalibrations(
            IEnumerable<GrowthCalibration> calibrations,
            int sampleCount)
        {
            var builder = new StringBuilder();
            builder.AppendLine("buildId,samplesPerLevel,normalPercentile,normalRepresentativeSample,normalRepresentativeSeed,normalAttack,normalHealth,normalRepresentativeBoard,highPercentile,highRepresentativeSample,highRepresentativeSeed,highAttack,highHealth,highRepresentativeBoard");
            foreach (var calibration in calibrations)
            {
                AppendCsvRow(builder, new object[]
                {
                    calibration.BuildId,
                    sampleCount,
                    "P50",
                    calibration.Normal.SampleIndex,
                    calibration.Normal.Seed,
                    calibration.Normal.Attack,
                    calibration.Normal.Health,
                    calibration.Normal.BoardText,
                    "P50",
                    calibration.High.SampleIndex,
                    calibration.High.Seed,
                    calibration.High.Attack,
                    calibration.High.Health,
                    calibration.High.BoardText
                });
            }
            return builder.ToString();
        }

        private static void AppendCsvRow(StringBuilder builder, IEnumerable<object> values)
        {
            builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        private static string EscapeCsv(object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (!text.Contains(",") && !text.Contains("\"") &&
                !text.Contains("\r") && !text.Contains("\n"))
            {
                return text;
            }
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        private static int GrowthSeed(string buildId, string level, int sampleIndex)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var character in $"{buildId}:{level}")
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return hash ^ (sampleIndex * 397);
            }
        }

        private static string RepositoryRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException("Unity project root is unavailable.");
            return Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
        }

        private sealed class GrowthOptions
        {
            public string CandidateId { get; set; }
            public int SampleCount { get; set; }
            public string OutputDirectory { get; set; }

            public static GrowthOptions FromCommandLine(IReadOnlyList<string> arguments)
            {
                var countText = ReadArgument(arguments, "-growthSamples");
                return new GrowthOptions
                {
                    CandidateId = ReadArgument(arguments, "-balanceCandidate") ??
                                  DefaultCandidate,
                    SampleCount = int.TryParse(countText, out var count)
                        ? Math.Max(1, count)
                        : DefaultSampleCount,
                    OutputDirectory = ReadArgument(arguments, "-growthOutput")
                };
            }

            public string ResolveOutputDirectory()
            {
                if (!string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    return Path.GetFullPath(Path.IsPathRooted(OutputDirectory)
                        ? OutputDirectory
                        : Path.Combine(RepositoryRoot(), OutputDirectory));
                }
                return Path.Combine(
                    RepositoryRoot(),
                    "balance-results",
                    "phase-6-v0.3",
                    CandidateId,
                    "GROWTH");
            }

            private static string ReadArgument(IReadOnlyList<string> arguments, string name)
            {
                for (var index = 0; index < arguments.Count - 1; index++)
                {
                    if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                    {
                        return arguments[index + 1];
                    }
                }
                return null;
            }
        }

        private sealed class GrowthSample
        {
            public string BuildId { get; set; }
            public string Level { get; set; }
            public int SampleIndex { get; set; }
            public int Seed { get; set; }
            public int Attack { get; set; }
            public int Health { get; set; }
            public int FlourishStacks { get; set; }
            public IReadOnlyList<int> StartingGoldByTurn { get; set; }
            public IReadOnlyList<int> RefreshesByTurn { get; set; }
            public IReadOnlyList<int> TemporarySpellsByTurn { get; set; }
            public IReadOnlyList<GrowthSlotSample> Slots { get; set; }
            public int TotalRefreshes => RefreshesByTurn?.Sum() ?? 0;
            public int TotalTemporarySpellsUsed => TemporarySpellsByTurn?.Sum() ?? 0;
            public string StartingGoldByTurnText => string.Join("/", StartingGoldByTurn ?? Array.Empty<int>());
            public string RefreshesByTurnText => string.Join("/", RefreshesByTurn ?? Array.Empty<int>());
            public string TemporarySpellsByTurnText => string.Join("/", TemporarySpellsByTurn ?? Array.Empty<int>());
            public string BoardText => string.Join(";", Slots.Select(value => value.BoardText)
                .Concat(FlourishStacks > 0
                    ? new[] { $"flourish={FlourishStacks}" }
                    : Array.Empty<string>()));
        }

        private sealed class GrowthSlotSample
        {
            public GrowthSlotSample(ShopCardInstance card, int slot)
            {
                if (card == null) throw new InvalidOperationException($"Growth slot {slot} is empty.");
                Slot = slot;
                MinionId = card.ConfigId;
                IsGolden = card.IsGolden;
                PermanentAttackBonus = card.PermanentAttackBonus;
                PermanentHealthBonus = card.PermanentHealthBonus;
                ExpectedAttack = (card.IsGolden ? card.Minion.GoldenAttack : card.Minion.Attack) +
                                 card.PermanentAttackBonus;
                ExpectedHealth = (card.IsGolden ? card.Minion.GoldenHealth : card.Minion.Health) +
                                 card.PermanentHealthBonus;
            }

            public int Slot { get; }
            public string MinionId { get; }
            public bool IsGolden { get; }
            public int PermanentAttackBonus { get; }
            public int PermanentHealthBonus { get; }
            public int ExpectedAttack { get; }
            public int ExpectedHealth { get; }
            public string BoardText => $"{MinionId}={ExpectedAttack}/{ExpectedHealth}";
        }

        private sealed class GrowthCalibration
        {
            public string BuildId { get; set; }
            public int SampleCount { get; set; }
            public GrowthSample Normal { get; set; }
            public GrowthSample High { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SpireChess.Battle;
using SpireChess.Config;

namespace SpireChess.Simulation
{
    public static class BalanceSeedSets
    {
        public static IReadOnlyList<int> S0Smoke { get; } =
            Enumerable.Range(1000, 100).ToList().AsReadOnly();
        public static IReadOnlyList<int> S1Calibration { get; } =
            Enumerable.Range(1000, 1000).ToList().AsReadOnly();
        public static IReadOnlyList<int> S2Runs { get; } =
            Enumerable.Range(2000, 20).ToList().AsReadOnly();
        public static IReadOnlyList<int> S3HoldoutA { get; } =
            Enumerable.Range(9000, 100).ToList().AsReadOnly();
        public static IReadOnlyList<int> S4HoldoutB { get; } =
            Enumerable.Range(9100, 100).ToList().AsReadOnly();
    }

    public sealed class BalanceFixtureCatalog
    {
        private readonly BalanceFixtureFile file;
        private readonly Func<string, MinionConfig> resolveMinion;

        private BalanceFixtureCatalog(
            BalanceFixtureFile file,
            Func<string, MinionConfig> resolveMinion)
        {
            this.file = file ?? throw new ArgumentNullException(nameof(file));
            this.resolveMinion = resolveMinion ?? throw new ArgumentNullException(nameof(resolveMinion));
        }

        public string FixtureVersion => file.FixtureVersion;
        public string CoreClassifierVersion => file.CoreClassifierVersion;
        public IReadOnlyList<string> BuildIds => file.Builds.Select(value => value.BuildId)
            .ToList().AsReadOnly();

        public static BalanceFixtureCatalog Load(
            string json,
            Func<string, MinionConfig> resolveMinion)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Fixture JSON is required.", nameof(json));
            }

            var file = JsonConvert.DeserializeObject<BalanceFixtureFile>(json);
            var catalog = new BalanceFixtureCatalog(file, resolveMinion);
            var errors = catalog.Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            return catalog;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (file.FixtureVersion != "0.2.0" && file.FixtureVersion != "0.3.0")
            {
                errors.Add("fixtureVersion must be 0.2.0 or 0.3.0.");
            }
            if (file.FixtureVersion == "0.3.0" && file.Calibration == null)
            {
                errors.Add("fixtureVersion 0.3.0 requires calibration metadata.");
            }
            if (file.CoreClassifierVersion != "0.2.1" &&
                file.CoreClassifierVersion != "0.2.2")
            {
                errors.Add("coreClassifierVersion must be 0.2.1 or 0.2.2.");
            }
            if (file.Builds == null || file.Builds.Count != 6)
            {
                errors.Add("Exactly six balance builds are required.");
                return errors;
            }
            if (file.Builds.Select(value => value.BuildId).Distinct().Count() != 6)
            {
                errors.Add("Balance build IDs must be unique.");
            }

            foreach (var build in file.Builds)
            {
                ValidateBuild(build, errors);
            }

            if (file.Dummies == null ||
                file.Dummies.All(value => value.Id != "D00_OUTPUT_DUMMY") ||
                file.Dummies.All(value => value.Id != "D01_MECHANIC_PRESSURE_N") ||
                file.Dummies.All(value => value.Id != "D01_MECHANIC_PRESSURE_H"))
            {
                errors.Add("D00 and both D01 pressure fixtures are required.");
            }

            return errors;
        }

        public BattleBoardState CreateFixture(string buildId, string developmentLevel)
        {
            var build = file.Builds.SingleOrDefault(value => value.BuildId == buildId);
            if (build == null)
            {
                throw new ArgumentException($"Unknown build ID: {buildId}.", nameof(buildId));
            }
            if (developmentLevel != "N" && developmentLevel != "H")
            {
                throw new ArgumentException("Development level must be N or H.", nameof(developmentLevel));
            }

            var state = new BattleBoardState();
            var fixtureId = $"{buildId}_{developmentLevel}";
            var flourishStacks = developmentLevel == "H"
                ? build.HighFlourishStacks
                : build.NormalFlourishStacks;
            state.PlayerFlourishStacks = flourishStacks;
            var overlays = developmentLevel == "H" ? build.HighOverlay : build.NormalOverlay;
            foreach (var slot in build.Slots.OrderBy(value => value.Slot))
            {
                var config = resolveMinion(slot.MinionId);
                var overlay = (overlays ?? new List<BalanceOverlayDefinition>())
                    .FirstOrDefault(value => value.Slot == slot.Slot);
                var isGolden = developmentLevel == "H"
                    ? slot.HighIsGolden ?? true
                    : slot.NormalIsGolden ?? false;
                var permanentAttack = developmentLevel == "H"
                    ? slot.HighPermanentAttackBonus ?? slot.PermanentAttackBonus * 2
                    : slot.PermanentAttackBonus;
                var permanentHealth = developmentLevel == "H"
                    ? slot.HighPermanentHealthBonus ?? slot.PermanentHealthBonus * 2
                    : slot.PermanentHealthBonus;
                var baseAttack = isGolden ? config.GoldenAttack : config.Attack;
                var baseHealth = isGolden ? config.GoldenHealth : config.Health;
                var temporaryAttack = overlay?.Attack ?? 0;
                var temporaryHealth = overlay?.Health ?? 0;
                var flourishAttack = config.Race == "WildSpirit" ? flourishStacks : 0;
                var runtime = new BattleMinionRuntime(
                    config,
                    isGolden,
                    baseAttack + permanentAttack + temporaryAttack + flourishAttack,
                    baseHealth + permanentHealth + temporaryHealth,
                    $"{fixtureId}-S{slot.Slot}",
                    permanentAttack,
                    permanentHealth,
                    overlay?.Keywords);
                state.Player[slot.Slot] = runtime;
            }

            return state;
        }

        public BattleBoardState CreateDummy(string dummyId)
        {
            var definition = file.Dummies.SingleOrDefault(value => value.Id == dummyId);
            if (definition == null)
            {
                throw new ArgumentException($"Unknown dummy ID: {dummyId}.", nameof(dummyId));
            }

            var config = resolveMinion(definition.MinionId);
            var state = new BattleBoardState();
            for (var slot = 0; slot < BattleBoardState.SlotCount; slot++)
            {
                state.Player[slot] = new BattleMinionRuntime(
                    config,
                    false,
                    definition.Attack,
                    definition.Health,
                    $"{dummyId}-S{slot}");
            }
            return state;
        }

        public BattleBoardState CreateMatch(string playerFixtureId, string enemyFixtureId)
        {
            var player = CreateNamedFixture(playerFixtureId);
            var enemy = CreateNamedFixture(enemyFixtureId);
            var state = new BattleBoardState();
            for (var slot = 0; slot < BattleBoardState.SlotCount; slot++)
            {
                state.Player[slot] = player.Player[slot]?.Clone();
                state.Enemy[slot] = enemy.Player[slot]?.Clone();
            }
            state.PlayerFlourishStacks = player.PlayerFlourishStacks;
            state.EnemyFlourishStacks = enemy.PlayerFlourishStacks;
            return state;
        }

        private BattleBoardState CreateNamedFixture(string fixtureId)
        {
            if (fixtureId != null && fixtureId.StartsWith("D", StringComparison.Ordinal))
            {
                return CreateDummy(fixtureId);
            }
            if (string.IsNullOrWhiteSpace(fixtureId) || fixtureId.Length < 3 ||
                (fixtureId.EndsWith("_N", StringComparison.Ordinal) == false &&
                 fixtureId.EndsWith("_H", StringComparison.Ordinal) == false))
            {
                throw new ArgumentException($"Invalid fixture ID: {fixtureId}.", nameof(fixtureId));
            }

            return CreateFixture(
                fixtureId.Substring(0, fixtureId.Length - 2),
                fixtureId.Substring(fixtureId.Length - 1));
        }

        private void ValidateBuild(BalanceBuildDefinition build, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(build.BuildId))
            {
                errors.Add("Every build requires buildId.");
                return;
            }
            if (file.FixtureVersion == "0.3.0")
            {
                if (build.Calibration == null)
                {
                    errors.Add($"{build.BuildId} requires v0.3 calibration evidence.");
                }
                else if (build.Calibration.ObservedSamples <
                         Math.Max(1, file.Calibration?.MinimumSamplesPerBuild ?? 1) ||
                         build.Calibration.Status != "Ready")
                {
                    errors.Add($"{build.BuildId} does not have ready v0.3 calibration evidence.");
                }
            }
            if (build.Slots == null || build.Slots.Count != BattleBoardState.SlotCount ||
                !build.Slots.Select(value => value.Slot)
                    .OrderBy(value => value)
                    .SequenceEqual(Enumerable.Range(0, BattleBoardState.SlotCount)))
            {
                errors.Add($"{build.BuildId} must define slots 0-4 exactly once.");
                return;
            }

            foreach (var slot in build.Slots)
            {
                var config = resolveMinion(slot.MinionId);
                if (config == null)
                {
                    errors.Add($"{build.BuildId} slot {slot.Slot} has unknown minion {slot.MinionId}.");
                    continue;
                }
                if (config.IsToken)
                {
                    errors.Add($"{build.BuildId} slot {slot.Slot} cannot use a Token.");
                    continue;
                }
                var normalGolden = slot.NormalIsGolden ?? false;
                var highGolden = slot.HighIsGolden ?? true;
                var highAttackBonus = slot.HighPermanentAttackBonus ??
                    slot.PermanentAttackBonus * 2;
                var highHealthBonus = slot.HighPermanentHealthBonus ??
                    slot.PermanentHealthBonus * 2;
                if ((normalGolden ? config.GoldenAttack : config.Attack) +
                        slot.PermanentAttackBonus != slot.ExpectedNormalAttack ||
                    (normalGolden ? config.GoldenHealth : config.Health) +
                        slot.PermanentHealthBonus != slot.ExpectedNormalHealth)
                {
                    errors.Add($"{build.BuildId} slot {slot.Slot} normal stats do not match config.");
                }
                if ((highGolden ? config.GoldenAttack : config.Attack) + highAttackBonus !=
                        slot.ExpectedHighAttack ||
                    (highGolden ? config.GoldenHealth : config.Health) + highHealthBonus !=
                        slot.ExpectedHighHealth)
                {
                    errors.Add($"{build.BuildId} slot {slot.Slot} high stats do not match config.");
                }
                if (file.FixtureVersion == "0.3.0" &&
                    (!slot.NormalIsGolden.HasValue || !slot.HighIsGolden.HasValue ||
                     !slot.HighPermanentAttackBonus.HasValue ||
                     !slot.HighPermanentHealthBonus.HasValue))
                {
                    errors.Add($"{build.BuildId} slot {slot.Slot} requires explicit v0.3 N/H values.");
                }
            }

            if (build.NormalFlourishStacks < 0 || build.HighFlourishStacks < 0)
            {
                errors.Add($"{build.BuildId} has invalid flourish stacks.");
            }

            ValidateOverlays(build.BuildId, build.NormalOverlay, errors);
            ValidateOverlays(build.BuildId, build.HighOverlay, errors);
        }

        private static void ValidateOverlays(
            string buildId,
            IEnumerable<BalanceOverlayDefinition> overlays,
            ICollection<string> errors)
        {
            var materialized = (overlays ?? Enumerable.Empty<BalanceOverlayDefinition>()).ToList();
            if (materialized.Any(value => value.Slot < 0 || value.Slot >= BattleBoardState.SlotCount) ||
                materialized.Select(value => value.Slot).Distinct().Count() != materialized.Count)
            {
                errors.Add($"{buildId} has invalid or duplicate overlay slots.");
            }
        }
    }

    public sealed class BalanceFixtureFile
    {
        [JsonProperty("fixtureVersion")]
        public string FixtureVersion { get; set; }

        [JsonProperty("coreClassifierVersion")]
        public string CoreClassifierVersion { get; set; }

        [JsonProperty("calibration")]
        public BalanceFixtureCalibrationDefinition Calibration { get; set; }

        [JsonProperty("builds")]
        public List<BalanceBuildDefinition> Builds { get; set; } = new List<BalanceBuildDefinition>();

        [JsonProperty("dummies")]
        public List<BalanceDummyDefinition> Dummies { get; set; } = new List<BalanceDummyDefinition>();
    }

    public sealed class BalanceBuildDefinition
    {
        [JsonProperty("buildId")]
        public string BuildId { get; set; }

        [JsonProperty("slots")]
        public List<BalanceSlotDefinition> Slots { get; set; } = new List<BalanceSlotDefinition>();

        [JsonProperty("normalFlourishStacks")]
        public int NormalFlourishStacks { get; set; }

        [JsonProperty("highFlourishStacks")]
        public int HighFlourishStacks { get; set; }

        [JsonProperty("normalOverlay")]
        public List<BalanceOverlayDefinition> NormalOverlay { get; set; }
            = new List<BalanceOverlayDefinition>();

        [JsonProperty("highOverlay")]
        public List<BalanceOverlayDefinition> HighOverlay { get; set; }
            = new List<BalanceOverlayDefinition>();

        [JsonProperty("calibration")]
        public BalanceBuildCalibrationDefinition Calibration { get; set; }
    }

    public sealed class BalanceSlotDefinition
    {
        [JsonProperty("slot")]
        public int Slot { get; set; }

        [JsonProperty("minionId")]
        public string MinionId { get; set; }

        [JsonProperty("permanentAttackBonus")]
        public int PermanentAttackBonus { get; set; }

        [JsonProperty("permanentHealthBonus")]
        public int PermanentHealthBonus { get; set; }

        [JsonProperty("normalIsGolden")]
        public bool? NormalIsGolden { get; set; }

        [JsonProperty("highIsGolden")]
        public bool? HighIsGolden { get; set; }

        [JsonProperty("highPermanentAttackBonus")]
        public int? HighPermanentAttackBonus { get; set; }

        [JsonProperty("highPermanentHealthBonus")]
        public int? HighPermanentHealthBonus { get; set; }

        [JsonProperty("expectedNormalAttack")]
        public int ExpectedNormalAttack { get; set; }

        [JsonProperty("expectedNormalHealth")]
        public int ExpectedNormalHealth { get; set; }

        [JsonProperty("expectedHighAttack")]
        public int ExpectedHighAttack { get; set; }

        [JsonProperty("expectedHighHealth")]
        public int ExpectedHighHealth { get; set; }
    }

    public sealed class BalanceFixtureCalibrationDefinition
    {
        [JsonProperty("candidateId")]
        public string CandidateId { get; set; }

        [JsonProperty("sourceCsv")]
        public string SourceCsv { get; set; }

        [JsonProperty("normalPercentile")]
        public string NormalPercentile { get; set; }

        [JsonProperty("highPercentile")]
        public string HighPercentile { get; set; }

        [JsonProperty("minimumSamplesPerBuild")]
        public int MinimumSamplesPerBuild { get; set; } = 10;
    }

    public sealed class BalanceBuildCalibrationDefinition
    {
        [JsonProperty("observedSamples")]
        public int ObservedSamples { get; set; }

        [JsonProperty("formationRate")]
        public double FormationRate { get; set; }

        [JsonProperty("normalRepresentativeSeed")]
        public int NormalRepresentativeSeed { get; set; }

        [JsonProperty("highRepresentativeSeed")]
        public int HighRepresentativeSeed { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public sealed class BalanceOverlayDefinition
    {
        [JsonProperty("slot")]
        public int Slot { get; set; }

        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("health")]
        public int Health { get; set; }

        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();
    }

    public sealed class BalanceDummyDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("minionId")]
        public string MinionId { get; set; }

        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("health")]
        public int Health { get; set; }
    }

    public sealed class BalanceBattleScenario
    {
        public BalanceBattleScenario(
            string scenarioId,
            string playerFixtureId,
            string enemyFixtureId,
            string developmentLevel,
            string orientation,
            BattleBoardState boardState)
        {
            ScenarioId = scenarioId;
            PlayerFixtureId = playerFixtureId;
            EnemyFixtureId = enemyFixtureId;
            DevelopmentLevel = developmentLevel;
            Orientation = orientation;
            BoardState = boardState;
        }

        public string ScenarioId { get; }
        public string PlayerFixtureId { get; }
        public string EnemyFixtureId { get; }
        public string DevelopmentLevel { get; }
        public string Orientation { get; }
        public bool CountsForCompetitiveSafety => Orientation != "OUTPUT_DUMMY";
        public BattleBoardState BoardState { get; }
    }

    public static class BalanceMatrixBuilder
    {
        public static IReadOnlyList<BalanceBattleScenario> Build(
            BalanceFixtureCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var scenarios = new List<BalanceBattleScenario>();
            foreach (var level in new[] { "N", "H" })
            {
                for (var first = 0; first < catalog.BuildIds.Count; first++)
                {
                    for (var second = first + 1; second < catalog.BuildIds.Count; second++)
                    {
                        var a = $"{catalog.BuildIds[first]}_{level}";
                        var b = $"{catalog.BuildIds[second]}_{level}";
                        scenarios.Add(Create(catalog, a, b, level, "A_PLAYER"));
                        scenarios.Add(Create(catalog, b, a, level, "B_PLAYER"));
                    }
                }
            }

            foreach (var level in new[] { "N", "H" })
            {
                foreach (var buildId in catalog.BuildIds)
                {
                    var fixtureId = $"{buildId}_{level}";
                    scenarios.Add(Create(
                        catalog,
                        fixtureId,
                        "D00_OUTPUT_DUMMY",
                        level,
                        "OUTPUT_DUMMY"));
                    scenarios.Add(Create(
                        catalog,
                        fixtureId,
                        $"D01_MECHANIC_PRESSURE_{level}",
                        level,
                        "MECHANIC_PRESSURE"));
                }
            }

            return scenarios.AsReadOnly();
        }

        private static BalanceBattleScenario Create(
            BalanceFixtureCatalog catalog,
            string playerFixtureId,
            string enemyFixtureId,
            string level,
            string orientation)
        {
            return new BalanceBattleScenario(
                $"{playerFixtureId}__VS__{enemyFixtureId}",
                playerFixtureId,
                enemyFixtureId,
                level,
                orientation,
                catalog.CreateMatch(playerFixtureId, enemyFixtureId));
        }
    }

    public sealed class BalanceScenarioResult
    {
        public BalanceScenarioResult(
            BalanceBattleScenario scenario,
            BattleBatchResult batch)
        {
            Scenario = scenario;
            Batch = batch;
        }

        public BalanceBattleScenario Scenario { get; }
        public BattleBatchResult Batch { get; }
    }

    public sealed class BalanceMatrixRunner
    {
        private readonly BattleBatchRunner runner;

        public BalanceMatrixRunner(Func<string, MinionConfig> resolveMinion)
        {
            runner = new BattleBatchRunner(resolveMinion);
        }

        public IReadOnlyList<BalanceScenarioResult> Run(
            BalanceFixtureCatalog catalog,
            IEnumerable<int> seeds)
        {
            if (seeds == null) throw new ArgumentNullException(nameof(seeds));
            var materializedSeeds = seeds.ToList();
            return BalanceMatrixBuilder.Build(catalog)
                .Select(scenario => new BalanceScenarioResult(
                    scenario,
                    runner.Run(scenario.BoardState, materializedSeeds)))
                .ToList().AsReadOnly();
        }
    }
}

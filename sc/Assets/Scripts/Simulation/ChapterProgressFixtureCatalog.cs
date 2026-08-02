using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SpireChess.Battle;
using SpireChess.Config;

namespace SpireChess.Simulation
{
    public sealed class ChapterProgressFixtureCatalog
    {
        private readonly ChapterProgressFixtureFile file;
        private readonly BalanceFixtureCatalog sourceFixtures;
        private readonly Func<string, MinionConfig> resolveMinion;

        private ChapterProgressFixtureCatalog(
            ChapterProgressFixtureFile file,
            BalanceFixtureCatalog sourceFixtures,
            Func<string, MinionConfig> resolveMinion)
        {
            this.file = file ?? throw new ArgumentNullException(nameof(file));
            this.sourceFixtures = sourceFixtures ??
                                  throw new ArgumentNullException(nameof(sourceFixtures));
            this.resolveMinion = resolveMinion ??
                                 throw new ArgumentNullException(nameof(resolveMinion));
        }

        public string FixtureVersion => file.FixtureVersion;
        public string SourceFixtureVersion => file.SourceFixtureVersion;
        public string CoreClassifierVersion => sourceFixtures.CoreClassifierVersion;
        public int ShopsPerFloor => file.ShopsPerFloor;
        public IReadOnlyList<string> BuildIds => sourceFixtures.BuildIds;
        public IReadOnlyList<ChapterProgressCheckpointDefinition> Checkpoints =>
            file.Checkpoints.AsReadOnly();

        public static ChapterProgressFixtureCatalog Load(
            string json,
            BalanceFixtureCatalog sourceFixtures,
            Func<string, MinionConfig> resolveMinion)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "Chapter progress fixture JSON is required.",
                    nameof(json));
            }

            var file = JsonConvert.DeserializeObject<ChapterProgressFixtureFile>(json);
            var catalog = new ChapterProgressFixtureCatalog(
                file,
                sourceFixtures,
                resolveMinion);
            var errors = catalog.Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }
            return catalog;
        }

        public ChapterProgressCheckpointDefinition ResolveCheckpoint(
            int floor,
            int combatIndex)
        {
            if (floor < 1 || floor > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(floor));
            }
            if (combatIndex < 1 || combatIndex > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(combatIndex));
            }

            var checkpointCombatIndex = combatIndex <= 2
                ? 2
                : combatIndex <= 4 ? 4 : 5;
            return file.Checkpoints.Single(value =>
                value.Floor == floor &&
                value.CombatIndex == checkpointCombatIndex);
        }

        public BattleBoardState CreateFixture(
            int floor,
            int combatIndex,
            string buildId)
        {
            var checkpoint = ResolveCheckpoint(floor, combatIndex);
            var build = file.Builds.SingleOrDefault(value =>
                value.BuildId == buildId);
            if (build == null)
            {
                throw new ArgumentException(
                    $"Unknown chapter progress build ID: {buildId}.",
                    nameof(buildId));
            }

            var roster = build.Rosters.Single(value =>
                value.CheckpointId == checkpoint.Id);
            var normal = sourceFixtures.CreateFixture(buildId, "N");
            var high = sourceFixtures.CreateFixture(buildId, "H");
            var flourishStacks = Scale(
                normal.PlayerFlourishStacks,
                high.PlayerFlourishStacks,
                checkpoint.HighGrowthBlend,
                checkpoint.GrowthScale);
            var state = new BattleBoardState
            {
                PlayerFlourishStacks = flourishStacks
            };

            var goldenSlots = new HashSet<int>(roster.GoldenSlots);
            var overlays = roster.Overlays.ToDictionary(value => value.Slot);
            for (var slot = 0; slot < roster.MinionIds.Count; slot++)
            {
                var config = resolveMinion(roster.MinionIds[slot]) ??
                             throw new InvalidOperationException(
                                 $"Missing chapter progress minion {roster.MinionIds[slot]}.");
                var sourceNormal = normal.Player[slot] ??
                                   throw new InvalidOperationException(
                                       $"Source fixture {buildId}_N slot {slot} is empty.");
                var sourceHigh = high.Player[slot] ??
                                 throw new InvalidOperationException(
                                     $"Source fixture {buildId}_H slot {slot} is empty.");
                overlays.TryGetValue(slot, out var overlay);
                var isGolden = goldenSlots.Contains(slot);
                var permanentAttack = Scale(
                    sourceNormal.PermanentAttackBonus,
                    sourceHigh.PermanentAttackBonus,
                    checkpoint.HighGrowthBlend,
                    checkpoint.GrowthScale);
                var permanentHealth = Scale(
                    sourceNormal.PermanentHealthBonus,
                    sourceHigh.PermanentHealthBonus,
                    checkpoint.HighGrowthBlend,
                    checkpoint.GrowthScale);
                var flourishAttack = config.Race == "WildSpirit"
                    ? flourishStacks
                    : 0;
                state.Player[slot] = new BattleMinionRuntime(
                    config,
                    isGolden,
                    initialAttack:
                        (isGolden ? config.GoldenAttack : config.Attack) +
                        permanentAttack +
                        flourishAttack,
                    initialHealth:
                        (isGolden ? config.GoldenHealth : config.Health) +
                        permanentHealth,
                    sourceInstanceId: $"{buildId}_{checkpoint.Id}-S{slot}",
                    permanentAttackBonus: permanentAttack,
                    permanentHealthBonus: permanentHealth,
                    permanentKeywords: overlay?.Keywords);
            }

            return state;
        }

        private IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (file == null)
            {
                errors.Add("Chapter progress fixture file could not be parsed.");
                return errors;
            }
            if (file.FixtureVersion != "0.4.0")
            {
                errors.Add("chapter progress fixtureVersion must be 0.4.0.");
            }
            if (file.SourceFixtureVersion != sourceFixtures.FixtureVersion)
            {
                errors.Add(
                    $"sourceFixtureVersion {file.SourceFixtureVersion} does not match " +
                    $"{sourceFixtures.FixtureVersion}.");
            }
            if (file.ShopsPerFloor != 6)
            {
                errors.Add("chapter progress fixtures require six shops per floor.");
            }

            ValidateCheckpoints(errors);
            ValidateBuilds(errors);
            return errors.AsReadOnly();
        }

        private void ValidateCheckpoints(ICollection<string> errors)
        {
            var expected = new HashSet<string>(
                from floor in Enumerable.Range(1, 3)
                from combatIndex in new[] { 2, 4, 5 }
                select $"F{floor}_C{combatIndex}",
                StringComparer.Ordinal);
            var actual = new HashSet<string>(
                file.Checkpoints.Select(value => value.Id),
                StringComparer.Ordinal);
            if (!actual.SetEquals(expected) || file.Checkpoints.Count != expected.Count)
            {
                errors.Add(
                    "chapter progress fixtures must define F1/F2/F3 C2, C4 and C5 exactly once.");
            }

            foreach (var checkpoint in file.Checkpoints)
            {
                var expectedId = $"F{checkpoint.Floor}_C{checkpoint.CombatIndex}";
                if (checkpoint.Id != expectedId)
                {
                    errors.Add(
                        $"checkpoint {checkpoint.Id} identity must be {expectedId}.");
                }
                var expectedTurn =
                    (checkpoint.Floor - 1) * file.ShopsPerFloor +
                    checkpoint.CombatIndex;
                if (checkpoint.RunTurn != expectedTurn)
                {
                    errors.Add(
                        $"checkpoint {checkpoint.Id} runTurn must be {expectedTurn}.");
                }
                if (checkpoint.ActiveSlots < 1 ||
                    checkpoint.ActiveSlots > BattleBoardState.SlotCount)
                {
                    errors.Add(
                        $"checkpoint {checkpoint.Id} has invalid activeSlots.");
                }
                if (checkpoint.MaxTavernTier < 1 || checkpoint.MaxTavernTier > 5)
                {
                    errors.Add(
                        $"checkpoint {checkpoint.Id} has invalid maxTavernTier.");
                }
                if (checkpoint.GrowthScale < 0d ||
                    checkpoint.GrowthScale > 1d ||
                    checkpoint.HighGrowthBlend < 0d ||
                    checkpoint.HighGrowthBlend > 1d)
                {
                    errors.Add(
                        $"checkpoint {checkpoint.Id} has invalid growth curve values.");
                }
            }
        }

        private void ValidateBuilds(ICollection<string> errors)
        {
            var expectedBuilds = new HashSet<string>(
                sourceFixtures.BuildIds,
                StringComparer.Ordinal);
            var actualBuilds = new HashSet<string>(
                file.Builds.Select(value => value.BuildId),
                StringComparer.Ordinal);
            if (!actualBuilds.SetEquals(expectedBuilds) ||
                file.Builds.Count != expectedBuilds.Count)
            {
                errors.Add(
                    "chapter progress fixtures must define the six calibrated builds exactly once.");
            }

            var checkpointIds = new HashSet<string>(
                file.Checkpoints.Select(value => value.Id),
                StringComparer.Ordinal);
            foreach (var build in file.Builds)
            {
                var rosterIds = new HashSet<string>(
                    build.Rosters.Select(value => value.CheckpointId),
                    StringComparer.Ordinal);
                if (!rosterIds.SetEquals(checkpointIds) ||
                    build.Rosters.Count != checkpointIds.Count)
                {
                    errors.Add(
                        $"{build.BuildId} must define every chapter progress checkpoint exactly once.");
                    continue;
                }

                foreach (var roster in build.Rosters)
                {
                    var checkpoint = file.Checkpoints.Single(value =>
                        value.Id == roster.CheckpointId);
                    if (roster.MinionIds.Count != checkpoint.ActiveSlots)
                    {
                        errors.Add(
                            $"{build.BuildId} {roster.CheckpointId} must define " +
                            $"{checkpoint.ActiveSlots} active minions.");
                    }
                    for (var slot = 0; slot < roster.MinionIds.Count; slot++)
                    {
                        var minion = resolveMinion(roster.MinionIds[slot]);
                        if (minion == null)
                        {
                            errors.Add(
                                $"{build.BuildId} {roster.CheckpointId} slot {slot} " +
                                $"references missing minion {roster.MinionIds[slot]}.");
                            continue;
                        }
                        if (minion.IsToken)
                        {
                            errors.Add(
                                $"{build.BuildId} {roster.CheckpointId} cannot field a Token.");
                        }
                        if (minion.Tier > checkpoint.MaxTavernTier)
                        {
                            errors.Add(
                                $"{build.BuildId} {roster.CheckpointId} fields T{minion.Tier} " +
                                $"{minion.Id} above the T{checkpoint.MaxTavernTier} gate.");
                        }
                    }

                    if (roster.GoldenSlots.Any(value =>
                            value < 0 || value >= roster.MinionIds.Count) ||
                        roster.GoldenSlots.Distinct().Count() !=
                        roster.GoldenSlots.Count)
                    {
                        errors.Add(
                            $"{build.BuildId} {roster.CheckpointId} has invalid golden slots.");
                    }
                    if (roster.Overlays.Any(value =>
                            value.Slot < 0 ||
                            value.Slot >= roster.MinionIds.Count) ||
                        roster.Overlays.Select(value => value.Slot)
                            .Distinct()
                            .Count() != roster.Overlays.Count)
                    {
                        errors.Add(
                            $"{build.BuildId} {roster.CheckpointId} has invalid overlays.");
                    }
                }

                var finalRoster = build.Rosters.Single(value =>
                    value.CheckpointId == "F3_C5");
                var sourceFinal = sourceFixtures.CreateFixture(build.BuildId, "N");
                var sourceIds = sourceFinal.Player
                    .Where(value => value != null)
                    .Select(value => value.Id);
                if (!finalRoster.MinionIds.SequenceEqual(sourceIds))
                {
                    errors.Add(
                        $"{build.BuildId} F3_C5 must converge to the calibrated final roster.");
                }
            }
        }

        private static int Scale(
            int normal,
            int high,
            double highBlend,
            double growthScale)
        {
            var blended = normal + (high - normal) * highBlend;
            return Math.Max(
                0,
                (int)Math.Round(
                    blended * growthScale,
                    MidpointRounding.AwayFromZero));
        }
    }

    public sealed class ChapterProgressFixtureFile
    {
        [JsonProperty("fixtureVersion")]
        public string FixtureVersion { get; set; }

        [JsonProperty("sourceFixtureVersion")]
        public string SourceFixtureVersion { get; set; }

        [JsonProperty("shopsPerFloor")]
        public int ShopsPerFloor { get; set; }

        [JsonProperty("checkpoints")]
        public List<ChapterProgressCheckpointDefinition> Checkpoints { get; set; } =
            new List<ChapterProgressCheckpointDefinition>();

        [JsonProperty("builds")]
        public List<ChapterProgressBuildDefinition> Builds { get; set; } =
            new List<ChapterProgressBuildDefinition>();
    }

    public sealed class ChapterProgressCheckpointDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("floor")]
        public int Floor { get; set; }

        [JsonProperty("combatIndex")]
        public int CombatIndex { get; set; }

        [JsonProperty("runTurn")]
        public int RunTurn { get; set; }

        [JsonProperty("activeSlots")]
        public int ActiveSlots { get; set; }

        [JsonProperty("maxTavernTier")]
        public int MaxTavernTier { get; set; }

        [JsonProperty("growthScale")]
        public double GrowthScale { get; set; }

        [JsonProperty("highGrowthBlend")]
        public double HighGrowthBlend { get; set; }

        public string Stage => $"C{CombatIndex}";
    }

    public sealed class ChapterProgressBuildDefinition
    {
        [JsonProperty("buildId")]
        public string BuildId { get; set; }

        [JsonProperty("rosters")]
        public List<ChapterProgressRosterDefinition> Rosters { get; set; } =
            new List<ChapterProgressRosterDefinition>();
    }

    public sealed class ChapterProgressRosterDefinition
    {
        [JsonProperty("checkpointId")]
        public string CheckpointId { get; set; }

        [JsonProperty("minionIds")]
        public List<string> MinionIds { get; set; } = new List<string>();

        [JsonProperty("goldenSlots")]
        public List<int> GoldenSlots { get; set; } = new List<int>();

        [JsonProperty("overlays")]
        public List<BalanceOverlayDefinition> Overlays { get; set; } =
            new List<BalanceOverlayDefinition>();
    }
}

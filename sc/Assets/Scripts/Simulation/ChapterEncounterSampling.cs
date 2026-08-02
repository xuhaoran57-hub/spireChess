using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;

namespace SpireChess.Simulation
{
    public static class ChapterEncounterSource
    {
        public const string Map = "Map";
        public const string Event = "Event";
    }

    public sealed class ChapterEncounterDefinition
    {
        public ChapterEncounterDefinition(
            string source,
            string mapId,
            string mapName,
            int floor,
            string nodeId,
            string nodeType,
            RunNodeType battleNodeType,
            int combatIndex,
            string routeTag,
            string eventId,
            string eventName,
            EncounterConfig encounter,
            int threatRating)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            MapId = mapId ?? throw new ArgumentNullException(nameof(mapId));
            MapName = mapName ?? throw new ArgumentNullException(nameof(mapName));
            if (floor < 1) throw new ArgumentOutOfRangeException(nameof(floor));
            Floor = floor;
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
            BattleNodeType = battleNodeType;
            if (combatIndex < 1) throw new ArgumentOutOfRangeException(nameof(combatIndex));
            CombatIndex = combatIndex;
            RouteTag = routeTag ?? string.Empty;
            EventId = eventId ?? string.Empty;
            EventName = eventName ?? string.Empty;
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            ThreatRating = threatRating;
        }

        public string Source { get; }
        public string MapId { get; }
        public string MapName { get; }
        public int Floor { get; }
        public string NodeId { get; }
        public string NodeType { get; }
        public RunNodeType BattleNodeType { get; }
        public int CombatIndex { get; }
        public string RouteTag { get; }
        public string EventId { get; }
        public string EventName { get; }
        public EncounterConfig Encounter { get; }
        public int ThreatRating { get; }
        public string EncounterId => Encounter.Id;
        public string EncounterName => Encounter.Name;
        public string DefinitionId => $"{Source}:{MapId}:{NodeId}:{EncounterId}";
    }

    public static class ChapterEncounterCatalog
    {
        public static IReadOnlyList<ChapterEncounterDefinition> Build(ConfigService configs)
        {
            if (configs == null) throw new ArgumentNullException(nameof(configs));
            if (configs.ContentRelease == null)
            {
                throw new InvalidOperationException(
                    "Content release must be loaded before encounter sampling.");
            }

            var releasedEncounterIds = new HashSet<string>(
                configs.ContentRelease.EncounterIds ?? new List<string>(),
                StringComparer.Ordinal);
            var releasedEventIds = new HashSet<string>(
                configs.ContentRelease.EventIds ?? new List<string>(),
                StringComparer.Ordinal);
            var mapsByFloor = configs.RunMaps.ToDictionary(value => value.Floor);
            var definitions = new List<ChapterEncounterDefinition>();

            foreach (var map in configs.RunMaps.OrderBy(value => value.Floor))
            {
                foreach (var node in map.Nodes
                             .OrderBy(value => value.CombatIndex)
                             .ThenBy(value => value.Row)
                             .ThenBy(value => value.Id))
                {
                    if (!Enum.TryParse(node.Type, true, out RunNodeType nodeType) ||
                        (nodeType != RunNodeType.Normal &&
                         nodeType != RunNodeType.Elite &&
                         nodeType != RunNodeType.Boss))
                    {
                        continue;
                    }

                    var encounter = RequireReleasedEncounter(
                        configs,
                        releasedEncounterIds,
                        node.PayloadId);
                    definitions.Add(new ChapterEncounterDefinition(
                        ChapterEncounterSource.Map,
                        map.Id,
                        map.DisplayName,
                        map.Floor,
                        node.Id,
                        node.Type,
                        nodeType,
                        node.CombatIndex,
                        node.RouteTag,
                        string.Empty,
                        string.Empty,
                        encounter,
                        ChapterThreatRating.Calculate(
                            map.Floor,
                            node.CombatIndex,
                            nodeType,
                            node.RouteTag,
                            encounter.DamageBonus)));
                }
            }

            var seenEventEncounterIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var eventConfig in configs.EventsById.Values
                         .Where(value => releasedEventIds.Contains(value.Id))
                         .OrderBy(value => value.Id))
            {
                foreach (var option in eventConfig.Options ?? new List<EventOptionConfig>())
                {
                    if (string.IsNullOrWhiteSpace(option.FollowupEncounterId) ||
                        !seenEventEncounterIds.Add(option.FollowupEncounterId))
                    {
                        continue;
                    }

                    var encounter = RequireReleasedEncounter(
                        configs,
                        releasedEncounterIds,
                        option.FollowupEncounterId);
                    if (!mapsByFloor.TryGetValue(encounter.Floor, out var map))
                    {
                        throw new InvalidOperationException(
                            $"Event encounter {encounter.Id} has no chapter map for floor " +
                            $"{encounter.Floor}.");
                    }

                    definitions.Add(new ChapterEncounterDefinition(
                        ChapterEncounterSource.Event,
                        map.Id,
                        map.DisplayName,
                        map.Floor,
                        eventConfig.Id,
                        "EventCombat",
                        RunNodeType.Normal,
                        4,
                        "Event",
                        eventConfig.Id,
                        eventConfig.Name,
                        encounter,
                        ChapterThreatRating.Calculate(
                            map.Floor,
                            4,
                            RunNodeType.Normal,
                            string.Empty,
                            encounter.DamageBonus)));
                }
            }

            return definitions
                .OrderBy(value => value.Floor)
                .ThenBy(value => value.CombatIndex)
                .ThenBy(value => value.Source == ChapterEncounterSource.Event ? 1 : 0)
                .ThenBy(value => value.RouteTag)
                .ThenBy(value => value.NodeId)
                .ToList()
                .AsReadOnly();
        }

        private static EncounterConfig RequireReleasedEncounter(
            ConfigService configs,
            ISet<string> releasedEncounterIds,
            string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId) ||
                !configs.EncountersById.TryGetValue(encounterId, out var encounter))
            {
                throw new InvalidOperationException(
                    $"Formal encounter is missing from loaded configs: {encounterId}.");
            }
            if (!releasedEncounterIds.Contains(encounterId))
            {
                throw new InvalidOperationException(
                    $"Reachable encounter is not in the content release: {encounterId}.");
            }
            return encounter;
        }
    }

    public sealed class ChapterEncounterScenarioResult
    {
        public ChapterEncounterScenarioResult(
            ChapterEncounterDefinition definition,
            string buildId,
            string developmentLevel,
            BattleBatchResult batch,
            string fixtureId = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            BuildId = buildId ?? throw new ArgumentNullException(nameof(buildId));
            DevelopmentLevel = developmentLevel ??
                               throw new ArgumentNullException(nameof(developmentLevel));
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
            FixtureId = string.IsNullOrWhiteSpace(fixtureId)
                ? $"{buildId}_{developmentLevel}"
                : fixtureId;
        }

        public ChapterEncounterDefinition Definition { get; }
        public string BuildId { get; }
        public string DevelopmentLevel { get; }
        public string FixtureId { get; }
        public BattleBatchResult Batch { get; }
        public string ScenarioId =>
            $"{Definition.DefinitionId}:{FixtureId}";

        public int RoundLimitCount => Batch.Samples.Count(value =>
            value.OutcomeReason == BattleOutcomeReason.RoundLimit);
        public int EffectLimitHitCount => Batch.Samples.Count(value =>
            value.Diagnostics != null && value.Diagnostics.HitEffectLimit);
        public double AverageRounds => ChapterEncounterStatistics.Average(
            SuccessfulSamples().Select(value => value.Diagnostics.RoundCount));
        public double P90Rounds => ChapterEncounterStatistics.Percentile(
            SuccessfulSamples().Select(value => value.Diagnostics.RoundCount),
            0.90d);
        public double AveragePlayerSurvivors => ChapterEncounterStatistics.Average(
            SuccessfulSamples().Select(value => value.PlayerSurvivors));
        public double AverageEnemySurvivors => ChapterEncounterStatistics.Average(
            SuccessfulSamples().Select(value => value.EnemySurvivors));
        public int EnemyCleaveHits => SumEnemy(value => value.CleaveHits);
        public int EnemySummonAttempts => SumEnemy(value => value.SummonAttempts);
        public int EnemySummonSuccesses => SumEnemy(value => value.SummonSuccesses);
        public int EnemyShieldsGranted => SumEnemy(value => value.ShieldsGranted);
        public int EnemyShieldDamageBlocks => SumEnemy(value => value.ShieldDamageBlocks);
        public int EnemyFurnaceTransfers => SumEnemy(value => value.FurnaceTransfers);
        public int EnemyFlourishGained => SumEnemy(value => value.FlourishGained);

        private IEnumerable<BattleSample> SuccessfulSamples()
        {
            return Batch.Samples.Where(value =>
                value.Succeeded && value.Diagnostics != null);
        }

        private int SumEnemy(Func<BattleSideDiagnostics, int> selector)
        {
            return SuccessfulSamples().Sum(value => selector(value.Diagnostics.Enemy));
        }
    }

    public sealed class ChapterEncounterSamplingRunner
    {
        private static readonly IReadOnlyList<string> DevelopmentLevels =
            new[] { "N", "H" };

        private readonly Func<string, MinionConfig> resolveMinion;

        public ChapterEncounterSamplingRunner(Func<string, MinionConfig> resolveMinion)
        {
            this.resolveMinion = resolveMinion ??
                                 throw new ArgumentNullException(nameof(resolveMinion));
        }

        public IReadOnlyList<ChapterEncounterScenarioResult> Run(
            IEnumerable<ChapterEncounterDefinition> definitions,
            BalanceFixtureCatalog fixtures,
            IEnumerable<int> seeds,
            Action<ChapterEncounterDefinition> encounterStarted = null)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (fixtures == null) throw new ArgumentNullException(nameof(fixtures));
            if (seeds == null) throw new ArgumentNullException(nameof(seeds));

            var materializedDefinitions = definitions.ToList();
            var materializedSeeds = seeds.ToList();
            if (materializedDefinitions.Count == 0)
            {
                throw new ArgumentException(
                    "At least one encounter definition is required.",
                    nameof(definitions));
            }
            if (materializedSeeds.Count == 0 ||
                materializedSeeds.Distinct().Count() != materializedSeeds.Count)
            {
                throw new ArgumentException(
                    "At least one unique battle seed is required.",
                    nameof(seeds));
            }

            var batchRunner = new BattleBatchRunner(resolveMinion);
            var results = new List<ChapterEncounterScenarioResult>();
            foreach (var definition in materializedDefinitions)
            {
                encounterStarted?.Invoke(definition);
                foreach (var developmentLevel in DevelopmentLevels)
                {
                    foreach (var buildId in fixtures.BuildIds)
                    {
                        var board = CreateBoard(
                            definition,
                            fixtures,
                            buildId,
                            developmentLevel);
                        results.Add(new ChapterEncounterScenarioResult(
                            definition,
                            buildId,
                            developmentLevel,
                            batchRunner.Run(board, materializedSeeds)));
                    }
                }
            }

            return results.AsReadOnly();
        }

        public IReadOnlyList<ChapterEncounterScenarioResult> Run(
            IEnumerable<ChapterEncounterDefinition> definitions,
            ChapterProgressFixtureCatalog fixtures,
            IEnumerable<int> seeds,
            Action<ChapterEncounterDefinition> encounterStarted = null)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (fixtures == null) throw new ArgumentNullException(nameof(fixtures));
            if (seeds == null) throw new ArgumentNullException(nameof(seeds));

            var materializedDefinitions = definitions.ToList();
            var materializedSeeds = seeds.ToList();
            ValidateInputs(materializedDefinitions, materializedSeeds);

            var batchRunner = new BattleBatchRunner(resolveMinion);
            var results = new List<ChapterEncounterScenarioResult>();
            foreach (var definition in materializedDefinitions)
            {
                encounterStarted?.Invoke(definition);
                var checkpoint = fixtures.ResolveCheckpoint(
                    definition.Floor,
                    definition.CombatIndex);
                foreach (var buildId in fixtures.BuildIds)
                {
                    var board = CreateBoard(definition, fixtures, buildId);
                    results.Add(new ChapterEncounterScenarioResult(
                        definition,
                        buildId,
                        checkpoint.Stage,
                        batchRunner.Run(board, materializedSeeds),
                        $"{buildId}_{checkpoint.Id}"));
                }
            }

            return results.AsReadOnly();
        }

        public BattleBoardState CreateBoard(
            ChapterEncounterDefinition definition,
            BalanceFixtureCatalog fixtures,
            string buildId,
            string developmentLevel)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (fixtures == null) throw new ArgumentNullException(nameof(fixtures));

            var board = fixtures.CreateFixture(buildId, developmentLevel);
            FillEnemy(board, definition);
            return board;
        }

        public BattleBoardState CreateBoard(
            ChapterEncounterDefinition definition,
            ChapterProgressFixtureCatalog fixtures,
            string buildId)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (fixtures == null) throw new ArgumentNullException(nameof(fixtures));

            var board = fixtures.CreateFixture(
                definition.Floor,
                definition.CombatIndex,
                buildId);
            FillEnemy(board, definition);
            return board;
        }

        private static void ValidateInputs(
            IReadOnlyCollection<ChapterEncounterDefinition> definitions,
            IReadOnlyCollection<int> seeds)
        {
            if (definitions.Count == 0)
            {
                throw new ArgumentException(
                    "At least one encounter definition is required.",
                    nameof(definitions));
            }
            if (seeds.Count == 0 || seeds.Distinct().Count() != seeds.Count)
            {
                throw new ArgumentException(
                    "At least one unique battle seed is required.",
                    nameof(seeds));
            }
        }

        private void FillEnemy(
            BattleBoardState board,
            ChapterEncounterDefinition definition)
        {
            foreach (var slot in definition.Encounter.EnemySlots ??
                                 new List<EnemySlotConfig>())
            {
                if (slot.Slot < 0 || slot.Slot >= BattleBoardState.SlotCount)
                {
                    throw new InvalidOperationException(
                        $"Encounter {definition.EncounterId} has invalid enemy slot " +
                        $"{slot.Slot}.");
                }
                var minion = resolveMinion(slot.MinionId) ??
                             throw new InvalidOperationException(
                                 $"Encounter {definition.EncounterId} references missing minion " +
                                 $"{slot.MinionId}.");
                board.Enemy[slot.Slot] = new BattleMinionRuntime(
                    minion,
                    slot.Golden,
                    permanentAttackBonus: slot.AttackBonus,
                    permanentHealthBonus: slot.HealthBonus,
                    permanentKeywords: slot.PermanentKeywords);
            }
        }
    }

    public sealed class ChapterEncounterAggregate
    {
        public string Source { get; set; }
        public string MapId { get; set; }
        public string MapName { get; set; }
        public int Floor { get; set; }
        public string NodeId { get; set; }
        public string NodeType { get; set; }
        public int CombatIndex { get; set; }
        public string RouteTag { get; set; }
        public string EventId { get; set; }
        public string EventName { get; set; }
        public string EncounterId { get; set; }
        public string EncounterName { get; set; }
        public int ThreatRating { get; set; }
        public string DevelopmentLevel { get; set; }
        public int ScenarioCount { get; set; }
        public int Battles { get; set; }
        public int PlayerWins { get; set; }
        public int EnemyWins { get; set; }
        public int Draws { get; set; }
        public int Exceptions { get; set; }
        public int RoundLimitCount { get; set; }
        public int EffectLimitHitCount { get; set; }
        public double AverageRounds { get; set; }
        public double P90Rounds { get; set; }
        public double AveragePlayerSurvivors { get; set; }
        public double AverageEnemySurvivors { get; set; }
        public int EnemyCleaveHits { get; set; }
        public int EnemySummonAttempts { get; set; }
        public int EnemySummonSuccesses { get; set; }
        public int EnemyShieldsGranted { get; set; }
        public int EnemyShieldDamageBlocks { get; set; }
        public int EnemyFurnaceTransfers { get; set; }
        public int EnemyFlourishGained { get; set; }
        public double BuildScoreSpread { get; set; }
        public string WeakestBuildId { get; set; }
        public string StrongestBuildId { get; set; }

        public double PlayerWinRate => ChapterEncounterStatistics.Rate(
            PlayerWins,
            Battles);
        public double PlayerScoreRate => Battles <= 0
            ? 0d
            : (PlayerWins + Draws * 0.5d) / Battles;
        public double DrawRate => ChapterEncounterStatistics.Rate(Draws, Battles);
        public double RoundLimitRate => ChapterEncounterStatistics.Rate(
            RoundLimitCount,
            Battles);
        public string AggregateId =>
            $"{Source}:{MapId}:{NodeId}:{EncounterId}:{DevelopmentLevel}";

        public static IReadOnlyList<ChapterEncounterAggregate> Build(
            IEnumerable<ChapterEncounterScenarioResult> scenarioResults)
        {
            if (scenarioResults == null)
            {
                throw new ArgumentNullException(nameof(scenarioResults));
            }

            return scenarioResults
                .GroupBy(value => new
                {
                    value.Definition.DefinitionId,
                    value.DevelopmentLevel
                })
                .Select(group => Create(group.AsEnumerable()))
                .OrderBy(value => value.Floor)
                .ThenBy(value => value.CombatIndex)
                .ThenBy(value => value.Source == ChapterEncounterSource.Event ? 1 : 0)
                .ThenBy(value => value.RouteTag)
                .ThenBy(value => value.NodeId)
                .ThenBy(value => value.DevelopmentLevel)
                .ToList()
                .AsReadOnly();
        }

        private static ChapterEncounterAggregate Create(
            IEnumerable<ChapterEncounterScenarioResult> values)
        {
            var rows = values.ToList();
            var first = rows.First();
            var definition = first.Definition;
            var samples = rows.SelectMany(value => value.Batch.Samples).ToList();
            var successfulSamples = samples.Where(value =>
                value.Succeeded && value.Diagnostics != null).ToList();
            var rankedBuilds = rows
                .OrderBy(value => value.Batch.PlayerScoreRate)
                .ThenBy(value => value.BuildId)
                .ToList();

            return new ChapterEncounterAggregate
            {
                Source = definition.Source,
                MapId = definition.MapId,
                MapName = definition.MapName,
                Floor = definition.Floor,
                NodeId = definition.NodeId,
                NodeType = definition.NodeType,
                CombatIndex = definition.CombatIndex,
                RouteTag = definition.RouteTag,
                EventId = definition.EventId,
                EventName = definition.EventName,
                EncounterId = definition.EncounterId,
                EncounterName = definition.EncounterName,
                ThreatRating = definition.ThreatRating,
                DevelopmentLevel = first.DevelopmentLevel,
                ScenarioCount = rows.Count,
                Battles = rows.Sum(value => value.Batch.Battles),
                PlayerWins = rows.Sum(value => value.Batch.PlayerWins),
                EnemyWins = rows.Sum(value => value.Batch.EnemyWins),
                Draws = rows.Sum(value => value.Batch.Draws),
                Exceptions = rows.Sum(value => value.Batch.Exceptions),
                RoundLimitCount = rows.Sum(value => value.RoundLimitCount),
                EffectLimitHitCount = rows.Sum(value => value.EffectLimitHitCount),
                AverageRounds = ChapterEncounterStatistics.Average(
                    successfulSamples.Select(value => value.Diagnostics.RoundCount)),
                P90Rounds = ChapterEncounterStatistics.Percentile(
                    successfulSamples.Select(value => value.Diagnostics.RoundCount),
                    0.90d),
                AveragePlayerSurvivors = ChapterEncounterStatistics.Average(
                    successfulSamples.Select(value => value.PlayerSurvivors)),
                AverageEnemySurvivors = ChapterEncounterStatistics.Average(
                    successfulSamples.Select(value => value.EnemySurvivors)),
                EnemyCleaveHits = rows.Sum(value => value.EnemyCleaveHits),
                EnemySummonAttempts = rows.Sum(value => value.EnemySummonAttempts),
                EnemySummonSuccesses = rows.Sum(value => value.EnemySummonSuccesses),
                EnemyShieldsGranted = rows.Sum(value => value.EnemyShieldsGranted),
                EnemyShieldDamageBlocks = rows.Sum(value =>
                    value.EnemyShieldDamageBlocks),
                EnemyFurnaceTransfers = rows.Sum(value => value.EnemyFurnaceTransfers),
                EnemyFlourishGained = rows.Sum(value => value.EnemyFlourishGained),
                BuildScoreSpread = rankedBuilds.Last().Batch.PlayerScoreRate -
                                   rankedBuilds.First().Batch.PlayerScoreRate,
                WeakestBuildId = rankedBuilds.First().BuildId,
                StrongestBuildId = rankedBuilds.Last().BuildId
            };
        }
    }

    public sealed class ChapterEncounterAnomaly
    {
        public string Severity { get; set; }
        public string Code { get; set; }
        public string Scope { get; set; }
        public int Floor { get; set; }
        public string DevelopmentLevel { get; set; }
        public string EncounterId { get; set; }
        public string Message { get; set; }
        public string Evidence { get; set; }
        public string Recommendation { get; set; }
    }

    public static class ChapterEncounterAnomalyAnalyzer
    {
        private const double ComparisonTolerance = 0.05d;

        public static IReadOnlyList<ChapterEncounterAnomaly> Analyze(
            IEnumerable<ChapterEncounterAggregate> aggregates)
        {
            if (aggregates == null) throw new ArgumentNullException(nameof(aggregates));
            var rows = aggregates.ToList();
            var anomalies = new List<ChapterEncounterAnomaly>();

            AddEncounterSafetyAndSpread(rows, anomalies);
            AddDevelopmentInversions(rows, anomalies);
            AddRouteAnomalies(rows, anomalies);
            AddBranchAnomalies(rows, anomalies);
            AddBossAnomalies(rows, anomalies);
            AddPacingAnomalies(rows, anomalies);
            AddChapterProgressionAnomalies(rows, anomalies);

            return anomalies
                .OrderBy(value => SeverityRank(value.Severity))
                .ThenBy(value => value.Floor)
                .ThenBy(value => value.DevelopmentLevel)
                .ThenBy(value => value.Code)
                .ThenBy(value => value.EncounterId)
                .ToList()
                .AsReadOnly();
        }

        private static void AddEncounterSafetyAndSpread(
            IEnumerable<ChapterEncounterAggregate> rows,
            ICollection<ChapterEncounterAnomaly> anomalies)
        {
            foreach (var row in rows)
            {
                if (row.Exceptions > 0)
                {
                    Add(
                        anomalies,
                        "P0",
                        "SIM_EXCEPTION",
                        row,
                        "批量战斗出现模拟异常。",
                        $"{row.Exceptions}/{row.Battles} 场抛出异常",
                        "先修复异常并重跑；该行胜率不可用于调数。");
                }
                if (row.EffectLimitHitCount > 0)
                {
                    Add(
                        anomalies,
                        "P0",
                        "EFFECT_LIMIT",
                        row,
                        "战斗触发效果队列上限。",
                        $"{row.EffectLimitHitCount}/{row.Battles} 场命中效果上限",
                        "检查亡语、召唤或护盾触发链是否形成非预期循环。");
                }
                if (row.RoundLimitRate > 0.01d)
                {
                    Add(
                        anomalies,
                        "P1",
                        "ROUND_LIMIT",
                        row,
                        "超过 1% 的样本到达回合上限。",
                        $"回合上限率 {Percent(row.RoundLimitRate)}",
                        "检查双方低攻高耐久组合，并降低无结论战斗的持续时间。");
                }
                if (row.BuildScoreSpread >= 0.35d)
                {
                    Add(
                        anomalies,
                        "P1",
                        "BUILD_HARD_COUNTER",
                        row,
                        "同档固定构筑之间存在显著胜率断层。",
                        $"计分胜率跨度 {Percent(row.BuildScoreSpread)}；" +
                        $"最低 {row.WeakestBuildId}，最高 {row.StrongestBuildId}",
                        "检查站位针对、关键词硬克制和单一机制门槛。");
                }
                var highBuildWall =
                    row.DevelopmentLevel == "H" &&
                    row.PlayerWinRate <= 0.02d;
                if (highBuildWall)
                {
                    Add(
                        anomalies,
                        "P1",
                        "HIGH_BUILD_WALL",
                        row,
                        "高成型固定构筑仍几乎无法取胜。",
                        $"H 档胜率 {Percent(row.PlayerWinRate)}",
                        "优先下调该遭遇的绝对数值或机制叠加，再用章节进度快照复测。");
                }
                if (!highBuildWall &&
                    (row.PlayerWinRate <= 0.02d || row.PlayerWinRate >= 0.98d))
                {
                    Add(
                        anomalies,
                        "P2",
                        "SATURATED_RESULT",
                        row,
                        "该档固定构筑的聚合结果接近全胜或全败。",
                        $"胜率 {Percent(row.PlayerWinRate)}",
                        "先结合 N/H 档定位是合理端点还是遭遇缺少区分度。");
                }
            }
        }

        private static void AddDevelopmentInversions(
            IEnumerable<ChapterEncounterAggregate> rows,
            ICollection<ChapterEncounterAnomaly> anomalies)
        {
            foreach (var group in rows.GroupBy(value =>
                         $"{value.Source}|{value.MapId}|{value.NodeId}|{value.EncounterId}"))
            {
                var normal = group.FirstOrDefault(value =>
                    value.DevelopmentLevel == "N");
                var high = group.FirstOrDefault(value =>
                    value.DevelopmentLevel == "H");
                if (normal == null || high == null ||
                    high.PlayerScoreRate + 0.02d >= normal.PlayerScoreRate)
                {
                    continue;
                }

                Add(
                    anomalies,
                    "P1",
                    "DEVELOPMENT_INVERSION",
                    high,
                    "高成型构筑的结果反而弱于普通构筑。",
                    $"N {Percent(normal.PlayerScoreRate)}，" +
                    $"H {Percent(high.PlayerScoreRate)}",
                    "检查金色化、额外成长和机制触发是否改变了站位或召唤顺序。");
            }
        }

        private static void AddRouteAnomalies(
            IEnumerable<ChapterEncounterAggregate> rows,
            ICollection<ChapterEncounterAnomaly> anomalies)
        {
            foreach (var group in rows
                         .Where(value =>
                             value.Source == ChapterEncounterSource.Map &&
                             value.CombatIndex == 4 &&
                             !string.IsNullOrWhiteSpace(value.RouteTag))
                         .GroupBy(value => new
                         {
                             value.Floor,
                             value.DevelopmentLevel
                         }))
            {
                var aggressive = Route(group, "Aggressive");
                var adventure = Route(group, "Adventure");
                var conservative = Route(group, "Conservative");
                if (aggressive == null || adventure == null || conservative == null)
                {
                    continue;
                }

                if (conservative.PlayerScoreRate + ComparisonTolerance <
                    adventure.PlayerScoreRate)
                {
                    AddComparison(
                        anomalies,
                        "P1",
                        "ROUTE_ORDER_INVERSION",
                        conservative,
                        "保守路线比冒险路线更难。",
                        $"保守 {Percent(conservative.PlayerScoreRate)}，" +
                        $"冒险 {Percent(adventure.PlayerScoreRate)}",
                        "下调保守路线敌方强度，或重新标注路线风险与奖励。");
                }
                if (adventure.PlayerScoreRate + ComparisonTolerance <
                    aggressive.PlayerScoreRate)
                {
                    AddComparison(
                        anomalies,
                        "P1",
                        "ROUTE_ORDER_INVERSION",
                        aggressive,
                        "激进路线比冒险路线更容易。",
                        $"计分胜率：激进 {Percent(aggressive.PlayerScoreRate)}，" +
                        $"冒险 {Percent(adventure.PlayerScoreRate)}",
                        "上调激进路线压力，或重新标注路线风险与奖励。");
                }

                var scores = new[]
                {
                    aggressive.PlayerScoreRate,
                    adventure.PlayerScoreRate,
                    conservative.PlayerScoreRate
                };
                var gap = scores.Max() - scores.Min();
                if (gap < 0.05d)
                {
                    AddComparison(
                        anomalies,
                        "P2",
                        "ROUTE_FLAT",
                        adventure,
                        "三条 C4 路线的战斗压力几乎相同。",
                        $"最大计分胜率差 {Percent(gap)}",
                        "拉开敌方数值或机制强度，让风险标签可由实战感知。");
                }
                else if (gap > 0.45d)
                {
                    AddComparison(
                        anomalies,
                        "P2",
                        "ROUTE_CLIFF",
                        adventure,
                        "三条 C4 路线的压力跨度过大。",
                        $"最大计分胜率差 {Percent(gap)}",
                        "检查是否存在单一路线的数值断层或构筑硬克制。");
                }
            }
        }

        private static void AddBranchAnomalies(
            IEnumerable<ChapterEncounterAggregate> rows,
            ICollection<ChapterEncounterAnomaly> anomalies)
        {
            foreach (var group in rows
                         .Where(value =>
                             value.Source == ChapterEncounterSource.Map &&
                             (value.CombatIndex == 2 || value.CombatIndex == 5) &&
                             string.IsNullOrWhiteSpace(value.RouteTag))
                         .GroupBy(value => new
                         {
                             value.Floor,
                             value.CombatIndex,
                             value.DevelopmentLevel
                         }))
            {
                var branchRows = group.OrderBy(value => value.NodeId).ToList();
                if (branchRows.Count != 2) continue;
                var gap = Math.Abs(
                    branchRows[0].PlayerScoreRate -
                    branchRows[1].PlayerScoreRate);
                if (gap < 0.25d) continue;

                AddComparison(
                    anomalies,
                    gap >= 0.50d ? "P1" : "P2",
                    "BRANCH_GAP",
                    branchRows[0],
                    $"C{group.Key.CombatIndex} 二选一战斗存在明显难度差。",
                    $"计分胜率：{branchRows[0].EncounterName} " +
                    $"{Percent(branchRows[0].PlayerScoreRate)}，" +
                    $"{branchRows[1].EncounterName} " +
                    $"{Percent(branchRows[1].PlayerScoreRate)}",
                    "确认该差异是否由奖励补偿；若没有，收敛数值或强化风险提示。");
            }
        }

        private static void AddBossAnomalies(
            IEnumerable<ChapterEncounterAggregate> rows,
            ICollection<ChapterEncounterAnomaly> anomalies)
        {
            foreach (var group in rows
                         .Where(value => value.Source == ChapterEncounterSource.Map)
                         .GroupBy(value => new
                         {
                             value.Floor,
                             value.DevelopmentLevel
                         }))
            {
                var boss = group.SingleOrDefault(value =>
                    string.Equals(value.NodeType, "Boss", StringComparison.OrdinalIgnoreCase));
                var lateRows = group.Where(value => value.CombatIndex == 5).ToList();
                if (boss == null || lateRows.Count == 0) continue;
                var lateScore = lateRows.Average(value => value.PlayerScoreRate);
                if (boss.PlayerScoreRate > lateScore + ComparisonTolerance)
                {
                    AddComparison(
                        anomalies,
                        "P1",
                        "BOSS_EASIER_THAN_C5",
                        boss,
                        "Boss 比同章 C5 终检战斗更容易。",
                        $"计分胜率：Boss {Percent(boss.PlayerScoreRate)}，" +
                        $"C5 均值 {Percent(lateScore)}",
                        "提高 Boss 的终局压力，或下调过强的 C5 分支。");
                }
                if (lateScore - boss.PlayerScoreRate > 0.40d)
                {
                    AddComparison(
                        anomalies,
                        "P1",
                        "BOSS_CLIFF",
                        boss,
                        "Boss 相对 C5 出现过大的难度悬崖。",
                        $"计分胜率：Boss {Percent(boss.PlayerScoreRate)}，" +
                        $"C5 均值 {Percent(lateScore)}",
                        "平滑 C5 到 Boss 的数值跃迁，并复核 Boss 机制叠加。");
                }
            }
        }

        private static void AddPacingAnomalies(
            IEnumerable<ChapterEncounterAggregate> rows,
            ICollection<ChapterEncounterAnomaly> anomalies)
        {
            foreach (var group in rows
                         .Where(value => value.Source == ChapterEncounterSource.Map)
                         .GroupBy(value => new
                         {
                             value.Floor,
                             value.DevelopmentLevel
                         }))
            {
                var buckets = group
                    .GroupBy(value => value.CombatIndex)
                    .Select(value => new
                    {
                        CombatIndex = value.Key,
                        Score = value.Average(row => row.PlayerScoreRate),
                        Anchor = value.First()
                    })
                    .OrderBy(value => value.CombatIndex)
                    .ToList();
                for (var index = 1; index < buckets.Count; index++)
                {
                    var previous = buckets[index - 1];
                    var current = buckets[index];
                    var easing = current.Score - previous.Score;
                    if (easing <= 0.10d) continue;
                    AddComparison(
                        anomalies,
                        easing >= 0.25d ? "P1" : "P2",
                        "PACING_DIP",
                        current.Anchor,
                        $"C{current.CombatIndex} 比 C{previous.CombatIndex} " +
                        "明显更容易。",
                        $"计分胜率：C{previous.CombatIndex} " +
                        $"{Percent(previous.Score)}，" +
                        $"C{current.CombatIndex} {Percent(current.Score)}",
                        "复核遭遇成长曲线，避免章节内出现无提示的压力回落。");
                }
            }
        }

        private static void AddChapterProgressionAnomalies(
            IEnumerable<ChapterEncounterAggregate> rows,
            ICollection<ChapterEncounterAnomaly> anomalies)
        {
            foreach (var group in rows
                         .Where(value => value.Source == ChapterEncounterSource.Map)
                         .GroupBy(value => new
                         {
                             value.CombatIndex,
                             value.DevelopmentLevel
                         }))
            {
                var floors = group
                    .GroupBy(value => value.Floor)
                    .Select(value => new
                    {
                        Floor = value.Key,
                        Score = value.Average(row => row.PlayerScoreRate),
                        Anchor = value.First()
                    })
                    .OrderBy(value => value.Floor)
                    .ToList();
                for (var index = 1; index < floors.Count; index++)
                {
                    var previous = floors[index - 1];
                    var current = floors[index];
                    if (current.Score <= previous.Score + 0.10d) continue;
                    AddComparison(
                        anomalies,
                        "P1",
                        "CHAPTER_REGRESSION",
                        current.Anchor,
                        $"第 {current.Floor} 章 C{group.Key.CombatIndex} " +
                        $"比第 {previous.Floor} 章同阶段更容易。",
                        $"计分胜率：F{previous.Floor} " +
                        $"{Percent(previous.Score)}，" +
                        $"F{current.Floor} {Percent(current.Score)}",
                        "复核跨章节基础数值倍率与该阶段的机制密度。");
                }
            }
        }

        private static ChapterEncounterAggregate Route(
            IEnumerable<ChapterEncounterAggregate> rows,
            string routeTag)
        {
            return rows.SingleOrDefault(value => string.Equals(
                value.RouteTag,
                routeTag,
                StringComparison.OrdinalIgnoreCase));
        }

        private static void Add(
            ICollection<ChapterEncounterAnomaly> anomalies,
            string severity,
            string code,
            ChapterEncounterAggregate row,
            string message,
            string evidence,
            string recommendation)
        {
            anomalies.Add(new ChapterEncounterAnomaly
            {
                Severity = severity,
                Code = code,
                Scope = row.Source == ChapterEncounterSource.Event
                    ? "Event"
                    : row.NodeType,
                Floor = row.Floor,
                DevelopmentLevel = row.DevelopmentLevel,
                EncounterId = row.EncounterId,
                Message = message,
                Evidence = evidence,
                Recommendation = recommendation
            });
        }

        private static void AddComparison(
            ICollection<ChapterEncounterAnomaly> anomalies,
            string severity,
            string code,
            ChapterEncounterAggregate row,
            string message,
            string evidence,
            string recommendation)
        {
            Add(
                anomalies,
                severity,
                code,
                row,
                message,
                evidence,
                recommendation);
        }

        private static string Percent(double value)
        {
            return value.ToString("P1", CultureInfo.InvariantCulture);
        }

        private static int SeverityRank(string severity)
        {
            switch (severity)
            {
                case "P0":
                    return 0;
                case "P1":
                    return 1;
                case "P2":
                    return 2;
                default:
                    return 3;
            }
        }
    }

    public struct ChapterEncounterConfidenceInterval
    {
        public ChapterEncounterConfidenceInterval(double low, double high)
        {
            Low = low;
            High = high;
        }

        public double Low { get; }
        public double High { get; }
    }

    public static class ChapterEncounterStatistics
    {
        public static double Average(IEnumerable<int> values)
        {
            var materialized = (values ?? Enumerable.Empty<int>()).ToList();
            return materialized.Count == 0 ? 0d : materialized.Average();
        }

        public static double Percentile(IEnumerable<int> values, double percentile)
        {
            if (percentile < 0d || percentile > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(percentile));
            }
            var sorted = (values ?? Enumerable.Empty<int>()).OrderBy(value => value)
                .ToList();
            if (sorted.Count == 0) return 0d;
            var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))];
        }

        public static double Rate(int numerator, int denominator)
        {
            return denominator <= 0 ? 0d : (double)numerator / denominator;
        }

        public static ChapterEncounterConfidenceInterval Wilson95(
            int wins,
            int battles)
        {
            if (battles <= 0)
            {
                return new ChapterEncounterConfidenceInterval(0d, 0d);
            }
            var probability = (double)wins / battles;
            const double z = 1.959963984540054d;
            var denominator = 1d + z * z / battles;
            var center = (probability + z * z / (2d * battles)) / denominator;
            var margin = z * Math.Sqrt(
                probability * (1d - probability) / battles +
                z * z / (4d * battles * battles)) / denominator;
            return new ChapterEncounterConfidenceInterval(
                Math.Max(0d, center - margin),
                Math.Min(1d, center + margin));
        }
    }
}

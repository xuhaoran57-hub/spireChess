using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Battle
{
    public sealed class BattleSimulator
    {
        public const int MaxRounds = 30;
        private readonly Random random;
        private readonly Func<string, MinionConfig> resolveMinionConfig;
        private readonly Queue<PendingBattleEffect> effectQueue =
            new Queue<PendingBattleEffect>();
        private readonly Dictionary<string, int> effectUsage =
            new Dictionary<string, int>();
        private readonly Dictionary<string, BattlePermanentDelta> permanentDeltas =
            new Dictionary<string, BattlePermanentDelta>();
        private int processedEffectCount;
        private bool isDrainingEffects;
        private const int MaxEffectEvents = 2048;

        public BattleSimulator()
            : this(new Random(), null)
        {
        }

        public BattleSimulator(Random random)
            : this(random, null)
        {
        }

        public BattleSimulator(Random random, Func<string, MinionConfig> resolveMinionConfig)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.resolveMinionConfig = resolveMinionConfig;
        }

        public BattleSimulationResult Simulate(BattleBoardState initialState)
        {
            return SimulateInternal(initialState, false);
        }

        public BattleSimulationResult SimulatePlayback(BattleBoardState initialState)
        {
            return SimulateInternal(initialState, true);
        }

        private BattleSimulationResult SimulateInternal(BattleBoardState initialState, bool captureSteps)
        {
            var state = initialState.Clone();
            var log = new List<string>();
            var steps = captureSteps ? new List<BattleStep>() : null;
            effectQueue.Clear();
            effectUsage.Clear();
            permanentDeltas.Clear();
            processedEffectCount = 0;
            isDrainingEffects = false;

            AddStep(state, steps, log, new[] { "战斗开始。" });

            var startAttacks = new List<PendingAttack>();
            EnqueueBattleStartEffects(state);
            var battleStartLog = new List<string>();
            DrainEffectQueue(state, battleStartLog, startAttacks);
            AddStep(state, steps, log, battleStartLog);
            foreach (var pendingAttack in startAttacks)
            {
                ResolveAttackStep(
                    state,
                    pendingAttack.Side,
                    pendingAttack.Minion,
                    log,
                    steps,
                    true);
            }

            BattleSide? winner;
            var battleOver = TryGetBattleResult(state, out winner);
            var round = 1;

            while (!battleOver && round <= MaxRounds)
            {
                AddStep(state, steps, log, new[] { $"第 {round} 轮。" });

                var playerActorsThatActed = new HashSet<BattleMinionRuntime>();
                var enemyActorsThatActed = new HashSet<BattleMinionRuntime>();

                while (true)
                {
                    var actionTaken = false;
                    var playerActor = FindNextNormalActor(
                        state.Player,
                        playerActorsThatActed);
                    if (playerActor != null)
                    {
                        playerActorsThatActed.Add(playerActor);
                        ResolveAttackStep(
                            state,
                            BattleSide.Player,
                            playerActor,
                            log,
                            steps);
                        actionTaken = true;
                        battleOver = TryGetBattleResult(state, out winner);
                        if (battleOver)
                        {
                            break;
                        }
                    }

                    var enemyActor = FindNextNormalActor(
                        state.Enemy,
                        enemyActorsThatActed);
                    if (enemyActor != null)
                    {
                        enemyActorsThatActed.Add(enemyActor);
                        ResolveAttackStep(
                            state,
                            BattleSide.Enemy,
                            enemyActor,
                            log,
                            steps);
                        actionTaken = true;
                        battleOver = TryGetBattleResult(state, out winner);
                        if (battleOver)
                        {
                            break;
                        }
                    }

                    if (!actionTaken)
                    {
                        break;
                    }
                }

                round++;
            }

            if (!battleOver)
            {
                AddStep(state, steps, log, new[] { "达到最大回合数，战斗平局。" });
            }
            else if (!winner.HasValue)
            {
                AddStep(state, steps, log, new[] { "双方同时倒下，战斗平局。" });
            }
            else
            {
                AddStep(
                    state,
                    steps,
                    log,
                    new[] { winner == BattleSide.Player ? "玩家胜利。" : "敌方胜利。" },
                    winner: winner);
            }

            var outcomeReason = !battleOver
                ? BattleOutcomeReason.RoundLimit
                : winner.HasValue
                    ? BattleOutcomeReason.Victory
                    : BattleOutcomeReason.MutualElimination;
            EnqueueCombatEndEffects(state, winner);
            DrainEffectQueue(state, log, new List<PendingAttack>());
            return new BattleSimulationResult(
                state,
                winner,
                outcomeReason,
                log,
                steps ?? new List<BattleStep>(),
                permanentDeltas.Values.OrderBy(value => value.SourceInstanceId));
        }

        private void ResolveAttackStep(
            BattleBoardState state,
            BattleSide attackerSide,
            BattleMinionRuntime scheduledAttacker,
            List<string> log,
            List<BattleStep> steps,
            bool isImmediateAttack = false)
        {
            if (scheduledAttacker == null || !scheduledAttacker.IsAlive)
            {
                return;
            }

            var attackerIndex = FindRuntimeIndex(state.GetRow(attackerSide), scheduledAttacker);
            if (attackerIndex < 0)
            {
                return;
            }

            var attackLog = new List<string>();
            var pendingAttacks = new List<PendingAttack>();
            int targetIndex;
            List<int> splashTargetIndexes;
            if (!ResolveAttack(
                    state,
                    attackerSide,
                    attackerIndex,
                    attackLog,
                    out targetIndex,
                    out splashTargetIndexes,
                    pendingAttacks,
                    isImmediateAttack))
            {
                return;
            }

            AddStep(
                state,
                steps,
                log,
                attackLog,
                attackerSide,
                attackerIndex,
                GetOpposingSide(attackerSide),
                targetIndex,
                splashTargetIndexes);

            foreach (var pendingAttack in pendingAttacks)
            {
                ResolveAttackStep(
                    state,
                    pendingAttack.Side,
                    pendingAttack.Minion,
                    log,
                    steps,
                    true);
            }
        }

        private bool ResolveAttack(
            BattleBoardState state,
            BattleSide attackerSide,
            int attackerIndex,
            IList<string> log,
            out int targetIndex,
            out List<int> splashTargetIndexes,
            ICollection<PendingAttack> pendingAttacks,
            bool isImmediateAttack)
        {
            targetIndex = -1;
            splashTargetIndexes = new List<int>();
            var attackers = state.GetRow(attackerSide);
            var attacker = attackers[attackerIndex];
            if (attacker == null || !attacker.IsAlive)
            {
                return false;
            }

            var targets = state.GetOpposingRow(attackerSide);
            targetIndex = SelectTargetIndex(targets);
            if (targetIndex < 0)
            {
                return false;
            }

            var target = targets[targetIndex];
            log.Add(isImmediateAttack
                ? $"{BuildSideName(attackerSide)} {attacker.Name} 立即攻击 {target.Name}。"
                : $"{BuildSideName(attackerSide)} {attacker.Name} 攻击 {target.Name}。");

            EnqueueSourceEffects(
                attacker,
                attackerSide,
                "OnAttackBefore",
                attacker,
                target,
                attackerIndex);
            DrainEffectQueue(state, log, pendingAttacks);
            if (!attacker.IsAlive || !target.IsAlive)
            {
                return true;
            }

            var attackerDamage = attacker.CurrentAttack;
            var counterDamage = target.CurrentAttack;

            var shieldState = state.Player.Concat(state.Enemy)
                .Where(value => value != null)
                .ToDictionary(value => value, value => value.HasShield);

            target.TakeDamage(attackerDamage, log);
            splashTargetIndexes = ResolveCleave(
                attacker,
                targets,
                targetIndex,
                attackerDamage,
                log);
            attacker.TakeDamage(counterDamage, log);

            foreach (var pair in shieldState)
            {
                if (pair.Value && !pair.Key.HasShield)
                {
                    var side = state.Player.Contains(pair.Key)
                        ? BattleSide.Player
                        : BattleSide.Enemy;
                    EnqueueObservedEffects(
                        state,
                        side,
                        "OnShieldLost",
                        pair.Key,
                        ReferenceEquals(pair.Key, target) ? attacker : target);
                }
            }

            var deaths = RemoveDead(
                targets,
                GetOpposingSide(attackerSide),
                log);
            deaths.AddRange(RemoveDead(attackers, attackerSide, log));
            ResolveDeathEffects(state, deaths, log, pendingAttacks);
            if (deaths.Any(value => ReferenceEquals(value.Minion, target)) && attacker.IsAlive)
            {
                EnqueueSourceEffects(
                    attacker,
                    attackerSide,
                    "OnKill",
                    target,
                    target,
                    attackerIndex);
            }

            DrainEffectQueue(state, log, pendingAttacks);
            return true;
        }

        private int SelectTargetIndex(IReadOnlyList<BattleMinionRuntime> targets)
        {
            var candidates = FindAliveIndexes(targets, minion => minion.HasTaunt);
            if (candidates.Count == 0)
            {
                candidates = FindAliveIndexes(targets, minion => true);
            }

            if (candidates.Count == 0)
            {
                return -1;
            }

            return candidates.Count == 1
                ? candidates[0]
                : candidates[random.Next(candidates.Count)];
        }

        private static List<int> FindAliveIndexes(
            IReadOnlyList<BattleMinionRuntime> row,
            System.Func<BattleMinionRuntime, bool> predicate)
        {
            var indexes = new List<int>();
            for (var i = 0; i < row.Count; i++)
            {
                var minion = row[i];
                if (minion != null && minion.IsAlive && predicate(minion))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private List<int> ResolveCleave(
            BattleMinionRuntime attacker,
            IList<BattleMinionRuntime> targets,
            int targetIndex,
            int attackDamage,
            IList<string> log)
        {
            var splashTargetIndexes = new List<int>();
            if (!attacker.HasCleave || attackDamage <= 0)
            {
                return splashTargetIndexes;
            }

            int leftDamage;
            int rightDamage;
            if (attacker.IsGolden)
            {
                leftDamage = attackDamage;
                rightDamage = attackDamage;
            }
            else
            {
                leftDamage = attackDamage / 2;
                rightDamage = attackDamage / 2;
                if (attackDamage % 2 != 0)
                {
                    if (random.Next(2) == 0)
                    {
                        leftDamage++;
                    }
                    else
                    {
                        rightDamage++;
                    }
                }
            }

            ApplyCleaveDamage(
                attacker,
                targets,
                targetIndex - 1,
                leftDamage,
                splashTargetIndexes,
                log);
            ApplyCleaveDamage(
                attacker,
                targets,
                targetIndex + 1,
                rightDamage,
                splashTargetIndexes,
                log);
            return splashTargetIndexes;
        }

        private static void ApplyCleaveDamage(
            BattleMinionRuntime attacker,
            IList<BattleMinionRuntime> targets,
            int targetIndex,
            int damage,
            ICollection<int> splashTargetIndexes,
            IList<string> log)
        {
            if (targetIndex < 0 || targetIndex >= targets.Count || damage <= 0)
            {
                return;
            }

            var target = targets[targetIndex];
            if (target == null || !target.IsAlive)
            {
                return;
            }

            splashTargetIndexes.Add(targetIndex);
            log.Add($"{attacker.Name} 的溅射对 {target.Name} 造成 {damage} 点伤害。");
            target.TakeDamage(damage, log);
        }

        private void ResolveDeathEffects(
            BattleBoardState state,
            IEnumerable<DeathRecord> deaths,
            IList<string> log,
            ICollection<PendingAttack> pendingAttacks)
        {
            var materialized = deaths.ToList();
            foreach (var death in materialized)
            {
                EnqueueSourceEffects(
                    death.Minion,
                    death.Side,
                    "OnDeath",
                    death.Minion,
                    null,
                    death.OriginalIndex,
                    death);
                EnqueueObservedEffects(
                    state,
                    death.Side,
                    "OnFriendlyDeath",
                    death.Minion,
                    null);
                if (death.Minion.Config.IsToken)
                {
                    EnqueueObservedEffects(
                        state,
                        death.Side,
                        "OnSummonedUnitDeath",
                        death.Minion,
                        null);
                }
            }

            DrainEffectQueue(state, log, pendingAttacks);
        }

        private void EnqueueBattleStartEffects(BattleBoardState state)
        {
            foreach (var scheduled in state.BattleStartEffects)
            {
                if (scheduled?.Effect == null)
                {
                    continue;
                }

                var row = state.GetRow(scheduled.Side);
                var sourceIndex = row.FindIndex(minion => minion != null && minion.IsAlive);
                if (sourceIndex < 0)
                {
                    continue;
                }

                var source = row[sourceIndex];
                effectQueue.Enqueue(new PendingBattleEffect(
                    source,
                    scheduled.Side,
                    sourceIndex,
                    source,
                    null,
                    scheduled.Effect,
                    null,
                    null));
            }

            for (var i = 0; i < BattleBoardState.SlotCount; i++)
            {
                var player = state.Player[i];
                if (player != null)
                {
                    EnqueueSourceEffects(
                        player,
                        BattleSide.Player,
                        "OnBattleStart",
                        player,
                        null,
                        i);
                }
            }

            for (var i = 0; i < BattleBoardState.SlotCount; i++)
            {
                var enemy = state.Enemy[i];
                if (enemy != null)
                {
                    EnqueueSourceEffects(
                        enemy,
                        BattleSide.Enemy,
                        "OnBattleStart",
                        enemy,
                        null,
                        i);
                }
            }
        }

        private void EnqueueCombatEndEffects(BattleBoardState state, BattleSide? winner)
        {
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            {
                var row = state.GetRow(side);
                for (var i = 0; i < row.Count; i++)
                {
                    if (row[i] != null)
                    {
                        EnqueueSourceEffects(
                            row[i],
                            side,
                            "OnCombatEnd",
                            row[i],
                            null,
                            i,
                            null,
                            winner);
                    }
                }
            }
        }

        private void EnqueueObservedEffects(
            BattleBoardState state,
            BattleSide side,
            string trigger,
            BattleMinionRuntime subject,
            BattleMinionRuntime related,
            BattleMinionRuntime excludedSource = null)
        {
            var row = state.GetRow(side);
            for (var i = 0; i < row.Count; i++)
            {
                var source = row[i];
                if (source == null || ReferenceEquals(source, excludedSource))
                {
                    continue;
                }

                EnqueueSourceEffects(source, side, trigger, subject, related, i);
            }
        }

        private void EnqueueSourceEffects(
            BattleMinionRuntime source,
            BattleSide side,
            string trigger,
            BattleMinionRuntime subject,
            BattleMinionRuntime related,
            int sourceIndex,
            DeathRecord death = null,
            BattleSide? winner = null)
        {
            var effects = source.IsGolden
                ? source.Config.GoldenEffects
                : source.Config.Effects;
            foreach (var effect in effects ?? Enumerable.Empty<EffectConfig>())
            {
                if (effect == null || effect.Trigger != trigger)
                {
                    continue;
                }

                effectQueue.Enqueue(new PendingBattleEffect(
                    source,
                    side,
                    sourceIndex,
                    subject,
                    related,
                    effect,
                    death,
                    winner));
            }
        }

        private void DrainEffectQueue(
            BattleBoardState state,
            IList<string> log,
            ICollection<PendingAttack> pendingAttacks)
        {
            if (isDrainingEffects)
            {
                return;
            }

            isDrainingEffects = true;
            try
            {
                while (effectQueue.Count > 0)
                {
                    if (++processedEffectCount > MaxEffectEvents)
                    {
                        effectQueue.Clear();
                        log.Add("效果队列超过 2048 个事件，已终止后续效果结算。");
                        break;
                    }

                    var pending = effectQueue.Dequeue();
                    if (!CanExecuteEffect(state, pending))
                    {
                        continue;
                    }

                    ExecuteEffect(state, pending, log, pendingAttacks);
                    RecordEffectUse(pending);
                }
            }
            finally
            {
                isDrainingEffects = false;
            }
        }

        private bool CanExecuteEffect(
            BattleBoardState state,
            PendingBattleEffect pending)
        {
            var limit = pending.Effect.Limit?.PerCombat ?? 0;
            if (limit > 0 && GetEffectUseCount(pending) >= limit)
            {
                return false;
            }

            var condition = pending.Effect.Condition;
            if (condition == null || string.IsNullOrWhiteSpace(condition.Type) ||
                condition.Type == "None")
            {
                return true;
            }

            switch (condition.Type)
            {
                case "HasShield":
                    return pending.Source.HasShield;
                case "IsGolden":
                    return pending.Source.IsGolden;
                case "SubjectIsToken":
                    return pending.Subject?.Config.IsToken == true;
                case "SubjectIsNonToken":
                    return pending.Subject != null && !pending.Subject.Config.IsToken &&
                        (string.IsNullOrWhiteSpace(condition.Race) ||
                         pending.Subject.Config.Race == condition.Race);
                case "SubjectRace":
                    return pending.Subject?.Config.Race == condition.Race;
                case "AttackerExists":
                    return pending.Related != null && pending.Related.IsAlive;
                case "CombatWon":
                    return pending.Winner == pending.Side;
                case "SubjectAdjacent":
                    var subjectIndex = FindRuntimeIndex(
                        state.GetRow(pending.Side),
                        pending.Subject);
                    return subjectIndex >= 0 &&
                        Math.Abs(subjectIndex - pending.SourceIndex) == 1;
                case "AttackBelowHealth":
                    return pending.Source.CurrentAttack < pending.Source.CurrentHealth;
                default:
                    return false;
            }
        }

        private void ExecuteEffect(
            BattleBoardState state,
            PendingBattleEffect pending,
            IList<string> log,
            ICollection<PendingAttack> pendingAttacks)
        {
            var effect = pending.Effect;
            if (effect.Action == "SummonToken")
            {
                var death = pending.Death ?? new DeathRecord(
                    pending.Side,
                    pending.SourceIndex,
                    pending.Source);
                ResolveSummonToken(state, death, effect, log, pendingAttacks);
                return;
            }

            if (effect.Action == "ImmediateAttack")
            {
                var actor = pending.Subject ?? pending.Source;
                if (actor != null && actor.IsAlive)
                {
                    pendingAttacks.Add(new PendingAttack(pending.Side, actor));
                }
                return;
            }

            var targets = ResolveBattleTargets(state, pending);
            foreach (var target in targets)
            {
                switch (effect.Action)
                {
                    case "ModifyStats":
                        var attack = effect.Value?.Resource == "SubjectAttack"
                            ? Math.Max(0, pending.Subject?.CurrentAttack ?? 0)
                            : effect.Value?.Attack ?? 0;
                        var health = effect.Value?.Health ?? 0;
                        target.AddTemporaryStats(
                            attack,
                            health,
                            log);
                        if (effect.Value?.Duration == "Permanent")
                        {
                            AddPermanentDelta(
                                pending.Side,
                                target,
                                attack,
                                health,
                                null);
                        }
                        break;
                    case "AddShield":
                        if (target.TryAddShield(log))
                        {
                            var side = GetRuntimeSide(state, target, pending.Side);
                            EnqueueObservedEffects(
                                state,
                                side,
                                "OnShieldGained",
                                target,
                                pending.Source);
                        }
                        break;
                    case "RemoveShield":
                        if (target.TryRemoveShield(log))
                        {
                            var side = GetRuntimeSide(state, target, pending.Side);
                            EnqueueObservedEffects(
                                state,
                                side,
                                "OnShieldLost",
                                target,
                                pending.Source);
                        }
                        break;
                    case "AddKeyword":
                        if (target.TryAddKeyword(effect.Value?.Keyword, log) &&
                            effect.Value?.Duration == "Permanent")
                        {
                            AddPermanentDelta(
                                pending.Side,
                                target,
                                0,
                                0,
                                effect.Value.Keyword);
                        }
                        break;
                    case "DealDamage":
                        ApplyEffectDamage(
                            state,
                            pending,
                            target,
                            Math.Max(0, effect.Value?.Amount ?? 0),
                            log,
                            pendingAttacks);
                        break;
                }
            }
        }

        private IReadOnlyList<BattleMinionRuntime> ResolveBattleTargets(
            BattleBoardState state,
            PendingBattleEffect pending)
        {
            var target = pending.Effect.Target;
            if (target == null)
            {
                return new[] { pending.Source };
            }

            if (target.Scope == "Self")
            {
                return pending.Source.IsAlive
                    ? new[] { pending.Source }
                    : Array.Empty<BattleMinionRuntime>();
            }

            if (target.Scope == "EventSubject")
            {
                return IsValidEffectTarget(pending.Subject, target)
                    ? new[] { pending.Subject }
                    : Array.Empty<BattleMinionRuntime>();
            }

            if (target.Scope == "Attacker")
            {
                return IsValidEffectTarget(pending.Related, target)
                    ? new[] { pending.Related }
                    : Array.Empty<BattleMinionRuntime>();
            }

            if (target.Scope == "Related")
            {
                return IsValidEffectTarget(pending.Related, target)
                    ? new[] { pending.Related }
                    : Array.Empty<BattleMinionRuntime>();
            }

            if (target.Scope == "RelatedAdjacent")
            {
                var relatedSide = GetOpposingSide(pending.Side);
                var relatedRow = state.GetRow(relatedSide);
                var relatedIndex = FindRuntimeIndex(relatedRow, pending.Related);
                if (relatedIndex < 0)
                {
                    return Array.Empty<BattleMinionRuntime>();
                }

                return relatedRow
                    .Select((value, index) => new { value, index })
                    .Where(entry => Math.Abs(entry.index - relatedIndex) <= 1 &&
                        IsValidEffectTarget(entry.value, target) && entry.value.HasShield)
                    .Select(entry => entry.value)
                    .ToList();
            }

            var targetSide = target.Side == "Enemy"
                ? GetOpposingSide(pending.Side)
                : pending.Side;
            var row = state.GetRow(targetSide);
            string resolvedSpecialRace = null;
            if (target.Race == "MostCommonAtLeast3")
            {
                var group = row.Where(value => value != null && value.IsAlive)
                    .GroupBy(value => value.Config.Race)
                    .OrderByDescending(value => value.Count())
                    .ThenBy(value => value.Key)
                    .FirstOrDefault();
                if (group == null || group.Count() < 3)
                {
                    return Array.Empty<BattleMinionRuntime>();
                }

                resolvedSpecialRace = group.Key;
            }
            var indexed = row
                .Select((value, index) => new { value, index })
                .Where(entry => IsValidEffectTarget(entry.value, target) ||
                    (resolvedSpecialRace != null && entry.value != null &&
                     entry.value.IsAlive && entry.value.Config.Race == resolvedSpecialRace &&
                     (target.IncludeToken || !entry.value.Config.IsToken)))
                .ToList();
            if (target.Selector == "NoShieldRandom")
            {
                indexed = indexed.Where(entry => !entry.value.HasShield).ToList();
            }
            else if (target.Selector == "Shielded")
            {
                indexed = indexed.Where(entry => entry.value.HasShield).ToList();
            }

            if (target.Scope == "All")
            {
                return indexed.Select(entry => entry.value).ToList();
            }

            if (target.Scope == "Left" || target.Scope == "Right" ||
                target.Scope == "Adjacent")
            {
                return indexed.Where(entry =>
                        (target.Scope == "Left" && entry.index == pending.SourceIndex - 1) ||
                        (target.Scope == "Right" && entry.index == pending.SourceIndex + 1) ||
                        (target.Scope == "Adjacent" &&
                         Math.Abs(entry.index - pending.SourceIndex) == 1))
                    .Select(entry => entry.value).ToList();
            }

            if (indexed.Count == 0)
            {
                return Array.Empty<BattleMinionRuntime>();
            }

            switch (target.Selector)
            {
                case "Random":
                case "NoShieldRandom":
                case "Shielded":
                    var count = target.MaxTargets > 0
                        ? Math.Min(target.MaxTargets, indexed.Count)
                        : 1;
                    var chosen = new List<BattleMinionRuntime>();
                    var available = indexed.ToList();
                    while (chosen.Count < count)
                    {
                        var selection = random.Next(available.Count);
                        chosen.Add(available[selection].value);
                        available.RemoveAt(selection);
                    }
                    return chosen;
                case "LowestAttack":
                    return indexed.OrderBy(entry => entry.value.CurrentAttack)
                        .ThenBy(entry => entry.index)
                        .Take(target.MaxTargets > 0 ? target.MaxTargets : 1)
                        .Select(entry => entry.value).ToList();
                case "LowestHealth":
                    return new[] { indexed.OrderBy(entry => entry.value.CurrentHealth)
                        .ThenBy(entry => entry.index).First().value };
                case "Leftmost":
                    return new[] { indexed.OrderBy(entry => entry.index).First().value };
                case "Rightmost":
                    return new[] { indexed.OrderByDescending(entry => entry.index).First().value };
                default:
                    return indexed.Take(Math.Max(1, target.MaxTargets)).Select(entry => entry.value).ToList();
            }
        }

        private void ApplyEffectDamage(
            BattleBoardState state,
            PendingBattleEffect pending,
            BattleMinionRuntime target,
            int amount,
            IList<string> log,
            ICollection<PendingAttack> pendingAttacks)
        {
            if (target == null || amount <= 0 || !target.IsAlive)
            {
                return;
            }

            var hadShield = target.HasShield;
            target.TakeDamage(amount, log);
            var targetSide = GetRuntimeSide(state, target, GetOpposingSide(pending.Side));
            if (hadShield && !target.HasShield)
            {
                EnqueueObservedEffects(
                    state,
                    targetSide,
                    "OnShieldLost",
                    target,
                    pending.Source);
            }

            var deaths = RemoveDead(state.GetRow(targetSide), targetSide, log);
            if (deaths.Count > 0)
            {
                ResolveDeathEffects(state, deaths, log, pendingAttacks);
            }
        }

        private void AddPermanentDelta(
            BattleSide effectSide,
            BattleMinionRuntime target,
            int attack,
            int health,
            string keyword)
        {
            if (effectSide != BattleSide.Player || target == null ||
                target.Config.IsToken || string.IsNullOrWhiteSpace(target.SourceInstanceId))
            {
                return;
            }

            if (!permanentDeltas.TryGetValue(target.SourceInstanceId, out var delta))
            {
                delta = new BattlePermanentDelta(target.SourceInstanceId);
                permanentDeltas.Add(target.SourceInstanceId, delta);
            }

            delta.Add(attack, health, keyword);
        }

        private static BattleSide GetRuntimeSide(
            BattleBoardState state,
            BattleMinionRuntime runtime,
            BattleSide fallback)
        {
            if (state.Player.Contains(runtime))
            {
                return BattleSide.Player;
            }

            if (state.Enemy.Contains(runtime))
            {
                return BattleSide.Enemy;
            }

            return fallback;
        }

        private int GetEffectUseCount(PendingBattleEffect pending)
        {
            effectUsage.TryGetValue(BuildEffectUsageKey(pending), out var count);
            return count;
        }

        private void RecordEffectUse(PendingBattleEffect pending)
        {
            var key = BuildEffectUsageKey(pending);
            effectUsage[key] = GetEffectUseCount(pending) + 1;
        }

        private static string BuildEffectUsageKey(PendingBattleEffect pending)
        {
            return $"{pending.Source.GetHashCode()}:{pending.Effect.Id}";
        }

        private void ResolveSummonToken(
            BattleBoardState state,
            DeathRecord death,
            EffectConfig effect,
            IList<string> log,
            ICollection<PendingAttack> pendingAttacks)
        {
            var resourceId = effect.Value?.Resource;
            var tokenConfig = string.IsNullOrWhiteSpace(resourceId)
                ? null
                : resolveMinionConfig?.Invoke(resourceId);
            if (tokenConfig == null || !tokenConfig.IsToken)
            {
                log.Add($"{death.Minion.Name} 的召唤失败：找不到 Token 配置 {resourceId}。");
                return;
            }

            var amount = Math.Max(1, effect.Value?.Amount ?? 1);
            var row = state.GetRow(death.Side);
            for (var summonIndex = 0; summonIndex < amount; summonIndex++)
            {
                var slotIndex = FindSummonSlot(row, death.OriginalIndex);
                if (slotIndex < 0)
                {
                    log.Add($"{death.Minion.Name} 召唤 {tokenConfig.Name} 失败：没有空位。");
                    ResolveFallbackEffects(row, effect.FallbackEffects, log);
                    continue;
                }

                var attackOverride = effect.Value != null && effect.Value.Attack > 0
                    ? (int?)effect.Value.Attack
                    : null;
                var healthOverride = effect.Value != null && effect.Value.Health > 0
                    ? (int?)effect.Value.Health
                    : null;
                var token = new BattleMinionRuntime(
                    tokenConfig,
                    false,
                    attackOverride,
                    healthOverride);
                row[slotIndex] = token;
                log.Add($"{death.Minion.Name} 在 {slotIndex + 1} 号位召唤了 {token.Name}。");
                EnqueueSourceEffects(
                    token,
                    death.Side,
                    "OnSummon",
                    token,
                    death.Minion,
                    slotIndex);
                EnqueueObservedEffects(
                    state,
                    death.Side,
                    "OnSummon",
                    token,
                    death.Minion,
                    token);
            }
        }

        private void ResolveFallbackEffects(
            IReadOnlyList<BattleMinionRuntime> row,
            IEnumerable<EffectConfig> effects,
            IList<string> log)
        {
            foreach (var effect in effects ?? Enumerable.Empty<EffectConfig>())
            {
                if (effect.Action != "ModifyStats" || effect.Value == null)
                {
                    continue;
                }

                var candidates = FindAliveIndexes(
                    row,
                    minion => IsValidEffectTarget(minion, effect.Target));
                if (candidates.Count == 0)
                {
                    log.Add("召唤失败补偿未生效：没有合法目标。");
                    continue;
                }

                var targetIndex = candidates.Count == 1
                    ? candidates[0]
                    : candidates[random.Next(candidates.Count)];
                row[targetIndex].AddTemporaryStats(
                    effect.Value.Attack,
                    effect.Value.Health,
                    log);
            }
        }

        private static bool IsValidEffectTarget(
            BattleMinionRuntime minion,
            TargetConfig target)
        {
            if (minion == null || !minion.IsAlive)
            {
                return false;
            }

            if (target == null)
            {
                return true;
            }

            if (!target.IncludeToken && minion.Config.IsToken)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(target.Race) || minion.Config.Race == target.Race;
        }

        private static bool HasImmediateAttackOnSummon(BattleMinionRuntime minion)
        {
            return (minion.Config.Effects ?? new List<EffectConfig>())
                .Any(effect => effect.Trigger == "OnSummon" && effect.Action == "ImmediateAttack");
        }

        private static int FindSummonSlot(
            IReadOnlyList<BattleMinionRuntime> row,
            int originalIndex)
        {
            if (originalIndex >= 0 && originalIndex < row.Count && row[originalIndex] == null)
            {
                return originalIndex;
            }

            for (var distance = 1; distance < row.Count; distance++)
            {
                var rightIndex = originalIndex + distance;
                if (rightIndex < row.Count && row[rightIndex] == null)
                {
                    return rightIndex;
                }

                var leftIndex = originalIndex - distance;
                if (leftIndex >= 0 && row[leftIndex] == null)
                {
                    return leftIndex;
                }
            }

            return -1;
        }

        private static List<DeathRecord> RemoveDead(
            IList<BattleMinionRuntime> row,
            BattleSide side,
            IList<string> log)
        {
            var deaths = new List<DeathRecord>();
            for (var i = 0; i < row.Count; i++)
            {
                if (row[i] == null || row[i].IsAlive)
                {
                    continue;
                }

                var minion = row[i];
                log.Add($"{minion.Name} 从 {i + 1} 号位移除。");
                deaths.Add(new DeathRecord(side, i, minion));
                row[i] = null;
            }

            return deaths;
        }

        private static BattleMinionRuntime FindNextNormalActor(
            IReadOnlyList<BattleMinionRuntime> row,
            ISet<BattleMinionRuntime> actorsThatActed)
        {
            for (var i = 0; i < row.Count; i++)
            {
                var minion = row[i];
                if (minion != null && minion.IsAlive && !actorsThatActed.Contains(minion))
                {
                    return minion;
                }
            }

            return null;
        }

        private static int FindRuntimeIndex(
            IReadOnlyList<BattleMinionRuntime> row,
            BattleMinionRuntime minion)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (ReferenceEquals(row[i], minion))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryGetBattleResult(BattleBoardState state, out BattleSide? winner)
        {
            var playerAlive = state.HasAlive(BattleSide.Player);
            var enemyAlive = state.HasAlive(BattleSide.Enemy);

            if (playerAlive && enemyAlive)
            {
                winner = null;
                return false;
            }

            if (playerAlive)
            {
                winner = BattleSide.Player;
                return true;
            }

            if (enemyAlive)
            {
                winner = BattleSide.Enemy;
                return true;
            }

            winner = null;
            return true;
        }

        private static void AddStep(
            BattleBoardState state,
            List<BattleStep> steps,
            List<string> log,
            IEnumerable<string> messages,
            BattleSide? attackerSide = null,
            int attackerIndex = -1,
            BattleSide? targetSide = null,
            int targetIndex = -1,
            IEnumerable<int> splashTargetIndexes = null,
            BattleSide? winner = null)
        {
            var messageList = messages as IList<string> ?? messages.ToList();
            if (messageList.Count == 0)
            {
                return;
            }

            log.AddRange(messageList);
            if (steps == null)
            {
                return;
            }

            steps.Add(new BattleStep(
                state.Clone(),
                messageList,
                attackerSide,
                attackerIndex,
                targetSide,
                targetIndex,
                splashTargetIndexes,
                winner));
        }

        private static BattleSide GetOpposingSide(BattleSide side)
        {
            return side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
        }

        private static string BuildSideName(BattleSide side)
        {
            return side == BattleSide.Player ? "玩家" : "敌方";
        }

        private sealed class DeathRecord
        {
            public DeathRecord(BattleSide side, int originalIndex, BattleMinionRuntime minion)
            {
                Side = side;
                OriginalIndex = originalIndex;
                Minion = minion;
            }

            public BattleSide Side { get; }
            public int OriginalIndex { get; }
            public BattleMinionRuntime Minion { get; }
        }

        private sealed class PendingAttack
        {
            public PendingAttack(BattleSide side, BattleMinionRuntime minion)
            {
                Side = side;
                Minion = minion;
            }

            public BattleSide Side { get; }
            public BattleMinionRuntime Minion { get; }
        }

        private sealed class PendingBattleEffect
        {
            public PendingBattleEffect(
                BattleMinionRuntime source,
                BattleSide side,
                int sourceIndex,
                BattleMinionRuntime subject,
                BattleMinionRuntime related,
                EffectConfig effect,
                DeathRecord death,
                BattleSide? winner)
            {
                Source = source;
                Side = side;
                SourceIndex = sourceIndex;
                Subject = subject;
                Related = related;
                Effect = effect;
                Death = death;
                Winner = winner;
            }

            public BattleMinionRuntime Source { get; }
            public BattleSide Side { get; }
            public int SourceIndex { get; }
            public BattleMinionRuntime Subject { get; }
            public BattleMinionRuntime Related { get; }
            public EffectConfig Effect { get; }
            public DeathRecord Death { get; }
            public BattleSide? Winner { get; }
        }
    }
}

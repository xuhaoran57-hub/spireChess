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

            AddStep(state, steps, log, new[] { "战斗开始。" });

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
            return new BattleSimulationResult(
                state,
                winner,
                outcomeReason,
                log,
                steps ?? new List<BattleStep>());
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

            var attackerDamage = attacker.CurrentAttack;
            var counterDamage = target.CurrentAttack;

            target.TakeDamage(attackerDamage, log);
            splashTargetIndexes = ResolveCleave(
                attacker,
                targets,
                targetIndex,
                attackerDamage,
                log);
            attacker.TakeDamage(counterDamage, log);

            var deaths = RemoveDead(
                targets,
                GetOpposingSide(attackerSide),
                log);
            deaths.AddRange(RemoveDead(attackers, attackerSide, log));
            ResolveDeathEffects(state, deaths, log, pendingAttacks);
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
            foreach (var death in deaths)
            {
                var effects = death.Minion.IsGolden
                    ? death.Minion.Config.GoldenEffects
                    : death.Minion.Config.Effects;
                foreach (var effect in effects ?? Enumerable.Empty<EffectConfig>())
                {
                    if (effect.Trigger != "OnDeath" || effect.Action != "SummonToken")
                    {
                        continue;
                    }

                    ResolveSummonToken(
                        state,
                        death,
                        effect,
                        log,
                        pendingAttacks);
                }
            }
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

                if (HasImmediateAttackOnSummon(token))
                {
                    pendingAttacks.Add(new PendingAttack(death.Side, token));
                }
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
    }
}

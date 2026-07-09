using System.Collections.Generic;
using System.Linq;

namespace SpireChess.Battle
{
    public sealed class BattleSimulator
    {
        private const int MaxRounds = 30;

        public BattleSimulationResult Simulate(BattleBoardState initialState)
        {
            return SimulateInternal(initialState, false);
        }

        public BattleSimulationResult SimulatePlayback(BattleBoardState initialState)
        {
            return SimulateInternal(initialState, true);
        }

        private static BattleSimulationResult SimulateInternal(BattleBoardState initialState, bool captureSteps)
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

                for (var slotIndex = 0; slotIndex < BattleBoardState.SlotCount; slotIndex++)
                {
                    ResolveAttackStep(
                        state,
                        BattleSide.Player,
                        slotIndex,
                        log,
                        steps);
                    battleOver = TryGetBattleResult(state, out winner);
                    if (battleOver)
                    {
                        break;
                    }

                    ResolveAttackStep(
                        state,
                        BattleSide.Enemy,
                        slotIndex,
                        log,
                        steps);
                    battleOver = TryGetBattleResult(state, out winner);
                    if (battleOver)
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

            return new BattleSimulationResult(state, winner, log, steps ?? new List<BattleStep>());
        }

        private static void ResolveAttackStep(
            BattleBoardState state,
            BattleSide attackerSide,
            int attackerIndex,
            List<string> log,
            List<BattleStep> steps)
        {
            var attackLog = new List<string>();
            int targetIndex;
            if (!ResolveAttack(state, attackerSide, attackerIndex, attackLog, out targetIndex))
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
                targetIndex);
        }

        private static bool ResolveAttack(
            BattleBoardState state,
            BattleSide attackerSide,
            int attackerIndex,
            IList<string> log,
            out int targetIndex)
        {
            targetIndex = -1;
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
            log.Add($"{BuildSideName(attackerSide)} {attacker.Name} 攻击 {target.Name}。");

            var attackerDamage = attacker.CurrentAttack;
            var counterDamage = target.CurrentAttack;

            target.TakeDamage(attackerDamage, log);
            attacker.TakeDamage(counterDamage, log);

            RemoveDead(attackers, log);
            RemoveDead(targets, log);
            return true;
        }

        private static int SelectTargetIndex(IReadOnlyList<BattleMinionRuntime> targets)
        {
            var tauntIndex = FirstAliveIndex(targets, minion => minion.HasTaunt);
            if (tauntIndex >= 0)
            {
                return tauntIndex;
            }

            return FirstAliveIndex(targets, minion => true);
        }

        private static int FirstAliveIndex(
            IReadOnlyList<BattleMinionRuntime> row,
            System.Func<BattleMinionRuntime, bool> predicate)
        {
            for (var i = 0; i < row.Count; i++)
            {
                var minion = row[i];
                if (minion != null && minion.IsAlive && predicate(minion))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void RemoveDead(IList<BattleMinionRuntime> row, IList<string> log)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (row[i] != null && !row[i].IsAlive)
                {
                    log.Add($"{row[i].Name} 从 {i + 1} 号位移除。");
                    row[i] = null;
                }
            }
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
    }
}

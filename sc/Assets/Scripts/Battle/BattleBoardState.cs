using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Battle
{
    public sealed class BattleRuleModifiers
    {
        public int PlayerExtraDeathrattleTriggers { get; set; }
        public int PlayerFirstNonTokenDeathSummonCount { get; set; }
        public string PlayerFirstNonTokenDeathTokenId { get; set; }
        public int PlayerFirstNonTokenDeathTokenAttack { get; set; }
        public int PlayerFirstNonTokenDeathTokenHealth { get; set; }
        public int PlayerBattleStartShieldTargets { get; set; }
        public int PlayerDistinctRaceStatBonus { get; set; }

        public BattleRuleModifiers Clone()
        {
            return new BattleRuleModifiers
            {
                PlayerExtraDeathrattleTriggers = PlayerExtraDeathrattleTriggers,
                PlayerFirstNonTokenDeathSummonCount = PlayerFirstNonTokenDeathSummonCount,
                PlayerFirstNonTokenDeathTokenId = PlayerFirstNonTokenDeathTokenId,
                PlayerFirstNonTokenDeathTokenAttack = PlayerFirstNonTokenDeathTokenAttack,
                PlayerFirstNonTokenDeathTokenHealth = PlayerFirstNonTokenDeathTokenHealth,
                PlayerBattleStartShieldTargets = PlayerBattleStartShieldTargets,
                PlayerDistinctRaceStatBonus = PlayerDistinctRaceStatBonus
            };
        }
    }

    public sealed class BattleStartEffectState
    {
        public BattleStartEffectState(BattleSide side, EffectConfig effect)
        {
            Side = side;
            Effect = effect;
        }

        public BattleSide Side { get; }
        public EffectConfig Effect { get; }
    }

    public sealed class BattleBoardState
    {
        public const int SlotCount = 5;

        public BattleBoardState()
        {
            Player = new List<BattleMinionRuntime>(new BattleMinionRuntime[SlotCount]);
            Enemy = new List<BattleMinionRuntime>(new BattleMinionRuntime[SlotCount]);
            BattleStartEffects = new List<BattleStartEffectState>();
            RuleModifiers = new BattleRuleModifiers();
        }

        public List<BattleMinionRuntime> Player { get; }
        public List<BattleMinionRuntime> Enemy { get; }
        public List<BattleStartEffectState> BattleStartEffects { get; }
        public BattleRuleModifiers RuleModifiers { get; private set; }
        public int PlayerFlourishStacks { get; set; }
        public int EnemyFlourishStacks { get; set; }

        public int GetFlourishStacks(BattleSide side)
        {
            return side == BattleSide.Player
                ? PlayerFlourishStacks
                : EnemyFlourishStacks;
        }

        public void AddFlourishStacks(BattleSide side, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (side == BattleSide.Player)
            {
                PlayerFlourishStacks += amount;
            }
            else
            {
                EnemyFlourishStacks += amount;
            }
        }

        public List<BattleMinionRuntime> GetRow(BattleSide side)
        {
            return side == BattleSide.Player ? Player : Enemy;
        }

        public List<BattleMinionRuntime> GetOpposingRow(BattleSide side)
        {
            return side == BattleSide.Player ? Enemy : Player;
        }

        public bool HasAlive(BattleSide side)
        {
            return GetRow(side).Any(minion => minion != null && minion.IsAlive);
        }

        public BattleBoardState Clone()
        {
            var clone = new BattleBoardState();
            CopyRow(Player, clone.Player);
            CopyRow(Enemy, clone.Enemy);
            clone.PlayerFlourishStacks = PlayerFlourishStacks;
            clone.EnemyFlourishStacks = EnemyFlourishStacks;
            clone.BattleStartEffects.AddRange(BattleStartEffects);
            clone.RuleModifiers = RuleModifiers.Clone();
            return clone;
        }

        private static void CopyRow(
            IReadOnlyList<BattleMinionRuntime> source,
            IList<BattleMinionRuntime> destination)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                destination[i] = source[i]?.Clone();
            }
        }
    }
}

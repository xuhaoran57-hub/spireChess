using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Battle
{
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
        }

        public List<BattleMinionRuntime> Player { get; }
        public List<BattleMinionRuntime> Enemy { get; }
        public List<BattleStartEffectState> BattleStartEffects { get; }

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
            clone.BattleStartEffects.AddRange(BattleStartEffects);
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

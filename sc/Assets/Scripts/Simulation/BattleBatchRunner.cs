using System;
using System.Collections.Generic;
using SpireChess.Battle;
using SpireChess.Config;

namespace SpireChess.Simulation
{
    public sealed class BattleBatchResult
    {
        public int Battles { get; internal set; }
        public int PlayerWins { get; internal set; }
        public int EnemyWins { get; internal set; }
        public int Draws { get; internal set; }
        public double PlayerWinRate => Battles <= 0 ? 0d : (double)PlayerWins / Battles;
    }

    public sealed class BattleBatchRunner
    {
        private readonly Func<string, MinionConfig> resolveMinion;

        public BattleBatchRunner(Func<string, MinionConfig> resolveMinion)
        {
            this.resolveMinion = resolveMinion;
        }

        public BattleBatchResult Run(
            BattleBoardState fixture,
            int firstSeed,
            int battleCount)
        {
            if (fixture == null) throw new ArgumentNullException(nameof(fixture));
            if (battleCount < 1) throw new ArgumentOutOfRangeException(nameof(battleCount));

            var result = new BattleBatchResult { Battles = battleCount };
            for (var i = 0; i < battleCount; i++)
            {
                var simulator = new BattleSimulator(
                    new Random(unchecked(firstSeed + i)),
                    resolveMinion);
                var battle = simulator.Simulate(fixture);
                if (battle.Winner == BattleSide.Player)
                {
                    result.PlayerWins++;
                }
                else if (battle.Winner == BattleSide.Enemy)
                {
                    result.EnemyWins++;
                }
                else
                {
                    result.Draws++;
                }
            }

            return result;
        }
    }

    public static class BuildFixture
    {
        public static BattleBoardState Create(
            IEnumerable<BattleMinionRuntime> player,
            IEnumerable<BattleMinionRuntime> enemy)
        {
            var state = new BattleBoardState();
            Fill(state.Player, player);
            Fill(state.Enemy, enemy);
            return state;
        }

        private static void Fill(
            IList<BattleMinionRuntime> row,
            IEnumerable<BattleMinionRuntime> values)
        {
            var index = 0;
            foreach (var value in values ?? Array.Empty<BattleMinionRuntime>())
            {
                if (index >= row.Count) break;
                row[index++] = value;
            }
        }
    }
}

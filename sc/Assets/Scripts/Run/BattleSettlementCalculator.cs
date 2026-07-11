using System;
using System.Linq;
using SpireChess.Battle;
using SpireChess.Config;

namespace SpireChess.Run
{
    public static class BattleSettlementCalculator
    {
        public static BattleSettlementResult Calculate(
            BattleSimulationResult result,
            EncounterConfig encounter)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            var playerWon = result.OutcomeReason == BattleOutcomeReason.Victory &&
                            result.Winner == BattleSide.Player;
            if (playerWon)
            {
                return new BattleSettlementResult(
                    true,
                    0,
                    0,
                    0,
                    encounter.DamageBonus,
                    result.OutcomeReason);
            }

            if (result.OutcomeReason != BattleOutcomeReason.Victory)
            {
                return new BattleSettlementResult(
                    false,
                    1,
                    0,
                    0,
                    encounter.DamageBonus,
                    result.OutcomeReason);
            }

            var survivors = result.FinalState.Enemy
                .Where(minion => minion != null && minion.IsAlive)
                .ToList();
            if (survivors.Count == 0)
            {
                return new BattleSettlementResult(
                    false,
                    0,
                    0,
                    0,
                    encounter.DamageBonus,
                    result.OutcomeReason);
            }

            var highestTier = survivors.Max(minion => minion.Config.Tier);
            var damage = Math.Max(1, survivors.Count + highestTier + encounter.DamageBonus);
            return new BattleSettlementResult(
                false,
                damage,
                survivors.Count,
                highestTier,
                encounter.DamageBonus,
                result.OutcomeReason);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Shop
{
    public sealed class ShopTargetingResult
    {
        private const string IllegalBattleTargetReason = "该随从不是合法目标";

        public ShopTargetingResult(
            bool requiresBattleTarget,
            IEnumerable<int> legalBattleTargetIndexes,
            string disabledReason = null)
        {
            RequiresBattleTarget = requiresBattleTarget;
            LegalBattleTargetIndexes = Array.AsReadOnly(
                (legalBattleTargetIndexes ?? Array.Empty<int>()).ToArray());
            DisabledReason = disabledReason;
        }

        public bool RequiresBattleTarget { get; }
        public IReadOnlyList<int> LegalBattleTargetIndexes { get; }
        public string DisabledReason { get; }

        public bool IsLegalBattleTarget(int index)
        {
            return LegalBattleTargetIndexes.Contains(index);
        }

        public string GetBattleTargetDisabledReason(int index)
        {
            if (!RequiresBattleTarget || IsLegalBattleTarget(index))
            {
                return null;
            }

            return DisabledReason ?? IllegalBattleTargetReason;
        }
    }

    public static class ShopTargetingQuery
    {
        private const string NoLegalTargetReason = "没有合法目标";
        private const string InvalidTimingReason = "当前阶段不能使用";

        public static ShopTargetingResult ForHandCard(
            ShopSession session,
            int handIndex)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (handIndex < 0 || handIndex >= session.Collection.Bench.Count)
            {
                return NoTargetRequired();
            }

            var card = session.Collection.Bench[handIndex];
            if (card == null)
            {
                return NoTargetRequired();
            }

            if (card.CardType == ShopCardType.Spell &&
                (card.Spell.UseTiming == null ||
                 !card.Spell.UseTiming.Contains("Shop")))
            {
                return new ShopTargetingResult(
                    true,
                    Array.Empty<int>(),
                    InvalidTimingReason);
            }

            var trigger = card.CardType == ShopCardType.Minion
                ? "OnPlay"
                : "Manual";
            var directTargetEffects = GetTriggeredEffects(card, trigger)
                .Where(IsDirectBattleTargetEffect)
                .ToArray();
            if (directTargetEffects.Length == 0)
            {
                return NoTargetRequired();
            }

            var engine = new ShopEffectEngine(
                session.Collection,
                new Random(0),
                session.PhaseStats,
                _ => { },
                _ => { },
                _ => { },
                _ => { });
            var source = card.CardType == ShopCardType.Minion ? card : null;
            var legalIndexes = new List<int>();
            for (var battleIndex = 0;
                 battleIndex < session.Collection.Battle.Count;
                 battleIndex++)
            {
                if (session.Collection.Battle[battleIndex] == null)
                {
                    continue;
                }

                if (engine.TryBuildPlan(
                        directTargetEffects,
                        source,
                        -1,
                        battleIndex,
                        true,
                        out var plan,
                        out _) &&
                    plan.Count > 0)
                {
                    legalIndexes.Add(battleIndex);
                }
            }

            if (legalIndexes.Count == 0 &&
                card.CardType == ShopCardType.Minion)
            {
                return NoTargetRequired();
            }

            return new ShopTargetingResult(
                true,
                legalIndexes,
                legalIndexes.Count == 0 ? NoLegalTargetReason : null);
        }

        private static ShopTargetingResult NoTargetRequired()
        {
            return new ShopTargetingResult(
                false,
                Array.Empty<int>());
        }

        private static IEnumerable<EffectConfig> GetTriggeredEffects(
            ShopCardInstance card,
            string trigger)
        {
            IEnumerable<EffectConfig> effects = card.CardType == ShopCardType.Spell
                ? card.Spell.Effects
                : card.IsGolden
                    ? card.Minion.GoldenEffects
                    : card.Minion.Effects;
            return (effects ?? Enumerable.Empty<EffectConfig>())
                .Where(effect => effect != null && effect.Trigger == trigger);
        }

        private static bool IsDirectBattleTargetEffect(EffectConfig effect)
        {
            if (effect?.Target == null ||
                effect.Target.Scope != "Single" ||
                (effect.Target.Selector != "PlayerChoice" &&
                 effect.Target.Selector != "None"))
            {
                return false;
            }

            switch (effect.Action)
            {
                case "ModifyStats":
                case "AddShield":
                case "AddKeyword":
                case "SetPendingCombatBuff":
                    return true;
                default:
                    return false;
            }
        }
    }
}

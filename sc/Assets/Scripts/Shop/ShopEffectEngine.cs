using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Shop
{
    internal sealed class ResolvedShopEffect
    {
        public ResolvedShopEffect(
            EffectConfig effect,
            IReadOnlyList<ShopCardInstance> targets)
        {
            Effect = effect;
            Targets = targets;
        }

        public EffectConfig Effect { get; }
        public IReadOnlyList<ShopCardInstance> Targets { get; }
    }

    internal sealed class ShopEffectEngine
    {
        private readonly PlayerCollection collection;
        private readonly Random random;
        private readonly ShopPhaseStats stats;
        private readonly Action<int> grantGold;
        private readonly Action<int> grantFreeRefreshes;
        private readonly Action<int> scheduleGold;
        private readonly Action<ShopCardInstance> shieldGained;

        public ShopEffectEngine(
            PlayerCollection collection,
            Random random,
            ShopPhaseStats stats,
            Action<int> grantGold,
            Action<int> grantFreeRefreshes,
            Action<int> scheduleGold,
            Action<ShopCardInstance> shieldGained)
        {
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.grantGold = grantGold ?? throw new ArgumentNullException(nameof(grantGold));
            this.grantFreeRefreshes = grantFreeRefreshes ??
                throw new ArgumentNullException(nameof(grantFreeRefreshes));
            this.scheduleGold = scheduleGold ?? throw new ArgumentNullException(nameof(scheduleGold));
            this.shieldGained = shieldGained ??
                throw new ArgumentNullException(nameof(shieldGained));
        }

        public bool TryBuildPlan(
            IReadOnlyList<EffectConfig> effects,
            ShopCardInstance source,
            int sourceBattleIndex,
            int playerTargetIndex,
            bool requireEveryEffect,
            out List<ResolvedShopEffect> plan,
            out ShopOperationError error,
            ShopCardInstance eventSubject = null)
        {
            plan = new List<ResolvedShopEffect>();
            error = ShopOperationError.None;

            foreach (var effect in effects ?? Array.Empty<EffectConfig>())
            {
                if (effect == null)
                {
                    continue;
                }

                if (!EvaluateCondition(effect.Condition, source))
                {
                    continue;
                }

                var value = effect.Value;
                switch (effect.Action)
                {
                    case "ModifyStats":
                    case "AddShield":
                    case "AddKeyword":
                    case "SetPendingCombatBuff":
                        if (value == null || !HasUsefulValue(effect.Action, value))
                        {
                            error = requireEveryEffect
                                ? ShopOperationError.NoBenefit
                                : ShopOperationError.UnsupportedEffect;
                            return false;
                        }

                        if (!TryResolveTargets(
                                effect.Target,
                                source,
                                sourceBattleIndex,
                                playerTargetIndex,
                                eventSubject,
                                out var targets,
                                out error))
                        {
                            return false;
                        }

                        if (targets.Count == 0)
                        {
                            if (requireEveryEffect)
                            {
                                error = ShopOperationError.NoBenefit;
                                return false;
                            }

                            continue;
                        }

                        targets = FilterTargetsForCondition(
                            targets,
                            effect.Condition);
                        if (targets.Count == 0)
                        {
                            continue;
                        }

                        if (effect.Action == "AddShield" &&
                            targets.All(target => target.HasEffectiveKeyword("Shield")) &&
                            effect.FallbackEffects != null &&
                            effect.FallbackEffects.Count > 0)
                        {
                            foreach (var fallback in effect.FallbackEffects)
                            {
                                plan.Add(new ResolvedShopEffect(fallback, targets));
                            }
                            break;
                        }

                        plan.Add(new ResolvedShopEffect(effect, targets));
                        break;
                    case "GainGold":
                    case "ScheduleGold":
                    case "FreeRefresh":
                        if (value == null || value.Amount <= 0)
                        {
                            error = requireEveryEffect
                                ? ShopOperationError.NoBenefit
                                : ShopOperationError.UnsupportedEffect;
                            return false;
                        }

                        plan.Add(new ResolvedShopEffect(
                            effect,
                            Array.Empty<ShopCardInstance>()));
                        break;
                    case "ActivateCardListeners":
                        plan.Add(new ResolvedShopEffect(
                            effect,
                            Array.Empty<ShopCardInstance>()));
                        break;
                    default:
                        error = ShopOperationError.UnsupportedEffect;
                        return false;
                }
            }

            if (requireEveryEffect && plan.Count == 0)
            {
                error = ShopOperationError.NoBenefit;
                return false;
            }

            return true;
        }

        public void ApplyPlan(IEnumerable<ResolvedShopEffect> plan)
        {
            foreach (var resolved in plan ?? Array.Empty<ResolvedShopEffect>())
            {
                var value = resolved.Effect.Value;
                switch (resolved.Effect.Action)
                {
                    case "ModifyStats":
                        foreach (var target in resolved.Targets)
                        {
                            if (IsNextCombat(value))
                            {
                                target.AddPendingCombatModifier(new PendingCombatModifier(
                                    resolved.Effect.Id,
                                    value.Attack,
                                    value.Health,
                                    null,
                                    false));
                            }
                            else
                            {
                                target.ApplyPermanentStats(value.Attack, value.Health);
                            }
                        }
                        break;
                    case "AddShield":
                        foreach (var target in resolved.Targets)
                        {
                            var alreadyHasShield = target.HasEffectiveKeyword("Shield") ||
                                target.HasPendingCombatShield;
                            target.AddPendingCombatModifier(new PendingCombatModifier(
                                resolved.Effect.Id,
                                value.Attack,
                                value.Health,
                                value.Keyword,
                                true));
                            if (!alreadyHasShield)
                            {
                                shieldGained(target);
                            }
                        }
                        break;
                    case "SetPendingCombatBuff":
                        foreach (var target in resolved.Targets)
                        {
                            target.AddPendingCombatModifier(new PendingCombatModifier(
                                resolved.Effect.Id,
                                value.Attack,
                                value.Health,
                                value.Keyword,
                                value.Keyword == "Shield"));
                        }
                        break;
                    case "AddKeyword":
                        foreach (var target in resolved.Targets)
                        {
                            if (IsNextCombat(value))
                            {
                                target.AddPendingCombatModifier(new PendingCombatModifier(
                                    resolved.Effect.Id,
                                    0,
                                    0,
                                    value.Keyword,
                                    value.Keyword == "Shield"));
                            }
                            else
                            {
                                target.TryGrantPermanentKeyword(value.Keyword);
                            }
                        }
                        break;
                    case "GainGold":
                        grantGold(value.Amount);
                        break;
                    case "ScheduleGold":
                        scheduleGold(value.Amount);
                        break;
                    case "FreeRefresh":
                        grantFreeRefreshes(value.Amount);
                        break;
                }
            }
        }

        private bool EvaluateCondition(ConditionConfig condition, ShopCardInstance source)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.Type) ||
                condition.Type == "None")
            {
                return true;
            }

            switch (condition.Type)
            {
                case "IsGolden":
                    return source == null || source.IsGolden;
                case "HasGoldenMinion":
                    return collection.Battle.Any(card => card != null && card.IsGolden);
                case "PhaseStatAtLeast":
                    return GetPhaseStat(condition.PhaseStat) >= condition.Threshold;
                case "RaceCountAtLeast":
                    return collection.Battle.Count(card =>
                        card != null && card.Minion.Race == condition.Race) >= condition.Threshold;
                case "HasShield":
                    return source != null && source.HasEffectiveKeyword("Shield");
                case "TargetAlreadyHasShield":
                case "IsMostCommonMainRace":
                    return true;
                default:
                    return false;
            }
        }

        private int GetPhaseStat(string phaseStat)
        {
            switch (phaseStat)
            {
                case "RefreshCount": return stats.RefreshCount;
                case "SpellUsedCount": return stats.SpellUsedCount;
                case "SpellBoughtCount": return stats.SpellBoughtCount;
                case "MinionBoughtCount": return stats.MinionBoughtCount;
                case "RefreshesSinceActivation": return int.MaxValue;
                default: return 0;
            }
        }

        private IReadOnlyList<ShopCardInstance> FilterTargetsForCondition(
            IReadOnlyList<ShopCardInstance> targets,
            ConditionConfig condition)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.Type) ||
                condition.Type == "None")
            {
                return targets;
            }

            switch (condition.Type)
            {
                case "TargetAlreadyHasShield":
                    return targets.Where(target => target.HasEffectiveKeyword("Shield")).ToList();
                case "IsGolden":
                    return targets.Where(target => target.IsGolden).ToList();
                case "IsMostCommonMainRace":
                    var priority = new[] { "ForgeSoul", "WildSpirit", "Starbound" };
                    var counts = priority.ToDictionary(
                        race => race,
                        race => collection.Battle.Count(card =>
                            card != null && card.Minion.Race == race));
                    var maximum = counts.Values.DefaultIfEmpty(0).Max();
                    var mostCommon = priority.FirstOrDefault(race => counts[race] == maximum && maximum > 0);
                    return targets.Where(target => target.Minion.Race == mostCommon).ToList();
                default:
                    return targets;
            }
        }

        private bool TryResolveTargets(
            TargetConfig target,
            ShopCardInstance source,
            int sourceBattleIndex,
            int playerTargetIndex,
            ShopCardInstance eventSubject,
            out IReadOnlyList<ShopCardInstance> targets,
            out ShopOperationError error)
        {
            targets = Array.Empty<ShopCardInstance>();
            error = ShopOperationError.None;
            if (target == null ||
                (!string.IsNullOrWhiteSpace(target.Side) && target.Side != "Ally") ||
                (target.Zones != null && target.Zones.Count > 0 &&
                 !target.Zones.Contains("Battle")))
            {
                error = ShopOperationError.UnsupportedEffect;
                return false;
            }

            if (target.Scope == "Self")
            {
                if (source != null)
                {
                    targets = new[] { source };
                }

                return true;
            }

            if (target.Scope == "EventSubject")
            {
                if (IsEligibleTarget(eventSubject, source, target))
                {
                    targets = new[] { eventSubject };
                }

                return true;
            }

            var candidates = new List<KeyValuePair<int, ShopCardInstance>>();
            for (var i = 0; i < collection.Battle.Count; i++)
            {
                var card = i == sourceBattleIndex ? source : collection.Battle[i];
                if (IsEligibleTarget(card, source, target))
                {
                    candidates.Add(new KeyValuePair<int, ShopCardInstance>(i, card));
                }
            }

            switch (target.Scope)
            {
                case "All":
                    targets = candidates.Select(candidate => candidate.Value).ToList();
                    return true;
                case "Left":
                    targets = candidates
                        .Where(candidate => candidate.Key == sourceBattleIndex - 1)
                        .Select(candidate => candidate.Value).ToList();
                    return true;
                case "Right":
                    targets = candidates
                        .Where(candidate => candidate.Key == sourceBattleIndex + 1)
                        .Select(candidate => candidate.Value).ToList();
                    return true;
                case "Adjacent":
                    targets = candidates
                        .Where(candidate => Math.Abs(candidate.Key - sourceBattleIndex) == 1)
                        .Select(candidate => candidate.Value).ToList();
                    return true;
                case "Single":
                    break;
                default:
                    error = ShopOperationError.UnsupportedEffect;
                    return false;
            }

            if (candidates.Count == 0)
            {
                return true;
            }

            switch (target.Selector)
            {
                case "Random":
                    var randomCount = target.MaxTargets > 0
                        ? Math.Min(target.MaxTargets, candidates.Count)
                        : 1;
                    var randomCandidates = candidates.ToList();
                    var randomTargets = new List<ShopCardInstance>();
                    while (randomTargets.Count < randomCount)
                    {
                        var index = random.Next(randomCandidates.Count);
                        randomTargets.Add(randomCandidates[index].Value);
                        randomCandidates.RemoveAt(index);
                    }
                    targets = randomTargets;
                    return true;
                case "LowestAttack":
                    targets = candidates.OrderBy(value => value.Value.CurrentAttack)
                        .ThenBy(value => value.Key)
                        .Take(target.MaxTargets > 0 ? target.MaxTargets : 1)
                        .Select(value => value.Value).ToList();
                    return true;
                case "LowestHealth":
                    targets = candidates.OrderBy(value => value.Value.CurrentHealth)
                        .ThenBy(value => value.Key)
                        .Take(target.MaxTargets > 0 ? target.MaxTargets : 1)
                        .Select(value => value.Value).ToList();
                    return true;
                case "Leftmost":
                    targets = new[] { candidates.OrderBy(value => value.Key).First().Value };
                    return true;
                case "Rightmost":
                    targets = new[] { candidates.OrderByDescending(value => value.Key).First().Value };
                    return true;
                case "PlayerChoice":
                case "None":
                    var selected = candidates.FirstOrDefault(value => value.Key == playerTargetIndex);
                    if (selected.Value == null)
                    {
                        error = ShopOperationError.InvalidTarget;
                        return false;
                    }

                    targets = new[] { selected.Value };
                    return true;
                default:
                    error = ShopOperationError.UnsupportedEffect;
                    return false;
            }
        }

        private static bool IsEligibleTarget(
            ShopCardInstance card,
            ShopCardInstance source,
            TargetConfig target)
        {
            if (card == null || card.CardType != ShopCardType.Minion)
            {
                return false;
            }

            if (!target.IncludeSelf && source != null && ReferenceEquals(card, source))
            {
                return false;
            }

            if (!target.IncludeToken && card.Minion.IsToken)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(target.Race) || target.Race == card.Minion.Race;
        }

        private static bool HasUsefulValue(string action, ValueConfig value)
        {
            if (action == "AddShield")
            {
                return true;
            }

            if (action == "AddKeyword")
            {
                return !string.IsNullOrWhiteSpace(value.Keyword);
            }

            return value.Attack != 0 || value.Health != 0 ||
                !string.IsNullOrWhiteSpace(value.Keyword);
        }

        private static bool IsNextCombat(ValueConfig value)
        {
            return value != null && value.Duration == "NextCombat";
        }
    }
}

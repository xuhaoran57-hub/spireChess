using System;
using System.Collections.Generic;
using System.Linq;

namespace SpireChess.Config
{
    public static class RelicConfigValidator
    {
        private static readonly HashSet<string> ValidGrades = new HashSet<string>(
            new[] { "Crown", "Curio" },
            StringComparer.Ordinal);

        private static readonly HashSet<string> ValidEffectTypes = new HashSet<string>(
            new[]
            {
                "ExtraDeathrattleTriggers",
                "ExtraBattlecryTriggers",
                "FirstPurchaseFree",
                "GrantRandomSpellByShopInterval",
                "GrantRandomMinionByShopInterval",
                "GrantCombatShieldAtBattleStart",
                "SummonOnFirstFriendlyNonTokenDeath",
                "GrantCombatStatsPerDistinctRace",
                "FirstPaidRefreshFree",
                "GoldOnShopStart",
                "GoldOnFirstMinionSold",
                "HealAfterEliteOrBossVictory"
            },
            StringComparer.Ordinal);

        public static ConfigValidationResult Validate(
            IReadOnlyList<RelicConfig> relics,
            IReadOnlyDictionary<string, MinionConfig> minions = null)
        {
            var result = new ConfigValidationResult();
            relics = relics ?? Array.Empty<RelicConfig>();
            if (relics.Count == 0)
            {
                return result;
            }

            foreach (var group in relics.GroupBy(value => value?.Id))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    result.AddError("A relic has an empty id.");
                }
                else if (group.Count() > 1)
                {
                    result.AddError($"Duplicate relic id: {group.Key}.");
                }
            }

            foreach (var relic in relics.Where(value => value != null))
            {
                ValidateRelic(relic, minions, result);
            }

            var playable = relics.Where(value => value != null && value.Enabled &&
                value.ImplementationStatus == "Playable").ToList();
            if (playable.Count != 15)
            {
                result.AddError($"Relic config should contain 15 playable relics, got {playable.Count}.");
            }

            var crownCount = playable.Count(value => value.Grade == "Crown");
            var curioCount = playable.Count(value => value.Grade == "Curio");
            if (crownCount != 8 || curioCount != 7)
            {
                result.AddError(
                    $"Relic config should contain 8 Crown and 7 Curio relics, got {crownCount} and {curioCount}.");
            }

            return result;
        }

        private static void ValidateRelic(
            RelicConfig relic,
            IReadOnlyDictionary<string, MinionConfig> minions,
            ConfigValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(relic.Name) ||
                string.IsNullOrWhiteSpace(relic.Description) ||
                string.IsNullOrWhiteSpace(relic.Category))
            {
                result.AddError($"Relic {relic.Id} is missing display content.");
            }

            if (!ValidGrades.Contains(relic.Grade ?? string.Empty))
            {
                result.AddError($"Relic {relic.Id} has invalid grade {relic.Grade}.");
            }

            if (!ValidEffectTypes.Contains(relic.EffectType ?? string.Empty))
            {
                result.AddError($"Relic {relic.Id} has invalid effect type {relic.EffectType}.");
                return;
            }

            if ((relic.Grade == "Crown" &&
                 !(relic.Id ?? string.Empty).StartsWith("crown_", StringComparison.Ordinal)) ||
                (relic.Grade == "Curio" &&
                 !(relic.Id ?? string.Empty).StartsWith("curio_", StringComparison.Ordinal)))
            {
                result.AddError($"Relic {relic.Id} id does not match grade {relic.Grade}.");
            }

            switch (relic.EffectType)
            {
                case "ExtraDeathrattleTriggers":
                case "ExtraBattlecryTriggers":
                case "FirstPurchaseFree":
                case "FirstPaidRefreshFree":
                case "GoldOnFirstMinionSold":
                case "HealAfterEliteOrBossVictory":
                    RequirePositive(relic, relic.Amount, "amount", result);
                    break;
                case "GoldOnShopStart":
                    RequirePositive(relic, relic.Amount, "amount", result);
                    RequirePositive(relic, relic.Threshold, "threshold", result);
                    if (relic.ConditionType != "HealthPercentAtMost")
                    {
                        result.AddError($"Relic {relic.Id} has invalid shop-start condition.");
                    }
                    break;
                case "GrantRandomSpellByShopInterval":
                    RequirePositive(relic, relic.Interval, "interval", result);
                    if (relic.TierMode != "AtMostCurrent")
                    {
                        result.AddError($"Relic {relic.Id} has invalid spell tier mode.");
                    }
                    break;
                case "GrantRandomMinionByShopInterval":
                    RequirePositive(relic, relic.Interval, "interval", result);
                    if (relic.TierMode != "ExactCurrent" &&
                        (relic.TierMode != "Exact" || relic.CardTier < 1 || relic.CardTier > 6))
                    {
                        result.AddError($"Relic {relic.Id} has invalid minion tier mode.");
                    }
                    break;
                case "GrantCombatShieldAtBattleStart":
                    RequirePositive(relic, relic.TargetCount, "targetCount", result);
                    if (relic.Selector != "LowestHealthWithoutShield")
                    {
                        result.AddError($"Relic {relic.Id} has invalid shield selector.");
                    }
                    break;
                case "SummonOnFirstFriendlyNonTokenDeath":
                    RequirePositive(relic, relic.Amount, "amount", result);
                    RequirePositive(relic, relic.Attack, "attack", result);
                    RequirePositive(relic, relic.Health, "health", result);
                    if (string.IsNullOrWhiteSpace(relic.TokenId))
                    {
                        result.AddError($"Relic {relic.Id} is missing tokenId.");
                    }
                    else if (minions == null ||
                             !minions.TryGetValue(relic.TokenId, out var token) ||
                             token == null || !token.IsToken)
                    {
                        result.AddError(
                            $"Relic {relic.Id} references missing token {relic.TokenId}.");
                    }
                    break;
                case "GrantCombatStatsPerDistinctRace":
                    RequirePositive(relic, relic.Threshold, "threshold", result);
                    break;
            }
        }

        private static void RequirePositive(
            RelicConfig relic,
            int value,
            string field,
            ConfigValidationResult result)
        {
            if (value <= 0)
            {
                result.AddError($"Relic {relic.Id} must have a positive {field}.");
            }
        }
    }
}

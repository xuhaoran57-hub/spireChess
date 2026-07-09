using System.Collections.Generic;
using System.Linq;

namespace SpireChess.Config
{
    public static class ConfigValidator
    {
        private const int ExpectedMinionCount = 52;
        private const int ExpectedTokenCount = 2;
        private const int ExpectedSpellCount = 15;

        private static readonly HashSet<string> ValidRaces = new HashSet<string>
        {
            "ForgeSoul",
            "WildSpirit",
            "Starbound",
            "Wayfarer"
        };

        private static readonly HashSet<string> ValidKeywords = new HashSet<string>
        {
            "Taunt",
            "Shield",
            "Deathrattle",
            "Battlecry",
            "Cleave"
        };

        private static readonly HashSet<string> ValidSpellTypes = new HashSet<string>
        {
            "Growth",
            "Economy",
            "Defense",
            "Refresh",
            "Discover",
            "Copy",
            "CombatBuff"
        };

        private static readonly HashSet<string> ValidUseTiming = new HashSet<string>
        {
            "Shop",
            "Prep",
            "Combat"
        };

        public static ConfigValidationResult Validate(
            IReadOnlyList<MinionConfig> minions,
            IReadOnlyList<SpellConfig> spells)
        {
            var result = new ConfigValidationResult();
            ValidateMinions(minions, result);
            ValidateSpells(spells, result);
            return result;
        }

        private static void ValidateMinions(
            IReadOnlyList<MinionConfig> minions,
            ConfigValidationResult result)
        {
            if (minions == null)
            {
                result.AddError("Minion list is missing.");
                return;
            }

            if (minions.Count != ExpectedMinionCount)
            {
                result.AddError($"Minion count should be {ExpectedMinionCount}, got {minions.Count}.");
            }

            var tokenCount = minions.Count(minion => minion.IsToken);
            if (tokenCount != ExpectedTokenCount)
            {
                result.AddError($"Token count should be {ExpectedTokenCount}, got {tokenCount}.");
            }

            foreach (var group in minions.GroupBy(minion => minion.Id))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    result.AddError("A minion has an empty id.");
                }
                else if (group.Count() > 1)
                {
                    result.AddError($"Duplicate minion id: {group.Key}.");
                }
            }

            foreach (var minion in minions)
            {
                if (!IsValidMinionTier(minion))
                {
                    result.AddError($"Invalid tier for minion {minion.Id}: {minion.Tier}.");
                }

                if (!ValidRaces.Contains(minion.Race ?? string.Empty))
                {
                    result.AddError($"Invalid race for minion {minion.Id}: {minion.Race}.");
                }

                ValidateKeywords(minion, result);
            }
        }

        private static bool IsValidMinionTier(MinionConfig minion)
        {
            if (minion.IsToken)
            {
                return minion.Tier == 0;
            }

            return minion.Tier >= 1 && minion.Tier <= 5;
        }

        private static void ValidateKeywords(
            MinionConfig minion,
            ConfigValidationResult result)
        {
            if (minion.Keywords == null)
            {
                return;
            }

            foreach (var keyword in minion.Keywords)
            {
                if (!ValidKeywords.Contains(keyword ?? string.Empty))
                {
                    result.AddError($"Invalid keyword for minion {minion.Id}: {keyword}.");
                }
            }
        }

        private static void ValidateSpells(
            IReadOnlyList<SpellConfig> spells,
            ConfigValidationResult result)
        {
            if (spells == null)
            {
                result.AddError("Spell list is missing.");
                return;
            }

            if (spells.Count != ExpectedSpellCount)
            {
                result.AddError($"Spell count should be {ExpectedSpellCount}, got {spells.Count}.");
            }

            foreach (var group in spells.GroupBy(spell => spell.Id))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    result.AddError("A spell has an empty id.");
                }
                else if (group.Count() > 1)
                {
                    result.AddError($"Duplicate spell id: {group.Key}.");
                }
            }

            foreach (var spell in spells)
            {
                if (spell.Tier < 1 || spell.Tier > 5)
                {
                    result.AddError($"Invalid tier for spell {spell.Id}: {spell.Tier}.");
                }

                if (!ValidSpellTypes.Contains(spell.SpellType ?? string.Empty))
                {
                    result.AddError($"Invalid spell type for spell {spell.Id}: {spell.SpellType}.");
                }

                ValidateUseTiming(spell, result);
            }
        }

        private static void ValidateUseTiming(
            SpellConfig spell,
            ConfigValidationResult result)
        {
            if (spell.UseTiming == null || spell.UseTiming.Count == 0)
            {
                result.AddError($"Spell {spell.Id} has no use timing.");
                return;
            }

            foreach (var timing in spell.UseTiming)
            {
                if (!ValidUseTiming.Contains(timing ?? string.Empty))
                {
                    result.AddError($"Invalid use timing for spell {spell.Id}: {timing}.");
                }
            }
        }
    }
}

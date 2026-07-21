using System;
using System.Collections.Generic;
using System.Linq;

namespace SpireChess.Config
{
    public static class RunContentValidator
    {
        private static readonly HashSet<string> ValidNodeTypes = new HashSet<string>(
            new[] { "Shop", "Normal", "Elite", "Enhance", "Event", "Rest", "Boss" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ValidEncounterCategories = new HashSet<string>(
            new[] { "Normal", "Elite", "Boss" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ValidRouteTags = new HashSet<string>(
            new[] { "Aggressive", "Adventure", "Conservative" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ValidRewardTypes = new HashSet<string>(
            new[] { "NextShopGold", "FreeRefresh", "UpgradeDiscount", "Spell", "Minion", "PermanentStats" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ValidEventEffectTypes = new HashSet<string>(
            new[] { "LoseHealth", "HealHealth", "NextShopGold", "FreeRefresh", "UpgradeDiscount", "QueueRandomSpell" },
            StringComparer.OrdinalIgnoreCase);

        public static ConfigValidationResult Validate(
            IReadOnlyList<RunMapConfig> maps,
            IReadOnlyList<RunMapRuleProfileConfig> mapRuleProfiles,
            IReadOnlyList<EncounterConfig> encounters,
            IReadOnlyList<RewardTableConfig> rewardTables,
            IReadOnlyDictionary<string, MinionConfig> minions,
            IReadOnlyDictionary<string, SpellConfig> spells,
            IReadOnlyDictionary<string, EventPoolConfig> eventPools = null,
            IReadOnlyDictionary<string, EventConfig> events = null,
            IReadOnlyDictionary<string, EnhancementRecipeConfig> recipes = null,
            IReadOnlyDictionary<string, EnhanceNodeConfig> enhanceNodes = null,
            IReadOnlyDictionary<string, RestNodeConfig> restNodes = null)
        {
            var result = new ConfigValidationResult();
            maps = maps ?? Array.Empty<RunMapConfig>();
            mapRuleProfiles = mapRuleProfiles ?? Array.Empty<RunMapRuleProfileConfig>();
            encounters = encounters ?? Array.Empty<EncounterConfig>();
            rewardTables = rewardTables ?? Array.Empty<RewardTableConfig>();

            ValidateUniqueIds(maps.Select(map => map?.Id), "run map", result);
            ValidateUniqueIds(mapRuleProfiles.Select(profile => profile?.Id), "run map rule profile", result);
            ValidateUniqueIds(encounters.Select(encounter => encounter?.Id), "encounter", result);
            ValidateUniqueIds(rewardTables.Select(table => table?.Id), "reward table", result);

            var ruleProfilesById = mapRuleProfiles
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Id))
                .GroupBy(value => value.Id)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var profile in mapRuleProfiles.Where(value => value != null))
            {
                ValidateMapRuleProfile(profile, result);
            }

            var encountersById = encounters
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Id))
                .GroupBy(value => value.Id)
                .ToDictionary(group => group.Key, group => group.First());
            var rewardIds = new HashSet<string>(
                rewardTables.Where(value => value != null).Select(value => value.Id));

            foreach (var map in maps.Where(value => value != null))
            {
                if (!ruleProfilesById.TryGetValue(map.RuleProfileId ?? string.Empty, out var ruleProfile))
                {
                    result.AddError(
                        $"Map {map.Id} references missing rule profile {map.RuleProfileId}.");
                }

                ValidateMap(
                    map,
                    ruleProfile,
                    encountersById,
                    new HashSet<string>(eventPools == null ? Array.Empty<string>() : eventPools.Keys),
                    new HashSet<string>(enhanceNodes == null ? Array.Empty<string>() : enhanceNodes.Keys),
                    new HashSet<string>(restNodes == null ? Array.Empty<string>() : restNodes.Keys),
                    result);
            }

            foreach (var encounter in encounters.Where(value => value != null))
            {
                ValidateEncounter(encounter, rewardIds, minions, result);
            }

            foreach (var table in rewardTables.Where(value => value != null))
            {
                ValidateRewardTable(table, minions, spells, result);
            }

            ValidateFourBContent(
                eventPools,
                events,
                recipes,
                enhanceNodes,
                restNodes,
                rewardIds,
                encountersById,
                result);

            return result;
        }

        private static void ValidateMap(
            RunMapConfig map,
            RunMapRuleProfileConfig ruleProfile,
            IReadOnlyDictionary<string, EncounterConfig> encounters,
            ISet<string> eventPoolIds,
            ISet<string> enhanceNodeIds,
            ISet<string> restNodeIds,
            ConfigValidationResult result)
        {
            if (map.Floor < 1)
            {
                result.AddError($"Map {map.Id} has invalid floor {map.Floor}.");
            }

            var nodes = map.Nodes ?? new List<RunMapNodeConfig>();
            ValidateUniqueIds(nodes.Select(node => node?.Id), $"node in map {map.Id}", result);
            var nodeIds = new HashSet<string>(nodes.Where(node => node != null).Select(node => node.Id));
            foreach (var startId in map.StartNodeIds ?? new List<string>())
            {
                if (!nodeIds.Contains(startId))
                {
                    result.AddError($"Map {map.Id} references missing start node {startId}.");
                }
            }

            var bossCount = 0;
            foreach (var node in nodes.Where(value => value != null))
            {
                if (!ValidNodeTypes.Contains(node.Type ?? string.Empty))
                {
                    result.AddError($"Map {map.Id} node {node.Id} has invalid type {node.Type}.");
                }

                if (string.Equals(node.Type, "Boss", StringComparison.OrdinalIgnoreCase))
                {
                    bossCount++;
                }

                if ((string.Equals(node.Type, "Normal", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(node.Type, "Elite", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(node.Type, "Boss", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!encounters.TryGetValue(node.PayloadId ?? string.Empty, out var encounter))
                    {
                        result.AddError(
                            $"Map {map.Id} node {node.Id} references missing encounter {node.PayloadId}.");
                    }
                    else
                    {
                        if (!string.Equals(encounter.Category, node.Type, StringComparison.OrdinalIgnoreCase))
                        {
                            result.AddError(
                                $"Map {map.Id} node {node.Id} type {node.Type} does not match encounter category {encounter.Category}.");
                        }

                        if (encounter.Floor != map.Floor)
                        {
                            result.AddError(
                                $"Map {map.Id} node {node.Id} uses floor {encounter.Floor} encounter {encounter.Id}.");
                        }
                    }

                    var maximumCombatIndex = ruleProfile?.CombatCount ?? 0;
                    if (node.CombatIndex < 1 || node.CombatIndex > maximumCombatIndex)
                    {
                        result.AddError(
                            $"Map {map.Id} combat node {node.Id} has invalid combatIndex {node.CombatIndex}.");
                    }

                    if (string.Equals(node.Type, "Boss", StringComparison.OrdinalIgnoreCase) &&
                        ruleProfile != null &&
                        node.CombatIndex != ruleProfile.BossCombatIndex)
                    {
                        result.AddError(
                            $"Map {map.Id} Boss {node.Id} must use combatIndex {ruleProfile.BossCombatIndex}.");
                    }
                }
                else if (node.CombatIndex != 0)
                {
                    result.AddError(
                        $"Map {map.Id} non-combat node {node.Id} must not set combatIndex {node.CombatIndex}.");
                }

                if (!string.IsNullOrWhiteSpace(node.RouteTag))
                {
                    if (!ValidRouteTags.Contains(node.RouteTag))
                    {
                        result.AddError(
                            $"Map {map.Id} node {node.Id} has invalid routeTag {node.RouteTag}.");
                    }
                    else if (ruleProfile == null ||
                             node.CombatIndex != ruleProfile.EliteMinCombatIndex)
                    {
                        result.AddError(
                            $"Map {map.Id} routeTag must be attached to the route combat index.");
                    }
                }

                if (string.Equals(node.Type, "Event", StringComparison.OrdinalIgnoreCase) &&
                    !eventPoolIds.Contains(node.PayloadId ?? string.Empty))
                {
                    result.AddError($"Map {map.Id} node {node.Id} references missing event pool {node.PayloadId}.");
                }

                if (string.Equals(node.Type, "Enhance", StringComparison.OrdinalIgnoreCase) &&
                    !enhanceNodeIds.Contains(node.PayloadId ?? string.Empty))
                {
                    result.AddError($"Map {map.Id} node {node.Id} references missing enhance node {node.PayloadId}.");
                }

                if (string.Equals(node.Type, "Rest", StringComparison.OrdinalIgnoreCase) &&
                    !restNodeIds.Contains(node.PayloadId ?? string.Empty))
                {
                    result.AddError($"Map {map.Id} node {node.Id} references missing rest node {node.PayloadId}.");
                }

                foreach (var nextId in node.NextNodeIds ?? new List<string>())
                {
                    if (!nodeIds.Contains(nextId))
                    {
                        result.AddError($"Map {map.Id} node {node.Id} references missing node {nextId}.");
                    }
                }
            }

            if (bossCount != 1)
            {
                result.AddError($"Map {map.Id} must contain exactly one Boss, got {bossCount}.");
            }

            if (ruleProfile != null)
            {
                ValidateEqualRewardAlternatives(map, ruleProfile, encounters, result);
            }
        }

        private static void ValidateMapRuleProfile(
            RunMapRuleProfileConfig profile,
            ConfigValidationResult result)
        {
            if (profile.ShopCount < 1 || profile.CombatCount < 1)
            {
                result.AddError($"Map rule profile {profile.Id} must have positive shop and combat counts.");
            }

            if (profile.ShopCount != profile.CombatCount)
            {
                result.AddError($"Map rule profile {profile.Id} must use equal shop and combat counts.");
            }

            if (profile.BossCombatIndex != profile.CombatCount)
            {
                result.AddError(
                    $"Map rule profile {profile.Id} Boss must be the final combat.");
            }

            if (profile.EliteMinCombatIndex < 1 ||
                profile.EliteMinCombatIndex > profile.CombatCount)
            {
                result.AddError(
                    $"Map rule profile {profile.Id} has invalid elite minimum index.");
            }

            if (profile.UtilityCountPerPath < 0 ||
                profile.ExpectedNodeCount < 1 ||
                profile.ExpectedPathCount < 1)
            {
                result.AddError(
                    $"Map rule profile {profile.Id} has invalid structural counts.");
            }
        }

        private static void ValidateEqualRewardAlternatives(
            RunMapConfig map,
            RunMapRuleProfileConfig profile,
            IReadOnlyDictionary<string, EncounterConfig> encounters,
            ConfigValidationResult result)
        {
            foreach (var combatIndex in new[] { 2, profile.CombatCount - 1 })
            {
                var alternativeNodes = (map.Nodes ?? new List<RunMapNodeConfig>())
                    .Where(node => node != null &&
                                   node.CombatIndex == combatIndex &&
                                   string.Equals(node.Type, "Normal", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (alternativeNodes.Count != 2)
                {
                    result.AddError(
                        $"Map {map.Id} combat {combatIndex} must contain exactly two Normal alternatives.");
                }

                var alternatives = alternativeNodes
                    .Select(node => encounters.TryGetValue(node.PayloadId ?? string.Empty, out var encounter)
                        ? encounter.RewardTableId
                        : null)
                    .Where(value => value != null)
                    .Distinct()
                    .ToList();
                if (alternatives.Count > 1)
                {
                    result.AddError(
                        $"Map {map.Id} combat {combatIndex} alternatives must use the same reward table.");
                }
            }

            var routeNodes = (map.Nodes ?? new List<RunMapNodeConfig>())
                .Where(node => node != null &&
                               node.CombatIndex == profile.EliteMinCombatIndex &&
                               !string.IsNullOrWhiteSpace(node.RouteTag))
                .ToList();
            if (routeNodes.Count != 3 ||
                routeNodes.Select(node => node.RouteTag).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            {
                result.AddError(
                    $"Map {map.Id} must contain three distinct tagged route combats.");
            }
        }

        private static void ValidateEncounter(
            EncounterConfig encounter,
            ISet<string> rewardIds,
            IReadOnlyDictionary<string, MinionConfig> minions,
            ConfigValidationResult result)
        {
            if (!ValidEncounterCategories.Contains(encounter.Category ?? string.Empty))
            {
                result.AddError(
                    $"Encounter {encounter.Id} has invalid category {encounter.Category}.");
            }

            if (!string.IsNullOrWhiteSpace(encounter.RewardTableId) &&
                !rewardIds.Contains(encounter.RewardTableId))
            {
                result.AddError(
                    $"Encounter {encounter.Id} references missing reward table {encounter.RewardTableId}.");
            }

            var occupiedSlots = new HashSet<int>();
            foreach (var slot in encounter.EnemySlots ?? new List<EnemySlotConfig>())
            {
                if (slot.Slot < 0 || slot.Slot > 4 || !occupiedSlots.Add(slot.Slot))
                {
                    result.AddError($"Encounter {encounter.Id} has invalid or duplicate slot {slot.Slot}.");
                }

                if (minions == null || !minions.TryGetValue(slot.MinionId ?? string.Empty, out var minion) ||
                    minion == null || !minion.Enabled)
                {
                    result.AddError(
                        $"Encounter {encounter.Id} references missing minion {slot.MinionId}.");
                }
            }

            if (encounter.Id == "f1_opening_encounter")
            {
                var slots = encounter.EnemySlots ?? new List<EnemySlotConfig>();
                var openingMinion = slots.Count == 1 && minions != null &&
                                    minions.TryGetValue(slots[0].MinionId ?? string.Empty, out var value)
                    ? value
                    : null;
                if (openingMinion == null || openingMinion.Tier != 1 ||
                    openingMinion.IsToken || slots[0].Golden)
                {
                    result.AddError(
                        "The floor-one opening encounter must contain one non-golden tier-one non-token minion.");
                }
            }
        }

        private static void ValidateRewardTable(
            RewardTableConfig table,
            IReadOnlyDictionary<string, MinionConfig> minions,
            IReadOnlyDictionary<string, SpellConfig> spells,
            ConfigValidationResult result)
        {
            if (table.Mode != "AutomaticOne" && table.Mode != "ChooseOne")
            {
                result.AddError($"Reward table {table.Id} has invalid mode {table.Mode}.");
            }

            if (table.Mode == "ChooseOne" && table.CandidateCount < 2)
            {
                result.AddError($"Reward table {table.Id} must choose at least two candidates.");
            }

            foreach (var entry in table.Entries ?? new List<RewardEntryConfig>())
            {
                if (!ValidRewardTypes.Contains(entry.Type ?? string.Empty))
                {
                    result.AddError(
                        $"Reward table {table.Id} has invalid reward type {entry.Type}.");
                }

                if (entry.Weight <= 0)
                {
                    result.AddError($"Reward table {table.Id} has a non-positive weight.");
                }

                if (string.Equals(entry.Type, "Minion", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(entry.CardId) &&
                    (minions == null || !minions.ContainsKey(entry.CardId)))
                {
                    result.AddError(
                        $"Reward table {table.Id} references missing minion {entry.CardId}.");
                }

                if (string.Equals(entry.Type, "Spell", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(entry.CardId))
                {
                    if (spells == null || !spells.TryGetValue(entry.CardId, out var spell))
                    {
                        result.AddError(
                            $"Reward table {table.Id} references missing spell {entry.CardId}.");
                    }
                    else if (!spell.Enabled || spell.Effects == null || spell.Effects.Count == 0)
                    {
                        result.AddError(
                            $"Reward table {table.Id} references unavailable spell {entry.CardId}.");
                    }
                }

                if (string.Equals(entry.Type, "PermanentStats", StringComparison.OrdinalIgnoreCase) &&
                    entry.Attack == 0 && entry.Health == 0)
                {
                    result.AddError(
                        $"Reward table {table.Id} contains a zero-benefit permanent stat reward.");
                }


                if (string.Equals(entry.Type, "PermanentStats", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(entry.TargetScope, "Battle", StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError($"Reward table {table.Id} permanent stats must target Battle.");
                }
            }
        }

        private static void ValidateFourBContent(
            IReadOnlyDictionary<string, EventPoolConfig> eventPools,
            IReadOnlyDictionary<string, EventConfig> events,
            IReadOnlyDictionary<string, EnhancementRecipeConfig> recipes,
            IReadOnlyDictionary<string, EnhanceNodeConfig> enhanceNodes,
            IReadOnlyDictionary<string, RestNodeConfig> restNodes,
            ISet<string> rewardIds,
            IReadOnlyDictionary<string, EncounterConfig> encounters,
            ConfigValidationResult result)
        {
            eventPools = eventPools ?? new Dictionary<string, EventPoolConfig>();
            events = events ?? new Dictionary<string, EventConfig>();
            recipes = recipes ?? new Dictionary<string, EnhancementRecipeConfig>();
            enhanceNodes = enhanceNodes ?? new Dictionary<string, EnhanceNodeConfig>();
            restNodes = restNodes ?? new Dictionary<string, RestNodeConfig>();
            encounters = encounters ?? new Dictionary<string, EncounterConfig>();

            foreach (var pool in eventPools.Values)
            {
                if (pool.Entries == null || pool.Entries.Count == 0)
                    result.AddError($"Event pool {pool.Id} is empty.");
                foreach (var entry in pool.Entries ?? new List<EventPoolEntryConfig>())
                {
                    if (entry.Weight <= 0) result.AddError($"Event pool {pool.Id} has a non-positive weight.");
                    if (!events.ContainsKey(entry.EventId ?? string.Empty))
                        result.AddError($"Event pool {pool.Id} references missing event {entry.EventId}.");
                }
            }

            foreach (var eventConfig in events.Values)
            {
                var options = eventConfig.Options ?? new List<EventOptionConfig>();
                ValidateUniqueIds(options.Select(value => value?.Id), $"option in event {eventConfig.Id}", result);
                if (options.Count == 0) result.AddError($"Event {eventConfig.Id} has no options.");
                foreach (var option in options.Where(value => value != null))
                {
                    if (!string.IsNullOrWhiteSpace(option.FollowupRewardTableId) &&
                        !rewardIds.Contains(option.FollowupRewardTableId))
                        result.AddError($"Event {eventConfig.Id} references missing reward table {option.FollowupRewardTableId}.");
                    if (!string.IsNullOrWhiteSpace(option.FollowupRelicGrade) &&
                        option.FollowupRelicGrade != "Curio")
                        result.AddError($"Event {eventConfig.Id} has invalid relic grade {option.FollowupRelicGrade}.");
                    if (!string.IsNullOrWhiteSpace(option.FollowupEncounterId))
                    {
                        if (!encounters.TryGetValue(option.FollowupEncounterId, out var encounter))
                            result.AddError($"Event {eventConfig.Id} references missing encounter {option.FollowupEncounterId}.");
                        else if (!string.Equals(encounter.Category, "Normal", StringComparison.OrdinalIgnoreCase))
                            result.AddError($"Event {eventConfig.Id} encounter {option.FollowupEncounterId} must be Normal.");
                    }

                    var followupCount =
                        (string.IsNullOrWhiteSpace(option.FollowupRewardTableId) ? 0 : 1) +
                        (string.IsNullOrWhiteSpace(option.FollowupRelicGrade) ? 0 : 1) +
                        (string.IsNullOrWhiteSpace(option.FollowupEncounterId) ? 0 : 1);
                    if (followupCount > 1)
                        result.AddError($"Event {eventConfig.Id} cannot open multiple followups together.");
                    foreach (var effect in option.Effects ?? new List<RunEffectConfig>())
                    {
                        if (!ValidEventEffectTypes.Contains(effect.Type ?? string.Empty))
                            result.AddError($"Event {eventConfig.Id} has invalid effect {effect.Type}.");
                        if (effect.Amount <= 0)
                            result.AddError($"Event {eventConfig.Id} has non-positive effect amount.");
                        if (!string.IsNullOrWhiteSpace(option.FollowupEncounterId) &&
                            effect.Type == "QueueRandomSpell")
                            result.AddError($"Event {eventConfig.Id} cannot queue a spell before an encounter.");
                    }
                }
            }

            foreach (var recipe in recipes.Values)
            {
                var stats = recipe.Action == "ModifyStats" && (recipe.Attack > 0 || recipe.Health > 0);
                var keyword = recipe.Action == "GrantKeyword" &&
                              (recipe.Keyword == "Shield" || recipe.Keyword == "Taunt");
                if (!stats && !keyword) result.AddError($"Enhancement recipe {recipe.Id} has no valid effect.");
            }

            foreach (var node in enhanceNodes.Values)
            {
                if (node.RecipeIds == null || node.RecipeIds.Count == 0)
                    result.AddError($"Enhance node {node.Id} has no recipes.");
                foreach (var recipeId in node.RecipeIds ?? new List<string>())
                    if (!recipes.ContainsKey(recipeId ?? string.Empty))
                        result.AddError($"Enhance node {node.Id} references missing recipe {recipeId}.");
            }

            foreach (var node in restNodes.Values)
            {
                var options = node.Options ?? new List<RestOptionConfig>();
                ValidateUniqueIds(options.Select(value => value?.Id), $"option in rest node {node.Id}", result);
                if (options.Count == 0) result.AddError($"Rest node {node.Id} has no options.");
                foreach (var option in options.Where(value => value != null))
                    if (option.Heal < 0 || option.MaxHealth < 0)
                        result.AddError($"Rest node {node.Id} contains a negative effect.");
            }
        }

        private static void ValidateUniqueIds(
            IEnumerable<string> ids,
            string label,
            ConfigValidationResult result)
        {
            foreach (var group in (ids ?? Array.Empty<string>()).GroupBy(id => id))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    result.AddError($"A {label} has an empty id.");
                }
                else if (group.Count() > 1)
                {
                    result.AddError($"Duplicate {label} id: {group.Key}.");
                }
            }
        }
    }
}

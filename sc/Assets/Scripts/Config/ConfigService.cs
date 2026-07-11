using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Utils;
using UnityEngine;

namespace SpireChess.Config
{
    public sealed class ConfigService
    {
        private readonly IJsonSerializer serializer;
        private Dictionary<string, MinionConfig> minionsById = new Dictionary<string, MinionConfig>();
        private Dictionary<string, SpellConfig> spellsById = new Dictionary<string, SpellConfig>();
        private Dictionary<string, EncounterConfig> encountersById =
            new Dictionary<string, EncounterConfig>();
        private Dictionary<string, RewardTableConfig> rewardTablesById =
            new Dictionary<string, RewardTableConfig>();
        private Dictionary<string, EventPoolConfig> eventPoolsById =
            new Dictionary<string, EventPoolConfig>();
        private Dictionary<string, EventConfig> eventsById =
            new Dictionary<string, EventConfig>();
        private Dictionary<string, EnhancementRecipeConfig> enhancementRecipesById =
            new Dictionary<string, EnhancementRecipeConfig>();
        private Dictionary<string, EnhanceNodeConfig> enhanceNodesById =
            new Dictionary<string, EnhanceNodeConfig>();
        private Dictionary<string, RestNodeConfig> restNodesById =
            new Dictionary<string, RestNodeConfig>();

        public ConfigService(IJsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public string Version { get; private set; }
        public IReadOnlyList<MinionConfig> Minions { get; private set; } = Array.Empty<MinionConfig>();
        public IReadOnlyList<SpellConfig> Spells { get; private set; } = Array.Empty<SpellConfig>();
        public IReadOnlyDictionary<string, MinionConfig> MinionsById => minionsById;
        public IReadOnlyDictionary<string, SpellConfig> SpellsById => spellsById;
        public IReadOnlyList<RunMapConfig> RunMaps { get; private set; } = Array.Empty<RunMapConfig>();
        public IReadOnlyList<EncounterConfig> Encounters { get; private set; } =
            Array.Empty<EncounterConfig>();
        public IReadOnlyList<RewardTableConfig> RewardTables { get; private set; } =
            Array.Empty<RewardTableConfig>();
        public IReadOnlyDictionary<string, EncounterConfig> EncountersById => encountersById;
        public IReadOnlyDictionary<string, RewardTableConfig> RewardTablesById => rewardTablesById;
        public IReadOnlyDictionary<string, EventPoolConfig> EventPoolsById => eventPoolsById;
        public IReadOnlyDictionary<string, EventConfig> EventsById => eventsById;
        public IReadOnlyDictionary<string, EnhancementRecipeConfig> EnhancementRecipesById =>
            enhancementRecipesById;
        public IReadOnlyDictionary<string, EnhanceNodeConfig> EnhanceNodesById => enhanceNodesById;
        public IReadOnlyDictionary<string, RestNodeConfig> RestNodesById => restNodesById;

        public ConfigValidationResult LoadFromResources(
            string minionResourcePath = "Configs/Json/minions.v0.1",
            string spellResourcePath = "Configs/Json/spells.v0.1",
            string mapResourcePath = "Configs/Json/run-maps.v0.1",
            string encounterResourcePath = "Configs/Json/encounters.v0.1",
            string rewardResourcePath = "Configs/Json/rewards.v0.1",
            string eventResourcePath = "Configs/Json/events.v0.1",
            string enhancementResourcePath = "Configs/Json/enhancements.v0.1",
            string restResourcePath = "Configs/Json/rests.v0.1")
        {
            var minionAsset = Resources.Load<TextAsset>(minionResourcePath);
            var spellAsset = Resources.Load<TextAsset>(spellResourcePath);
            var mapAsset = Resources.Load<TextAsset>(mapResourcePath);
            var encounterAsset = Resources.Load<TextAsset>(encounterResourcePath);
            var rewardAsset = Resources.Load<TextAsset>(rewardResourcePath);
            var eventAsset = Resources.Load<TextAsset>(eventResourcePath);
            var enhancementAsset = Resources.Load<TextAsset>(enhancementResourcePath);
            var restAsset = Resources.Load<TextAsset>(restResourcePath);

            if (minionAsset == null)
            {
                throw new InvalidOperationException($"Missing minion config resource: {minionResourcePath}.");
            }

            if (spellAsset == null)
            {
                throw new InvalidOperationException($"Missing spell config resource: {spellResourcePath}.");
            }

            if (mapAsset == null || encounterAsset == null || rewardAsset == null ||
                eventAsset == null || enhancementAsset == null || restAsset == null)
            {
                throw new InvalidOperationException(
                    "Missing stage-four run content config resource.");
            }

            return LoadFromJson(
                minionAsset.text,
                spellAsset.text,
                mapAsset.text,
                encounterAsset.text,
                rewardAsset.text,
                eventAsset.text,
                enhancementAsset.text,
                restAsset.text);
        }

        public ConfigValidationResult LoadFromJson(string minionsJson, string spellsJson)
        {
            return LoadFromJson(minionsJson, spellsJson, null, null, null, null, null, null);
        }

        public ConfigValidationResult LoadFromJson(
            string minionsJson,
            string spellsJson,
            string mapsJson,
            string encountersJson,
            string rewardsJson)
        {
            return LoadFromJson(
                minionsJson,
                spellsJson,
                mapsJson,
                encountersJson,
                rewardsJson,
                null,
                null,
                null);
        }

        public ConfigValidationResult LoadFromJson(
            string minionsJson,
            string spellsJson,
            string mapsJson,
            string encountersJson,
            string rewardsJson,
            string eventsJson,
            string enhancementsJson,
            string restsJson)
        {
            var minionFile = serializer.FromJson<MinionConfigFile>(minionsJson);
            var spellFile = serializer.FromJson<SpellConfigFile>(spellsJson);

            if (minionFile == null)
            {
                throw new InvalidOperationException("Minion config JSON could not be parsed.");
            }

            if (spellFile == null)
            {
                throw new InvalidOperationException("Spell config JSON could not be parsed.");
            }

            Version = minionFile.Version;
            Minions = minionFile.Minions ?? new List<MinionConfig>();
            Spells = spellFile.Spells ?? new List<SpellConfig>();

            minionsById = Minions
                .Where(minion => !string.IsNullOrWhiteSpace(minion.Id))
                .GroupBy(minion => minion.Id)
                .ToDictionary(group => group.Key, group => group.First());

            spellsById = Spells
                .Where(spell => !string.IsNullOrWhiteSpace(spell.Id))
                .GroupBy(spell => spell.Id)
                .ToDictionary(group => group.Key, group => group.First());

            var validation = ConfigValidator.Validate(Minions, Spells);
            if (string.IsNullOrWhiteSpace(mapsJson) ||
                string.IsNullOrWhiteSpace(encountersJson) ||
                string.IsNullOrWhiteSpace(rewardsJson))
            {
                RunMaps = Array.Empty<RunMapConfig>();
                Encounters = Array.Empty<EncounterConfig>();
                RewardTables = Array.Empty<RewardTableConfig>();
                encountersById.Clear();
                rewardTablesById.Clear();
                eventPoolsById.Clear();
                eventsById.Clear();
                enhancementRecipesById.Clear();
                enhanceNodesById.Clear();
                restNodesById.Clear();
                return validation;
            }

            var mapFile = serializer.FromJson<RunMapConfigFile>(mapsJson);
            var encounterFile = serializer.FromJson<EncounterConfigFile>(encountersJson);
            var rewardFile = serializer.FromJson<RewardConfigFile>(rewardsJson);
            if (mapFile == null || encounterFile == null || rewardFile == null)
            {
                throw new InvalidOperationException("Run content JSON could not be parsed.");
            }

            RunMaps = mapFile.Maps ?? new List<RunMapConfig>();
            Encounters = encounterFile.Encounters ?? new List<EncounterConfig>();
            RewardTables = rewardFile.RewardTables ?? new List<RewardTableConfig>();
            encountersById = Encounters
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Id))
                .GroupBy(value => value.Id)
                .ToDictionary(group => group.Key, group => group.First());
            rewardTablesById = RewardTables
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Id))
                .GroupBy(value => value.Id)
                .ToDictionary(group => group.Key, group => group.First());

            if (!string.IsNullOrWhiteSpace(eventsJson) &&
                !string.IsNullOrWhiteSpace(enhancementsJson) &&
                !string.IsNullOrWhiteSpace(restsJson))
            {
                var eventFile = serializer.FromJson<EventConfigFile>(eventsJson);
                var enhancementFile = serializer.FromJson<EnhancementConfigFile>(enhancementsJson);
                var restFile = serializer.FromJson<RestConfigFile>(restsJson);
                if (eventFile == null || enhancementFile == null || restFile == null)
                {
                    throw new InvalidOperationException("Stage 4B content JSON could not be parsed.");
                }

                eventPoolsById = ToDictionary(eventFile.EventPools, value => value.Id);
                eventsById = ToDictionary(eventFile.Events, value => value.Id);
                enhancementRecipesById = ToDictionary(
                    enhancementFile.Recipes,
                    value => value.Id);
                enhanceNodesById = ToDictionary(enhancementFile.Nodes, value => value.Id);
                restNodesById = ToDictionary(restFile.Nodes, value => value.Id);
            }

            var runValidation = RunContentValidator.Validate(
                RunMaps,
                Encounters,
                RewardTables,
                MinionsById,
                SpellsById,
                EventPoolsById,
                EventsById,
                EnhancementRecipesById,
                EnhanceNodesById,
                RestNodesById);
            Merge(validation, runValidation);
            return validation;
        }

        public bool TryGetMinion(string id, out MinionConfig config)
        {
            return minionsById.TryGetValue(id, out config);
        }

        public bool TryGetSpell(string id, out SpellConfig config)
        {
            return spellsById.TryGetValue(id, out config);
        }

        public bool TryGetEncounter(string id, out EncounterConfig config)
        {
            return encountersById.TryGetValue(id ?? string.Empty, out config);
        }

        public bool TryGetRewardTable(string id, out RewardTableConfig config)
        {
            return rewardTablesById.TryGetValue(id ?? string.Empty, out config);
        }

        public bool TryGetEventPool(string id, out EventPoolConfig config)
        {
            return eventPoolsById.TryGetValue(id ?? string.Empty, out config);
        }

        public bool TryGetEvent(string id, out EventConfig config)
        {
            return eventsById.TryGetValue(id ?? string.Empty, out config);
        }

        public bool TryGetEnhanceNode(string id, out EnhanceNodeConfig config)
        {
            return enhanceNodesById.TryGetValue(id ?? string.Empty, out config);
        }

        public bool TryGetEnhancementRecipe(string id, out EnhancementRecipeConfig config)
        {
            return enhancementRecipesById.TryGetValue(id ?? string.Empty, out config);
        }

        public bool TryGetRestNode(string id, out RestNodeConfig config)
        {
            return restNodesById.TryGetValue(id ?? string.Empty, out config);
        }

        private static Dictionary<string, T> ToDictionary<T>(
            IEnumerable<T> values,
            Func<T, string> getId)
            where T : class
        {
            return (values ?? Array.Empty<T>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(getId(value)))
                .GroupBy(getId)
                .ToDictionary(group => group.Key, group => group.First());
        }

        private static void Merge(ConfigValidationResult target, ConfigValidationResult source)
        {
            foreach (var error in source.Errors)
            {
                target.AddError(error);
            }

            foreach (var warning in source.Warnings)
            {
                target.AddWarning(warning);
            }
        }
    }
}

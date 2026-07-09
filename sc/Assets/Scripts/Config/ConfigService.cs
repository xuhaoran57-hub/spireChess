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

        public ConfigService(IJsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public string Version { get; private set; }
        public IReadOnlyList<MinionConfig> Minions { get; private set; } = Array.Empty<MinionConfig>();
        public IReadOnlyList<SpellConfig> Spells { get; private set; } = Array.Empty<SpellConfig>();
        public IReadOnlyDictionary<string, MinionConfig> MinionsById => minionsById;
        public IReadOnlyDictionary<string, SpellConfig> SpellsById => spellsById;

        public ConfigValidationResult LoadFromResources(
            string minionResourcePath = "Configs/Json/minions.v0.1",
            string spellResourcePath = "Configs/Json/spells.v0.1")
        {
            var minionAsset = Resources.Load<TextAsset>(minionResourcePath);
            var spellAsset = Resources.Load<TextAsset>(spellResourcePath);

            if (minionAsset == null)
            {
                throw new InvalidOperationException($"Missing minion config resource: {minionResourcePath}.");
            }

            if (spellAsset == null)
            {
                throw new InvalidOperationException($"Missing spell config resource: {spellResourcePath}.");
            }

            return LoadFromJson(minionAsset.text, spellAsset.text);
        }

        public ConfigValidationResult LoadFromJson(string minionsJson, string spellsJson)
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

            return ConfigValidator.Validate(Minions, Spells);
        }

        public bool TryGetMinion(string id, out MinionConfig config)
        {
            return minionsById.TryGetValue(id, out config);
        }

        public bool TryGetSpell(string id, out SpellConfig config)
        {
            return spellsById.TryGetValue(id, out config);
        }
    }
}

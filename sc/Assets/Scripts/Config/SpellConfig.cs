using System.Collections.Generic;
using Newtonsoft.Json;

namespace SpireChess.Config
{
    public sealed class SpellConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("spells")]
        public List<SpellConfig> Spells { get; set; } = new List<SpellConfig>();
    }

    public sealed class SpellConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("tier")]
        public int Tier { get; set; }

        [JsonProperty("spellType")]
        public string SpellType { get; set; }

        [JsonProperty("useTiming")]
        public List<string> UseTiming { get; set; } = new List<string>();

        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        [JsonProperty("cost")]
        public int Cost { get; set; }

        [JsonProperty("artId")]
        public string ArtId { get; set; }

        [JsonProperty("iconId")]
        public string IconId { get; set; }

        [JsonProperty("audioId")]
        public string AudioId { get; set; }

        [JsonProperty("effects")]
        public List<EffectConfig> Effects { get; set; } = new List<EffectConfig>();

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("devNote")]
        public string DevNote { get; set; }
    }
}

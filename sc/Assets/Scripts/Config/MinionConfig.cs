using System.Collections.Generic;
using Newtonsoft.Json;

namespace SpireChess.Config
{
    public sealed class MinionConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("minions")]
        public List<MinionConfig> Minions { get; set; } = new List<MinionConfig>();
    }

    public sealed class MinionConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("goldenDescription")]
        public string GoldenDescription { get; set; }

        [JsonProperty("tier")]
        public int Tier { get; set; }

        [JsonProperty("race")]
        public string Race { get; set; }

        [JsonProperty("archetypes")]
        public List<string> Archetypes { get; set; } = new List<string>();

        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();

        [JsonProperty("isToken")]
        public bool IsToken { get; set; }

        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("health")]
        public int Health { get; set; }

        [JsonProperty("goldenAttack")]
        public int GoldenAttack { get; set; }

        [JsonProperty("goldenHealth")]
        public int GoldenHealth { get; set; }

        [JsonProperty("artId")]
        public string ArtId { get; set; }

        [JsonProperty("iconId")]
        public string IconId { get; set; }

        [JsonProperty("audioId")]
        public string AudioId { get; set; }

        [JsonProperty("effects")]
        public List<EffectConfig> Effects { get; set; } = new List<EffectConfig>();

        [JsonProperty("goldenEffects")]
        public List<EffectConfig> GoldenEffects { get; set; } = new List<EffectConfig>();

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("devNote")]
        public string DevNote { get; set; }
    }
}

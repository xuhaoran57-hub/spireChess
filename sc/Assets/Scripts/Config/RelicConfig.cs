using System.Collections.Generic;
using Newtonsoft.Json;

namespace SpireChess.Config
{
    public sealed class RelicConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("relics")]
        public List<RelicConfig> Relics { get; set; } = new List<RelicConfig>();
    }

    public sealed class RelicConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("grade")]
        public string Grade { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("effectType")]
        public string EffectType { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("health")]
        public int Health { get; set; }

        [JsonProperty("interval")]
        public int Interval { get; set; }

        [JsonProperty("targetCount")]
        public int TargetCount { get; set; }

        [JsonProperty("cardTier")]
        public int CardTier { get; set; }

        [JsonProperty("tokenId")]
        public string TokenId { get; set; }

        [JsonProperty("selector")]
        public string Selector { get; set; }

        [JsonProperty("tierMode")]
        public string TierMode { get; set; }

        [JsonProperty("conditionType")]
        public string ConditionType { get; set; }

        [JsonProperty("threshold")]
        public int Threshold { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("implementationStatus")]
        public string ImplementationStatus { get; set; } = "Playable";

        [JsonProperty("uiIconId")]
        public string UiIconId { get; set; }

        [JsonProperty("devNote")]
        public string DevNote { get; set; }
    }
}

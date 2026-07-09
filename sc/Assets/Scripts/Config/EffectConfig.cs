using System.Collections.Generic;
using Newtonsoft.Json;

namespace SpireChess.Config
{
    public sealed class EffectConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("trigger")]
        public string Trigger { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("target")]
        public TargetConfig Target { get; set; }

        [JsonProperty("value")]
        public ValueConfig Value { get; set; }

        [JsonProperty("condition")]
        public ConditionConfig Condition { get; set; }

        [JsonProperty("limit")]
        public LimitConfig Limit { get; set; }

        [JsonProperty("discover")]
        public DiscoverConfig Discover { get; set; }

        [JsonProperty("fallbackEffects")]
        public List<EffectConfig> FallbackEffects { get; set; } = new List<EffectConfig>();
    }

    public sealed class TargetConfig
    {
        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("race")]
        public string Race { get; set; }

        [JsonProperty("includeToken")]
        public bool IncludeToken { get; set; }

        [JsonProperty("maxTargets")]
        public int MaxTargets { get; set; }

        [JsonProperty("selector")]
        public string Selector { get; set; }
    }

    public sealed class ValueConfig
    {
        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("health")]
        public int Health { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("keyword")]
        public string Keyword { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }
    }

    public sealed class ConditionConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("race")]
        public string Race { get; set; }

        [JsonProperty("keyword")]
        public string Keyword { get; set; }

        [JsonProperty("threshold")]
        public int Threshold { get; set; }

        [JsonProperty("compare")]
        public string Compare { get; set; }

        [JsonProperty("phaseStat")]
        public string PhaseStat { get; set; }
    }

    public sealed class LimitConfig
    {
        [JsonProperty("perCombat")]
        public int PerCombat { get; set; }

        [JsonProperty("perShop")]
        public int PerShop { get; set; }

        [JsonProperty("perRun")]
        public int PerRun { get; set; }
    }

    public sealed class DiscoverConfig
    {
        [JsonProperty("cardType")]
        public string CardType { get; set; }

        [JsonProperty("race")]
        public string Race { get; set; }

        [JsonProperty("minTier")]
        public int MinTier { get; set; }

        [JsonProperty("maxTierMode")]
        public string MaxTierMode { get; set; }

        [JsonProperty("maxTierOffset")]
        public int MaxTierOffset { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("pick")]
        public int Pick { get; set; }

        [JsonProperty("includeToken")]
        public bool IncludeToken { get; set; }

        [JsonProperty("includeDisabled")]
        public bool IncludeDisabled { get; set; }

        [JsonProperty("requireGolden")]
        public bool RequireGolden { get; set; }
    }
}

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SpireChess.Config
{
    public sealed class RunMapConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("maps")]
        public List<RunMapConfig> Maps { get; set; } = new List<RunMapConfig>();
    }

    public sealed class RunMapConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("floor")]
        public int Floor { get; set; }

        [JsonProperty("startNodeIds")]
        public List<string> StartNodeIds { get; set; } = new List<string>();

        [JsonProperty("nodes")]
        public List<RunMapNodeConfig> Nodes { get; set; } = new List<RunMapNodeConfig>();
    }

    public sealed class RunMapNodeConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("row")]
        public int Row { get; set; }

        [JsonProperty("payloadId")]
        public string PayloadId { get; set; }

        [JsonProperty("nextNodeIds")]
        public List<string> NextNodeIds { get; set; } = new List<string>();
    }

    public sealed class EncounterConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("encounters")]
        public List<EncounterConfig> Encounters { get; set; } = new List<EncounterConfig>();
    }

    public sealed class EncounterConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("floor")]
        public int Floor { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("theme")]
        public string Theme { get; set; }

        [JsonProperty("riskText")]
        public string RiskText { get; set; }

        [JsonProperty("rewardPreviewText")]
        public string RewardPreviewText { get; set; }

        [JsonProperty("damageBonus")]
        public int DamageBonus { get; set; }

        [JsonProperty("rewardTableId")]
        public string RewardTableId { get; set; }

        [JsonProperty("enemySlots")]
        public List<EnemySlotConfig> EnemySlots { get; set; } = new List<EnemySlotConfig>();
    }

    public sealed class EnemySlotConfig
    {
        [JsonProperty("slot")]
        public int Slot { get; set; }

        [JsonProperty("minionId")]
        public string MinionId { get; set; }

        [JsonProperty("golden")]
        public bool Golden { get; set; }

        [JsonProperty("attackBonus")]
        public int AttackBonus { get; set; }

        [JsonProperty("healthBonus")]
        public int HealthBonus { get; set; }

        [JsonProperty("permanentKeywords")]
        public List<string> PermanentKeywords { get; set; } = new List<string>();
    }

    public sealed class RewardConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("rewardTables")]
        public List<RewardTableConfig> RewardTables { get; set; } = new List<RewardTableConfig>();
    }

    public sealed class RewardTableConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("entries")]
        public List<RewardEntryConfig> Entries { get; set; } = new List<RewardEntryConfig>();

        [JsonProperty("mode")]
        public string Mode { get; set; } = "AutomaticOne";

        [JsonProperty("candidateCount")]
        public int CandidateCount { get; set; } = 1;

        [JsonProperty("preferDistinctCategories")]
        public bool PreferDistinctCategories { get; set; }

        [JsonProperty("allowSkip")]
        public bool AllowSkip { get; set; } = true;
    }

    public sealed class RewardEntryConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("cardId")]
        public string CardId { get; set; }

        [JsonProperty("maximumTierOffset")]
        public int MaximumTierOffset { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("health")]
        public int Health { get; set; }

        [JsonProperty("targetScope")]
        public string TargetScope { get; set; }
    }

    public sealed class EventConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("eventPools")]
        public List<EventPoolConfig> EventPools { get; set; } = new List<EventPoolConfig>();

        [JsonProperty("events")]
        public List<EventConfig> Events { get; set; } = new List<EventConfig>();
    }

    public sealed class EventPoolConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("entries")]
        public List<EventPoolEntryConfig> Entries { get; set; } = new List<EventPoolEntryConfig>();
    }

    public sealed class EventPoolEntryConfig
    {
        [JsonProperty("eventId")]
        public string EventId { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }
    }

    public sealed class EventConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("options")]
        public List<EventOptionConfig> Options { get; set; } = new List<EventOptionConfig>();
    }

    public sealed class EventOptionConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("effects")]
        public List<RunEffectConfig> Effects { get; set; } = new List<RunEffectConfig>();

        [JsonProperty("followupRewardTableId")]
        public string FollowupRewardTableId { get; set; }
    }

    public sealed class RunEffectConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }
    }

    public sealed class EnhancementConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("recipes")]
        public List<EnhancementRecipeConfig> Recipes { get; set; } =
            new List<EnhancementRecipeConfig>();

        [JsonProperty("nodes")]
        public List<EnhanceNodeConfig> Nodes { get; set; } = new List<EnhanceNodeConfig>();
    }

    public sealed class EnhancementRecipeConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("health")]
        public int Health { get; set; }

        [JsonProperty("keyword")]
        public string Keyword { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public sealed class EnhanceNodeConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("recipeIds")]
        public List<string> RecipeIds { get; set; } = new List<string>();

        [JsonProperty("allowSkip")]
        public bool AllowSkip { get; set; } = true;
    }

    public sealed class RestConfigFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("nodes")]
        public List<RestNodeConfig> Nodes { get; set; } = new List<RestNodeConfig>();
    }

    public sealed class RestNodeConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("options")]
        public List<RestOptionConfig> Options { get; set; } = new List<RestOptionConfig>();
    }

    public sealed class RestOptionConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("heal")]
        public int Heal { get; set; }

        [JsonProperty("maxHealth")]
        public int MaxHealth { get; set; }
    }
}

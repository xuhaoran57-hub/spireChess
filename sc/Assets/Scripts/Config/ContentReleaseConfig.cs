using System.Collections.Generic;
using Newtonsoft.Json;

namespace SpireChess.Config
{
    public sealed class ContentReleaseConfig
    {
        [JsonProperty("contentVersion")]
        public string ContentVersion { get; set; }

        [JsonProperty("minimumRulesVersion")]
        public string MinimumRulesVersion { get; set; }

        [JsonProperty("minionIds")]
        public List<string> MinionIds { get; set; } = new List<string>();

        [JsonProperty("spellIds")]
        public List<string> SpellIds { get; set; } = new List<string>();

        [JsonProperty("encounterIds")]
        public List<string> EncounterIds { get; set; } = new List<string>();

        [JsonProperty("eventIds")]
        public List<string> EventIds { get; set; } = new List<string>();

        [JsonProperty("rewardTableIds")]
        public List<string> RewardTableIds { get; set; } = new List<string>();
    }
}

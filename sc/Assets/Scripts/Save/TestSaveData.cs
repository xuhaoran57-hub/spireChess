using System.Collections.Generic;
using Newtonsoft.Json;

namespace SpireChess.Save
{
    public sealed class TestSaveData
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("gold")]
        public int Gold { get; set; }

        [JsonProperty("testMinionIds")]
        public List<string> TestMinionIds { get; set; } = new List<string>();
    }
}

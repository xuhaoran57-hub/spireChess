using Newtonsoft.Json;

namespace SpireChess.Utils
{
    public sealed class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public string ToJson<T>(T value, bool pretty = false)
        {
            return JsonConvert.SerializeObject(
                value,
                pretty ? Formatting.Indented : Formatting.None);
        }
    }
}

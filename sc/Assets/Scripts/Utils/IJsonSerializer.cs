namespace SpireChess.Utils
{
    public interface IJsonSerializer
    {
        T FromJson<T>(string json);
        string ToJson<T>(T value, bool pretty = false);
    }
}

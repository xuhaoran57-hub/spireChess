using SpireChess.Utils;

namespace SpireChess.Save
{
    public sealed class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly IJsonSerializer serializer;

        public SaveService(ISaveStorage storage, IJsonSerializer serializer)
        {
            this.storage = storage;
            this.serializer = serializer;
        }

        public bool Exists(string key)
        {
            return storage.Exists(key);
        }

        public void Save<T>(string key, T data)
        {
            storage.WriteText(key, serializer.ToJson(data, true));
        }

        public T Load<T>(string key)
        {
            return serializer.FromJson<T>(storage.ReadText(key));
        }

        public void Delete(string key)
        {
            storage.Delete(key);
        }
    }
}

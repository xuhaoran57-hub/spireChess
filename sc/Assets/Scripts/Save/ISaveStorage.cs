namespace SpireChess.Save
{
    public interface ISaveStorage
    {
        bool Exists(string key);
        string ReadText(string key);
        void WriteText(string key, string content);
        void Delete(string key);
    }
}
